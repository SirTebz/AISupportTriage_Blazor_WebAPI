using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AISupportTriage.BlazorClient.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace AISupportTriage.BlazorClient.Auth;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly LocalStorageService _storage;
    private readonly HttpClient _httpClient;
    private const string TokenKey = "auth_token";

    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    public CustomAuthStateProvider(LocalStorageService storage, HttpClient httpClient)
    {
        _storage = storage;
        _httpClient = httpClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _storage.GetAsync(TokenKey);

            if (string.IsNullOrWhiteSpace(token))
                return Anonymous;

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            if (jwt.ValidTo < DateTime.UtcNow)
            {
                await ClearTokenAsync();
                return Anonymous;
            }

            SetAuthHeader(token);

            var identity = new ClaimsIdentity(jwt.Claims, "jwt");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch
        {
            return Anonymous;
        }
    }

    public async Task MarkAuthenticatedAsync(string token)
    {
        await _storage.SetAsync(TokenKey, token);
        SetAuthHeader(token);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var identity = new ClaimsIdentity(jwt.Claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(user)));
    }

    public async Task MarkLoggedOutAsync()
    {
        await ClearTokenAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    public async Task<string?> GetTokenAsync()
        => await _storage.GetAsync(TokenKey);

    private void SetAuthHeader(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    private async Task ClearTokenAsync()
    {
        await _storage.RemoveAsync(TokenKey);
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }
}