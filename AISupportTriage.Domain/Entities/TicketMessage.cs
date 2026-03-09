namespace AISupportTriage.Domain.Entities;

public class TicketMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    public string SenderId { get; set; } = string.Empty;
    public ApplicationUser? Sender { get; set; }

    public string Content { get; set; } = string.Empty;
    public bool IsInternal { get; set; } = false;  // internal agent notes
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}