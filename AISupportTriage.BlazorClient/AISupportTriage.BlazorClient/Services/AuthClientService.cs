using System.Net.Http.Json;
using System.Text.Json;
using AISupportTriage.BlazorClient.Auth;
using AISupportTriage.BlazorClient.Models;

namespace AISupportTriage.BlazorClient.Services;

public class AuthClientService
{
    private readonly HttpClient _http;
    private readonly CustomAuthStateProvider _authProvider;

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    public AuthClientService(HttpClient http, CustomAuthStateProvider authProvider)
    {
        _http = http;
        _authProvider = authProvider;
    }

    public async Task<(bool Success, string? Error)> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/auth/login", request);

            if (!response.IsSuccessStatusCode)
            {
                var err = await TryReadErrorAsync(response);
                return (false, err);
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts);
            if (result == null) return (false, "Empty response from server.");

            await _authProvider.MarkAuthenticatedAsync(result.Token);
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

    public async Task<(bool Success, string? Error)> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/auth/register", request);

            if (!response.IsSuccessStatusCode)
            {
                var err = await TryReadErrorAsync(response);
                return (false, err);
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts);
            if (result == null) return (false, "Empty response from server.");

            await _authProvider.MarkAuthenticatedAsync(result.Token);
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
        => await _authProvider.MarkLoggedOutAsync();

    private static async Task<string> TryReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var err = await response.Content
                .ReadFromJsonAsync<ApiError>(new JsonSerializerOptions
                { PropertyNameCaseInsensitive = true });
            return err?.Error ?? $"Error {(int)response.StatusCode}";
        }
        catch
        {
            return $"Error {(int)response.StatusCode}";
        }
    }
}