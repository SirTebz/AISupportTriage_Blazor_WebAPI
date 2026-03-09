namespace AISupportTriage.Application.Interfaces;

public interface IRoutingEngine
{
    Task RouteTicketAsync(Guid ticketId);
}