using AISupportTriage.Application.DTOs.AI;

namespace AISupportTriage.Application.Interfaces;

public interface IAiTriageService
{
    Task<AiAnalysisResult> AnalyzeTicketAsync(string title, string description);
}