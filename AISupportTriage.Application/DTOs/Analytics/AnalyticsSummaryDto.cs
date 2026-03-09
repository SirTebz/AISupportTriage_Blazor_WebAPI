namespace AISupportTriage.Application.DTOs.Analytics;

public class AnalyticsSummaryDto
{
    public int TotalTickets { get; set; }
    public int OpenTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public int SlaBreachedTickets { get; set; }
    public double AverageResolutionHours { get; set; }
    public List<CategoryCountDto> TicketsByCategory { get; set; } = new();
    public List<AgentWorkloadDto> AgentWorkloads { get; set; } = new();
    public List<DailyTicketCountDto> DailyTickets { get; set; } = new();
}

public class CategoryCountDto
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class AgentWorkloadDto
{
    public string AgentId { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public int OpenTickets { get; set; }
    public int ResolvedTickets { get; set; }
}

public class DailyTicketCountDto
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}