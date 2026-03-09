using AISupportTriage.Application.DTOs.Tickets;
using AISupportTriage.Application.DTOs.Analytics;
using AISupportTriage.Application.Interfaces;
using AISupportTriage.Domain.Entities;
using AISupportTriage.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AISupportTriage.Application.Services;

public class TicketService : ITicketService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentTenantService _currentTenant;
    private readonly UserManager<ApplicationUser> _userManager;

    public TicketService(
        IApplicationDbContext context,
        ICurrentTenantService currentTenant,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _currentTenant = currentTenant;
        _userManager = userManager;
    }

    public async Task<TicketDto> CreateTicketAsync(CreateTicketDto dto)
    {
        var tenantId = _currentTenant.GetTenantId();
        var userId = _currentTenant.GetUserId();

        // SLA deadline: 24h for Free, 8h for Pro, 4h for Enterprise
        var tenant = await _context.Tenants.FindAsync(tenantId)
            ?? throw new InvalidOperationException("Tenant not found.");

        var slaHours = tenant.Plan switch
        {
            "Pro" => 8,
            "Enterprise" => 4,
            _ => 24
        };

        var ticket = new Ticket
        {
            TenantId = tenantId,
            Title = dto.Title,
            Description = dto.Description,
            CreatedById = userId,
            Status = TicketStatus.PendingAnalysis,
            SLADeadline = DateTime.UtcNow.AddHours(slaHours)
        };

        _context.Tickets.Add(ticket);

        // Add initial audit log
        _context.TicketAuditLogs.Add(new TicketAuditLog
        {
            TicketId = ticket.Id,
            Action = "Created",
            NewValue = "Open",
            PerformedById = userId
        });

        await _context.SaveChangesAsync();
        return await GetTicketByIdAsync(ticket.Id) ?? throw new InvalidOperationException("Failed to retrieve created ticket.");
    }

    public async Task<TicketDto?> GetTicketByIdAsync(Guid id)
    {
        var ticket = await _context.Tickets
            .Include(t => t.AssignedAgent)
            .Include(t => t.CreatedBy)
            .Include(t => t.Messages)
                .ThenInclude(m => m.Sender)
            .Include(t => t.AuditLogs)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

        return ticket == null ? null : MapToDto(ticket);
    }

    public async Task<List<TicketListDto>> GetTicketsAsync()
    {
        return await _context.Tickets
            .Include(t => t.AssignedAgent)
            .Include(t => t.CreatedBy)
            .Where(t => !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TicketListDto
            {
                Id = t.Id,
                Title = t.Title,
                Status = t.Status.ToString(),
                Priority = t.Priority.ToString(),
                Category = t.Category.ToString(),
                AssignedAgentName = t.AssignedAgent != null
                    ? t.AssignedAgent.FirstName + " " + t.AssignedAgent.LastName
                    : null,
                CreatedByName = t.CreatedBy != null
                    ? t.CreatedBy.FirstName + " " + t.CreatedBy.LastName
                    : "Unknown",
                CreatedAt = t.CreatedAt,
                SLADeadline = t.SLADeadline,
                SlaBreached = t.SlaBreached,
                AiAnalysisComplete = t.AiAnalysisComplete
            })
            .ToListAsync();
    }

    public async Task<TicketDto> UpdateStatusAsync(Guid id, UpdateTicketStatusDto dto)
    {
        var userId = _currentTenant.GetUserId();
        var ticket = await _context.Tickets.FindAsync(id)
            ?? throw new KeyNotFoundException($"Ticket {id} not found.");

        var oldStatus = ticket.Status.ToString();
        ticket.Status = dto.Status;
        ticket.UpdatedAt = DateTime.UtcNow;

        if (dto.Status == TicketStatus.Resolved)
            ticket.ResolvedAt = DateTime.UtcNow;

        _context.TicketAuditLogs.Add(new TicketAuditLog
        {
            TicketId = ticket.Id,
            Action = "StatusChanged",
            OldValue = oldStatus,
            NewValue = dto.Status.ToString(),
            PerformedById = userId
        });

        if (!string.IsNullOrWhiteSpace(dto.Note))
        {
            _context.TicketMessages.Add(new TicketMessage
            {
                TicketId = ticket.Id,
                SenderId = userId,
                Content = dto.Note,
                IsInternal = true
            });
        }

        await _context.SaveChangesAsync();
        return await GetTicketByIdAsync(id) ?? throw new InvalidOperationException("Failed to retrieve ticket.");
    }

    public async Task<TicketDto> AssignAgentAsync(Guid id, AssignAgentDto dto)
    {
        var userId = _currentTenant.GetUserId();
        var ticket = await _context.Tickets.FindAsync(id)
            ?? throw new KeyNotFoundException($"Ticket {id} not found.");

        var oldAgent = ticket.AssignedAgentId;
        ticket.AssignedAgentId = dto.AgentId;
        ticket.UpdatedAt = DateTime.UtcNow;

        if (ticket.Status == TicketStatus.Open || ticket.Status == TicketStatus.PendingAnalysis)
            ticket.Status = TicketStatus.InProgress;

        _context.TicketAuditLogs.Add(new TicketAuditLog
        {
            TicketId = ticket.Id,
            Action = "AgentAssigned",
            OldValue = oldAgent,
            NewValue = dto.AgentId,
            PerformedById = userId
        });

        await _context.SaveChangesAsync();
        return await GetTicketByIdAsync(id) ?? throw new InvalidOperationException("Failed to retrieve ticket.");
    }

    public async Task<TicketMessageDto> AddMessageAsync(Guid id, AddMessageDto dto)
    {
        var userId = _currentTenant.GetUserId();
        var ticket = await _context.Tickets.FindAsync(id)
            ?? throw new KeyNotFoundException($"Ticket {id} not found.");

        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        var message = new TicketMessage
        {
            TicketId = id,
            SenderId = userId,
            Content = dto.Content,
            IsInternal = dto.IsInternal
        };

        _context.TicketMessages.Add(message);
        ticket.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new TicketMessageDto
        {
            Id = message.Id,
            SenderId = userId,
            SenderName = user.FullName,
            Content = message.Content,
            IsInternal = message.IsInternal,
            CreatedAt = message.CreatedAt
        };
    }

    public async Task<AnalyticsSummaryDto> GetAnalyticsAsync()
    {
        var tickets = await _context.Tickets
            .Include(t => t.AssignedAgent)
            .Where(t => !t.IsDeleted)
            .ToListAsync();

        var resolved = tickets.Where(t => t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed).ToList();
        var avgResolution = resolved.Any()
            ? resolved
                .Where(t => t.ResolvedAt.HasValue)
                .Select(t => (t.ResolvedAt!.Value - t.CreatedAt).TotalHours)
                .DefaultIfEmpty(0)
                .Average()
            : 0;

        var byCategory = tickets
            .GroupBy(t => t.Category.ToString())
            .Select(g => new CategoryCountDto { Category = g.Key, Count = g.Count() })
            .ToList();

        var agentWorkloads = tickets
            .Where(t => t.AssignedAgent != null)
            .GroupBy(t => new { t.AssignedAgentId, t.AssignedAgent!.FullName })
            .Select(g => new AgentWorkloadDto
            {
                AgentId = g.Key.AssignedAgentId!,
                AgentName = g.Key.FullName,
                OpenTickets = g.Count(t => t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed),
                ResolvedTickets = g.Count(t => t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed)
            })
            .ToList();

        var last30Days = tickets
            .Where(t => t.CreatedAt >= DateTime.UtcNow.AddDays(-30))
            .GroupBy(t => t.CreatedAt.Date)
            .Select(g => new DailyTicketCountDto { Date = g.Key, Count = g.Count() })
            .OrderBy(d => d.Date)
            .ToList();

        return new AnalyticsSummaryDto
        {
            TotalTickets = tickets.Count,
            OpenTickets = tickets.Count(t => t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress),
            ResolvedTickets = resolved.Count,
            SlaBreachedTickets = tickets.Count(t => t.SlaBreached),
            AverageResolutionHours = Math.Round(avgResolution, 1),
            TicketsByCategory = byCategory,
            AgentWorkloads = agentWorkloads,
            DailyTickets = last30Days
        };
    }

    private static TicketDto MapToDto(Ticket t) => new()
    {
        Id = t.Id,
        Title = t.Title,
        Description = t.Description,
        Status = t.Status.ToString(),
        Priority = t.Priority.ToString(),
        Category = t.Category.ToString(),
        SentimentScore = t.SentimentScore,
        UrgencyScore = t.UrgencyScore,
        AiAnalysisComplete = t.AiAnalysisComplete,
        SuggestedTags = t.SuggestedTags,
        AssignedAgentId = t.AssignedAgentId,
        AssignedAgentName = t.AssignedAgent?.FullName,
        CreatedById = t.CreatedById,
        CreatedByName = t.CreatedBy?.FullName ?? "Unknown",
        CreatedAt = t.CreatedAt,
        SLADeadline = t.SLADeadline,
        ResolvedAt = t.ResolvedAt,
        SlaBreached = t.SlaBreached,
        Messages = t.Messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new TicketMessageDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                SenderName = m.Sender?.FullName ?? "Unknown",
                Content = m.Content,
                IsInternal = m.IsInternal,
                CreatedAt = m.CreatedAt
            }).ToList(),
        AuditLogs = t.AuditLogs
            .OrderByDescending(a => a.Timestamp)
            .Select(a => new TicketAuditLogDto
            {
                Action = a.Action,
                OldValue = a.OldValue,
                NewValue = a.NewValue,
                PerformedById = a.PerformedById,
                Timestamp = a.Timestamp
            }).ToList()
    };
}