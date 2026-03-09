using Microsoft.AspNetCore.SignalR;

namespace AISupportTriage.Infrastructure.Hubs;

public class TicketNotificationHub : Hub
{
    public async Task JoinTenantGroup(string tenantId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
    }

    public async Task LeaveTenantGroup(string tenantId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
    }
}