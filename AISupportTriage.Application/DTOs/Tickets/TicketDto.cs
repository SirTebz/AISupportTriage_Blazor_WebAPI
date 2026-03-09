namespace AISupportTriage.Application.DTOs.Tickets;

public class TicketDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double? SentimentScore { get; set; }
    public double? UrgencyScore { get; set; }
    public bool AiAnalysisComplete { get; set; }
    public string? SuggestedTags { get; set; }
    public string? AssignedAgentId { get; set; }
    public string? AssignedAgentName { get; set; }
    public string CreatedById { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? SLADeadline { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public bool SlaBreached { get; set; }
    public List<TicketMessageDto> Messages { get; set; } = new();
    public List<TicketAuditLogDto> AuditLogs { get; set; } = new();
}

public class TicketMessageDto
{
    public Guid Id { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TicketAuditLogDto
{
    public string Action { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string PerformedById { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class TicketListDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? AssignedAgentName { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? SLADeadline { get; set; }
    public bool SlaBreached { get; set; }
    public bool AiAnalysisComplete { get; set; }
}