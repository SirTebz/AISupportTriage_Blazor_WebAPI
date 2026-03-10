//using AISupportTriage.BlazorClient.Auth;
//using AISupportTriage.BlazorClient.Models;
//using System.Net.Http.Json;
//using System.Text.Json;

//namespace AISupportTriage.BlazorClient.Services;

//public class AuthService
//{
//    private readonly HttpClient _http;
//    private readonly CustomAuthStateProvider _authProvider;

//    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

//    public AuthService(HttpClient http, CustomAuthStateProvider authProvider)
//    {
//        _http = http;
//        _authProvider = authProvider;
//    }

//    public async Task<(bool Success, string? Error)> LoginAsync(LoginRequest request)
//    {
//        try
//        {
//            var response = await _http.PostAsJsonAsync("api/auth/login", request);
//            if (!response.IsSuccessStatusCode)
//            {
//                var err = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOpts);
//                return (false, err?.Error ?? "Login failed.");
//            }

//            var result = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts);
//            if (result == null) return (false, "Invalid response from server.");

//            await _authProvider.MarkUserAsAuthenticated(result.Token);
//            return (true, null);
//        }
//        catch (Exception ex)
//        {
//            return (false, $"Connection error: {ex.Message}");
//        }
//    }

//    public async Task<(bool Success, string? Error)> RegisterAsync(RegisterRequest request)
//    {
//        try
//        {
//            var response = await _http.PostAsJsonAsync("api/auth/register", request);
//            if (!response.IsSuccessStatusCode)
//            {
//                var err = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOpts);
//                return (false, err?.Error ?? "Registration failed.");
//            }

//            var result = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts);
//            if (result == null) return (false, "Invalid response from server.");

//            await _authProvider.MarkUserAsAuthenticated(result.Token);
//            return (true, null);
//        }
//        catch (Exception ex)
//        {
//            return (false, $"Connection error: {ex.Message}");
//        }
//    }

//    public async Task LogoutAsync()
//    {
//        await _authProvider.MarkUserAsLoggedOut();
//    }
//}

//public class ErrorResponse { public string? Error { get; set; } }