using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AISupportTriage.Application.DTOs.AI;
using AISupportTriage.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AISupportTriage.Infrastructure.AI;

public class OpenAiTriageService : IAiTriageService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<OpenAiTriageService> _logger;
    private const string OpenAiChatUrl = "https://api.openai.com/v1/chat/completions";

    public OpenAiTriageService(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<OpenAiTriageService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;

        var apiKey = _config["OpenAI:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);
        }
    }

    public async Task<AiAnalysisResult> AnalyzeTicketAsync(string title, string description)
    {
        var fallback = new AiAnalysisResult
        {
            Category = "General",
            SentimentScore = 0.5,
            UrgencyScore = 0.5,
            Confidence = 0.0,
            Success = false,
            ErrorMessage = "AI service unavailable — using defaults."
        };

        try
        {
            var apiKey = _config["OpenAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_OPENAI_API_KEY")
            {
                _logger.LogWarning("OpenAI API key not configured. Using mock analysis.");
                return GetMockAnalysis(title, description);
            }

            var systemPrompt = """
                You are an AI support ticket triage system. Analyze the support ticket and respond ONLY with valid JSON.
                No markdown, no explanation — raw JSON only.
                
                JSON format:
                {
                  "category": "Technical|Billing|Sales|Security|AccountManagement|General|Other",
                  "sentimentScore": 0.0-1.0 (0=very negative, 1=very positive),
                  "urgencyScore": 0.0-1.0 (0=not urgent, 1=extremely urgent/emergency),
                  "confidence": 0.0-1.0,
                  "suggestedTags": ["tag1", "tag2"],
                  "suggestedReply": "Brief suggested first response to the customer"
                }
                """;

            var userMessage = $"Ticket Title: {title}\n\nTicket Description: {description}";

            var requestBody = new
            {
                model = "gpt-4o-mini",
                max_tokens = 500,
                temperature = 0.2,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userMessage }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var response = await _httpClient.PostAsync(OpenAiChatUrl, content, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("OpenAI API returned {Status}: {Error}", response.StatusCode, error);
                return fallback;
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            var messageContent = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";

            // Strip any markdown fences just in case
            messageContent = messageContent
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            using var resultDoc = JsonDocument.Parse(messageContent);
            var root = resultDoc.RootElement;

            var result = new AiAnalysisResult
            {
                Category = root.TryGetProperty("category", out var cat) ? cat.GetString() ?? "General" : "General",
                SentimentScore = root.TryGetProperty("sentimentScore", out var sent) ? sent.GetDouble() : 0.5,
                UrgencyScore = root.TryGetProperty("urgencyScore", out var urg) ? urg.GetDouble() : 0.5,
                Confidence = root.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 0.5,
                SuggestedReply = root.TryGetProperty("suggestedReply", out var reply) ? reply.GetString() : null,
                Success = true
            };

            if (root.TryGetProperty("suggestedTags", out var tags) && tags.ValueKind == JsonValueKind.Array)
            {
                result.SuggestedTags = tags.EnumerateArray()
                    .Select(t => t.GetString() ?? "")
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToList();
            }

            return result;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("OpenAI request timed out.");
            var timeoutFallback = new AiAnalysisResult
            {
                Category = fallback.Category,
                SentimentScore = fallback.SentimentScore,
                UrgencyScore = fallback.UrgencyScore,
                Confidence = fallback.Confidence,
                SuggestedTags = fallback.SuggestedTags,
                SuggestedReply = fallback.SuggestedReply,
                Success = fallback.Success,
                ErrorMessage = "AI request timed out."
            };
            return timeoutFallback;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI analysis failed.");
            var errorFallback = new AiAnalysisResult
            {
                Category = fallback.Category,
                SentimentScore = fallback.SentimentScore,
                UrgencyScore = fallback.UrgencyScore,
                Confidence = fallback.Confidence,
                SuggestedTags = fallback.SuggestedTags,
                SuggestedReply = fallback.SuggestedReply,
                Success = fallback.Success,
                ErrorMessage = ex.Message
            };
            return errorFallback;
        }
    }

    // Mock analysis when no API key is configured (for development/demo)
    private static AiAnalysisResult GetMockAnalysis(string title, string description)
    {
        var text = (title + " " + description).ToLowerInvariant();

        var category = text.Contains("bill") || text.Contains("invoice") || text.Contains("payment") ? "Billing"
            : text.Contains("hack") || text.Contains("breach") || text.Contains("security") ? "Security"
            : text.Contains("bug") || text.Contains("error") || text.Contains("crash") || text.Contains("not working") ? "Technical"
            : text.Contains("buy") || text.Contains("price") || text.Contains("plan") ? "Sales"
            : "General";

        var urgency = text.Contains("urgent") || text.Contains("asap") || text.Contains("critical") || text.Contains("down")
            ? 0.85
            : text.Contains("soon") || text.Contains("important") ? 0.55
            : 0.35;

        var sentiment = text.Contains("terrible") || text.Contains("awful") || text.Contains("hate") ? 0.1
            : text.Contains("great") || text.Contains("love") || text.Contains("excellent") ? 0.9
            : 0.45;

        return new AiAnalysisResult
        {
            Category = category,
            SentimentScore = sentiment,
            UrgencyScore = urgency,
            Confidence = 0.75,
            SuggestedTags = [category.ToLower(), "auto-classified"],
            SuggestedReply = $"Thank you for contacting support. We've received your {category.ToLower()} request and will respond shortly.",
            Success = true
        };
    }
}