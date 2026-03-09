using AISupportTriage.Application.Interfaces;
using AISupportTriage.Domain.Entities;
using AISupportTriage.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AISupportTriage.Application.Services;

public class RoutingEngine : IRoutingEngine
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public RoutingEngine(IApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task RouteTicketAsync(Guid ticketId)
    {
        var ticket = await _context.Tickets.FindAsync(ticketId);
        if (ticket == null) return;

        // Step 1: Set priority based on urgency score
        if (ticket.UrgencyScore.HasValue)
        {
            ticket.Priority = ticket.UrgencyScore.Value switch
            {
                > 0.85 => TicketPriority.Critical,
                > 0.65 => TicketPriority.High,
                > 0.40 => TicketPriority.Medium,
                _ => TicketPriority.Low
            };
        }

        // Step 2: Auto-assign to agent with fewest open tickets in matching department
        var targetDept = ticket.Category switch
        {
            TicketCategory.Billing => "Billing",
            TicketCategory.Technical => "Technical",
            TicketCategory.Security => "Security",
            TicketCategory.Sales => "Sales",
            _ => "Support"
        };

        var agents = await _userManager.GetUsersInRoleAsync("SupportAgent");
        var tenantAgents = agents
            .Where(a => a.TenantId == ticket.TenantId && a.IsActive)
            .ToList();

        if (!tenantAgents.Any())
        {
            await _context.SaveChangesAsync();
            return;
        }

        // Prefer agents in matching department
        var deptAgents = tenantAgents
            .Where(a => a.Department.Equals(targetDept, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var candidateAgents = deptAgents.Any() ? deptAgents : tenantAgents;

        // Count open tickets per agent
        var agentTicketCounts = await _context.Tickets
            .Where(t => !t.IsDeleted &&
                        t.Status != TicketStatus.Resolved &&
                        t.Status != TicketStatus.Closed &&
                        t.AssignedAgentId != null &&
                        candidateAgents.Select(a => a.Id).Contains(t.AssignedAgentId))
            .GroupBy(t => t.AssignedAgentId)
            .Select(g => new { AgentId = g.Key!, Count = g.Count() })
            .ToListAsync();

        // Find agent with lowest open ticket count
        var assignedAgent = candidateAgents
            .OrderBy(a => agentTicketCounts.FirstOrDefault(x => x.AgentId == a.Id)?.Count ?? 0)
            .FirstOrDefault();

        if (assignedAgent != null)
        {
            ticket.AssignedAgentId = assignedAgent.Id;
            ticket.Status = TicketStatus.InProgress;

            _context.TicketAuditLogs.Add(new TicketAuditLog
            {
                TicketId = ticket.Id,
                Action = "AutoAssigned",
                NewValue = assignedAgent.FullName,
                PerformedById = "System"
            });
        }

        await _context.SaveChangesAsync();
    }
}