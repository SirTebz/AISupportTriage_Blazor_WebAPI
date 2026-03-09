using AISupportTriage.Domain.Enums;
using AISupportTriage.Infrastructure.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AISupportTriage.Infrastructure.Jobs;

public class SlaCheckJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SlaCheckJob> _logger;

    public SlaCheckJob(IServiceProvider serviceProvider, ILogger<SlaCheckJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task CheckSlaBreachesAsync()
    {
        _logger.LogInformation("Running SLA breach check at {Time}", DateTime.UtcNow);

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hubContext = scope.ServiceProvider.GetRequiredService<
            Microsoft.AspNetCore.SignalR.IHubContext<
                AISupportTriage.Infrastructure.Hubs.TicketNotificationHub>>();

        var now = DateTime.UtcNow;

        var breachedTickets = await context.Tickets
            .IgnoreQueryFilters()
            .Where(t =>
                !t.IsDeleted &&
                !t.SlaBreached &&
                t.SLADeadline.HasValue &&
                t.SLADeadline.Value < now &&
                t.Status != TicketStatus.Resolved &&
                t.Status != TicketStatus.Closed)
            .ToListAsync();

        foreach (var ticket in breachedTickets)
        {
            ticket.SlaBreached = true;

            // Escalate priority if not already Critical
            if (ticket.Priority != TicketPriority.Critical)
                ticket.Priority = TicketPriority.High;

            context.TicketAuditLogs.Add(new Domain.Entities.TicketAuditLog
            {
                TicketId = ticket.Id,
                Action = "SlaBreached",
                NewValue = $"SLA deadline was {ticket.SLADeadline:u}",
                PerformedById = "System"
            });
        }

        if (breachedTickets.Any())
        {
            await context.SaveChangesAsync();
            _logger.LogWarning("{Count} SLA breaches detected.", breachedTickets.Count);

            // Notify dashboard
            await hubContext.Clients.All.SendAsync("SlaWarning", breachedTickets.Select(t => t.Id.ToString()).ToArray());
        }
    }
}