namespace AISupportTriage.BlazorClient.Models;

// ── Auth ──────────────────────────────────────────────────────────────────────

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string CompanyName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public string AdminFirstName { get; set; } = string.Empty;
    public string AdminLastName { get; set; } = string.Empty;
    public string Plan { get; set; } = "Free";
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

// ── Ticket Requests ───────────────────────────────────────────────────────────

public class CreateTicketRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class UpdateStatusRequest
{
    public int Status { get; set; }
    public string? Note { get; set; }
}

public class AddMessageRequest
{
    public string Content { get; set; } = string.Empty;
    public bool IsInternal { get; set; } = false;
}

// ── Ticket List ───────────────────────────────────────────────────────────────

public class TicketListItem
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

// ── Ticket Detail — renamed to TicketDetailDto to avoid clash with ────────────
// ── the Razor page class auto-generated from TicketDetail.razor      ────────────

public class TicketDetailDto
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
    public List<MessageItem> Messages { get; set; } = new();
    public List<AuditItem> AuditLogs { get; set; } = new();
}

public class MessageItem
{
    public Guid Id { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AuditItem
{
    public string Action { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string PerformedById { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

// ── Analytics ─────────────────────────────────────────────────────────────────

public class AnalyticsSummary
{
    public int TotalTickets { get; set; }
    public int OpenTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public int SlaBreachedTickets { get; set; }
    public double AverageResolutionHours { get; set; }
    public List<CategoryCount> TicketsByCategory { get; set; } = new();
    public List<AgentWorkload> AgentWorkloads { get; set; } = new();
}

public class CategoryCount
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class AgentWorkload
{
    public string AgentId { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public int OpenTickets { get; set; }
    public int ResolvedTickets { get; set; }
}

public class ApiError
{
    public string? Error { get; set; }
}