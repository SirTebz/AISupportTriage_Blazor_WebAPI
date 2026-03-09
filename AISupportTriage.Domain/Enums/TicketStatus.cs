namespace AISupportTriage.Domain.Enums;

public enum TicketStatus
{
    Open = 0,
    PendingAnalysis = 1,
    InProgress = 2,
    WaitingOnCustomer = 3,
    Resolved = 4,
    Closed = 5
}