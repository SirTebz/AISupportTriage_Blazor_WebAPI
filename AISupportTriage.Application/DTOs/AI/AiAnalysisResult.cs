namespace AISupportTriage.Application.DTOs.AI;

public class AiAnalysisResult
{
    public string Category { get; set; } = "General";
    public double SentimentScore { get; set; } = 0.5;
    public double UrgencyScore { get; set; } = 0.5;
    public double Confidence { get; set; } = 0.0;
    public List<string> SuggestedTags { get; set; } = new();
    public string? SuggestedReply { get; set; }
    public bool Success { get; set; } = false;
    public string? ErrorMessage { get; set; }
}