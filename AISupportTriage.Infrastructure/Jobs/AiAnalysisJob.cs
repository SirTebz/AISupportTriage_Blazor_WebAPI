using AISupportTriage.Application.Interfaces;
using AISupportTriage.Domain.Entities;
using AISupportTriage.Domain.Enums;
using AISupportTriage.Infrastructure.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AISupportTriage.Infrastructure.Jobs;

public class AiAnalysisJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AiAnalysisJob> _logger;

    public AiAnalysisJob(IServiceProvider serviceProvider, ILogger<AiAnalysisJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task ProcessAsync(Guid ticketId)
    {
        _logger.LogInformation("Starting AI analysis for ticket {TicketId}", ticketId);

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var aiService = scope.ServiceProvider.GetRequiredService<IAiTriageService>();
        var routingEngine = scope.ServiceProvider.GetRequiredService<IRoutingEngine>();
        var hubContext = scope.ServiceProvider.GetRequiredService<
            Microsoft.AspNetCore.SignalR.IHubContext<
                AISupportTriage.Infrastructure.Hubs.TicketNotificationHub>>();

        // Use IgnoreQueryFilters to bypass the soft-delete filter
        var ticket = await context.Tickets
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket == null)
        {
            _logger.LogWarning("Ticket {TicketId} not found for AI analysis.", ticketId);
            return;
        }

        try
        {
            var result = await aiService.AnalyzeTicketAsync(ticket.Title, ticket.Description);

            // Parse category
            if (Enum.TryParse<TicketCategory>(result.Category, true, out var category))
                ticket.Category = category;

            ticket.SentimentScore = result.SentimentScore;
            ticket.UrgencyScore = result.UrgencyScore;
            ticket.ConfidenceScore = result.Confidence;
            ticket.SuggestedTags = result.SuggestedTags.Any()
                ? string.Join(",", result.SuggestedTags)
                : null;
            ticket.AiAnalysisComplete = true;

            if (ticket.Status == TicketStatus.PendingAnalysis)
                ticket.Status = TicketStatus.Open;

            // Add AI suggested reply as internal note
            if (!string.IsNullOrWhiteSpace(result.SuggestedReply))
            {
                context.TicketMessages.Add(new TicketMessage
                {
                    TicketId = ticket.Id,
                    SenderId = "System",
                    Content = $"[AI Suggestion] {result.SuggestedReply}",
                    IsInternal = true
                });
            }

            context.TicketAuditLogs.Add(new TicketAuditLog
            {
                TicketId = ticket.Id,
                Action = "AiAnalysisComplete",
                NewValue = $"Category={ticket.Category}, Urgency={ticket.UrgencyScore:F2}, Sentiment={ticket.SentimentScore:F2}",
                PerformedById = "System"
            });

            await context.SaveChangesAsync();

            // Run routing after AI analysis
            await routingEngine.RouteTicketAsync(ticketId);

            // Notify via SignalR
            await hubContext.Clients.All.SendAsync("TicketUpdated", ticketId.ToString());

            _logger.LogInformation("AI analysis complete for ticket {TicketId}", ticketId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI analysis failed for ticket {TicketId}", ticketId);

            ticket.AiAnalysisComplete = false;
            ticket.Status = TicketStatus.Open;
            await context.SaveChangesAsync();
        }
    }
}