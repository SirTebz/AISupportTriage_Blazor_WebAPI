using AISupportTriage.Application.DTOs.Tickets;
using AISupportTriage.Application.DTOs.Analytics;

namespace AISupportTriage.Application.Interfaces;

public interface ITicketService
{
    Task<TicketDto> CreateTicketAsync(CreateTicketDto dto);
    Task<TicketDto?> GetTicketByIdAsync(Guid id);
    Task<List<TicketListDto>> GetTicketsAsync();
    Task<TicketDto> UpdateStatusAsync(Guid id, UpdateTicketStatusDto dto);
    Task<TicketDto> AssignAgentAsync(Guid id, AssignAgentDto dto);
    Task<TicketMessageDto> AddMessageAsync(Guid id, AddMessageDto dto);
    Task<AnalyticsSummaryDto> GetAnalyticsAsync();
}