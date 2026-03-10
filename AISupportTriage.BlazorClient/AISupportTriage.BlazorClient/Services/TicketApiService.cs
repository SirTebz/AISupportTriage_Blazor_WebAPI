using System.Net.Http.Json;
using System.Text.Json;
using AISupportTriage.BlazorClient.Models;

namespace AISupportTriage.BlazorClient.Services;

public class TicketApiService
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    public TicketApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<TicketListItem>> GetTicketsAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<TicketListItem>>(
                "api/tickets", JsonOpts);
            return result ?? new List<TicketListItem>();
        }
        catch { return new List<TicketListItem>(); }
    }

    public async Task<TicketDetailDto?> GetTicketAsync(Guid id)
    {
        try
        {
            return await _http.GetFromJsonAsync<TicketDetailDto>(
                $"api/tickets/{id}", JsonOpts);
        }
        catch { return null; }
    }

    public async Task<(TicketDetailDto? Ticket, string? Error)> CreateTicketAsync(
        CreateTicketRequest req)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/tickets", req);
            if (!response.IsSuccessStatusCode)
                return (null, await ReadErrorAsync(response));

            var ticket = await response.Content
                .ReadFromJsonAsync<TicketDetailDto>(JsonOpts);
            return (ticket, null);
        }
        catch (Exception ex) { return (null, ex.Message); }
    }

    public async Task<(TicketDetailDto? Ticket, string? Error)> UpdateStatusAsync(
        Guid id, int status, string? note = null)
    {
        try
        {
            var req = new UpdateStatusRequest { Status = status, Note = note };
            var response = await _http.PutAsJsonAsync($"api/tickets/{id}/status", req);
            if (!response.IsSuccessStatusCode)
                return (null, await ReadErrorAsync(response));

            var ticket = await response.Content
                .ReadFromJsonAsync<TicketDetailDto>(JsonOpts);
            return (ticket, null);
        }
        catch (Exception ex) { return (null, ex.Message); }
    }

    public async Task<(MessageItem? Message, string? Error)> AddMessageAsync(
        Guid id, string content, bool isInternal = false)
    {
        try
        {
            var req = new AddMessageRequest { Content = content, IsInternal = isInternal };
            var response = await _http.PostAsJsonAsync($"api/tickets/{id}/messages", req);
            if (!response.IsSuccessStatusCode)
                return (null, await ReadErrorAsync(response));

            var msg = await response.Content
                .ReadFromJsonAsync<MessageItem>(JsonOpts);
            return (msg, null);
        }
        catch (Exception ex) { return (null, ex.Message); }
    }

    public async Task<AnalyticsSummary?> GetAnalyticsAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<AnalyticsSummary>(
                "api/analytics/summary", JsonOpts);
        }
        catch { return null; }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var err = await response.Content.ReadFromJsonAsync<ApiError>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return err?.Error ?? $"HTTP {(int)response.StatusCode}";
        }
        catch { return $"HTTP {(int)response.StatusCode}"; }
    }
}