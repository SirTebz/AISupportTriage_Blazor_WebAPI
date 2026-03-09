using AISupportTriage.Domain.Enums;

namespace AISupportTriage.Application.DTOs.Tickets;

public class UpdateTicketStatusDto
{
    public TicketStatus Status { get; set; }
    public string? Note { get; set; }
}