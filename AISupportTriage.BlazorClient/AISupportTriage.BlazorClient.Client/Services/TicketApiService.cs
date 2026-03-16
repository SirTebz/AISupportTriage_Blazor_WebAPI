using System.Net.Http.Json;
using System.Text.Json;
using AISupportTriage.BlazorClient.Client.Models;

namespace AISupportTriage.BlazorClient.Client.Services;

public class TicketApiService
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions Opts =
        new() { PropertyNameCaseInsensitive = true };

    public TicketApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<TicketListItem>> GetTicketsAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<TicketListItem>>(
                       "api/tickets", Opts)
                   ?? new List<TicketListItem>();
        }
        catch { return new List<TicketListItem>(); }
    }

    public async Task<TicketDetailDto?> GetTicketAsync(Guid id)
    {
        try
        {
            return await _http.GetFromJsonAsync<TicketDetailDto>(
                $"api/tickets/{id}", Opts);
        }
        catch { return null; }
    }

    public async Task<(TicketDetailDto? Ticket, string? Error)> CreateTicketAsync(
        CreateTicketRequest req)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("api/tickets", req);
            if (!res.IsSuccessStatusCode)
                return (null, await ReadErrorAsync(res));

            return (await res.Content.ReadFromJsonAsync<TicketDetailDto>(Opts), null);
        }
        catch (Exception ex) { return (null, ex.Message); }
    }

    public async Task<(TicketDetailDto? Ticket, string? Error)> UpdateStatusAsync(
        Guid id, int status, string? note = null)
    {
        try
        {
            var req = new UpdateStatusRequest { Status = status, Note = note };
            var res = await _http.PutAsJsonAsync($"api/tickets/{id}/status", req);
            if (!res.IsSuccessStatusCode)
                return (null, await ReadErrorAsync(res));

            return (await res.Content.ReadFromJsonAsync<TicketDetailDto>(Opts), null);
        }
        catch (Exception ex) { return (null, ex.Message); }
    }

    public async Task<(MessageItem? Message, string? Error)> AddMessageAsync(
        Guid id, string content, bool isInternal = false)
    {
        try
        {
            var req = new AddMessageRequest { Content = content, IsInternal = isInternal };
            var res = await _http.PostAsJsonAsync($"api/tickets/{id}/messages", req);
            if (!res.IsSuccessStatusCode)
                return (null, await ReadErrorAsync(res));

            return (await res.Content.ReadFromJsonAsync<MessageItem>(Opts), null);
        }
        catch (Exception ex) { return (null, ex.Message); }
    }

    public async Task<AnalyticsSummary?> GetAnalyticsAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<AnalyticsSummary>(
                "api/analytics/summary", Opts);
        }
        catch { return null; }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage res)
    {
        try
        {
            var err = await res.Content.ReadFromJsonAsync<ApiError>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return err?.Error ?? $"HTTP {(int)res.StatusCode}";
        }
        catch { return $"HTTP {(int)res.StatusCode}"; }
    }
}