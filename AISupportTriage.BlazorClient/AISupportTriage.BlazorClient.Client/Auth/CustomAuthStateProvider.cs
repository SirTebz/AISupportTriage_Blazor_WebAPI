using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AISupportTriage.BlazorClient.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace AISupportTriage.BlazorClient.Client.Auth;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly LocalStorageService _storage;
    private readonly HttpClient _http;
    private const string TokenKey = "auth_token";

    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    public CustomAuthStateProvider(LocalStorageService storage, HttpClient http)
    {
        _storage = storage;
        _http = http;
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
                await ClearAsync();
                return Anonymous;
            }

            SetHeader(token);

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
        SetHeader(token);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var identity = new ClaimsIdentity(jwt.Claims, "jwt");

        NotifyAuthenticationStateChanged(
            Task.FromResult(
                new AuthenticationState(new ClaimsPrincipal(identity))));
    }

    public async Task MarkLoggedOutAsync()
    {
        await ClearAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    public async Task<string?> GetTokenAsync()
        => await _storage.GetAsync(TokenKey);

    private void SetHeader(string token)
    {
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    private async Task ClearAsync()
    {
        await _storage.RemoveAsync(TokenKey);
        _http.DefaultRequestHeaders.Authorization = null;
    }
}