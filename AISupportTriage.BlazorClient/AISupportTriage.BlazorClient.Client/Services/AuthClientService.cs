using System.Net.Http.Json;
using System.Text.Json;
using AISupportTriage.BlazorClient.Client.Auth;
using AISupportTriage.BlazorClient.Client.Models;

namespace AISupportTriage.BlazorClient.Client.Services;

public class AuthClientService
{
    private readonly HttpClient _http;
    private readonly CustomAuthStateProvider _auth;

    private static readonly JsonSerializerOptions Opts =
        new() { PropertyNameCaseInsensitive = true };

    public AuthClientService(HttpClient http, CustomAuthStateProvider auth)
    {
        _http = http;
        _auth = auth;
    }

    public async Task<(bool Success, string? Error)> LoginAsync(LoginRequest req)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("api/auth/login", req);
            if (!res.IsSuccessStatusCode)
                return (false, await ReadErrorAsync(res));

            var data = await res.Content.ReadFromJsonAsync<AuthResponse>(Opts);
            if (data == null) return (false, "Empty response.");

            await _auth.MarkAuthenticatedAsync(data.Token);
            return (true, null);
        }
        catch (HttpRequestException)
        {
            return (false, "Cannot connect to server. Is the API running?");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? Error)> RegisterAsync(RegisterRequest req)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("api/auth/register", req);
            if (!res.IsSuccessStatusCode)
                return (false, await ReadErrorAsync(res));

            var data = await res.Content.ReadFromJsonAsync<AuthResponse>(Opts);
            if (data == null) return (false, "Empty response.");

            await _auth.MarkAuthenticatedAsync(data.Token);
            return (true, null);
        }
        catch (HttpRequestException)
        {
            return (false, "Cannot connect to server. Is the API running?");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task LogoutAsync()
        => await _auth.MarkLoggedOutAsync();

    private static async Task<string> ReadErrorAsync(HttpResponseMessage res)
    {
        try
        {
            var err = await res.Content.ReadFromJsonAsync<ApiError>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return err?.Error ?? $"HTTP {(int)res.StatusCode}";
        }
        catch
        {
            return $"HTTP {(int)res.StatusCode}";
        }
    }
}