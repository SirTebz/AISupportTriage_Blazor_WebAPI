using AISupportTriage.Domain.Enums;
using AISupportTriage.Domain.Interfaces;

namespace AISupportTriage.Domain.Entities;

public class Ticket : IHasTenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public TicketCategory Category { get; set; } = TicketCategory.General;

    // AI Analysis Fields
    public double? SentimentScore { get; set; }   // 0.0 (negative) → 1.0 (positive)
    public double? UrgencyScore { get; set; }      // 0.0 (low) → 1.0 (critical)
    public double? ConfidenceScore { get; set; }
    public string? SuggestedTags { get; set; }     // CSV string
    public bool AiAnalysisComplete { get; set; } = false;

    // Relationships
    public string? AssignedAgentId { get; set; }
    public ApplicationUser? AssignedAgent { get; set; }

    public string CreatedById { get; set; } = string.Empty;
    public ApplicationUser? CreatedBy { get; set; }

    // Timestamps & SLA
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? SLADeadline { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public bool SlaBreached { get; set; } = false;
    public bool IsDeleted { get; set; } = false;

    public ICollection<TicketMessage> Messages { get; set; } = new List<TicketMessage>();
    public ICollection<TicketAuditLog> AuditLogs { get; set; } = new List<TicketAuditLog>();
}