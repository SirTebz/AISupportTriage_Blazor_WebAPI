using AISupportTriage.BlazorClient.Auth;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;

namespace AISupportTriage.BlazorClient.Services;

public class SignalRService : IAsyncDisposable
{
    private readonly CustomAuthStateProvider _authProvider;
    private readonly string _apiBaseUrl;
    private HubConnection? _hub;

    public event Action<string, string>? OnTicketCreated;
    public event Action<string>? OnTicketUpdated;
    public event Action<string>? OnNewMessage;
    public event Action<string[]>? OnSlaWarning;

    public bool IsConnected =>
        _hub?.State == HubConnectionState.Connected;

    public SignalRService(CustomAuthStateProvider authProvider, IConfiguration config)
    {
        _authProvider = authProvider;
        _apiBaseUrl = config["ApiBaseUrl"] ?? "https://localhost:7100";
    }

    public async Task StartAsync()
    {
        if (_hub != null) return;

        var token = await _authProvider.GetTokenAsync();

        _hub = new HubConnectionBuilder()
            .WithUrl($"{_apiBaseUrl}/hubs/tickets", options =>
            {
                if (!string.IsNullOrWhiteSpace(token))
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .WithAutomaticReconnect()
            .Build();

        _hub.On<string, string>("TicketCreated",
            (id, title) => OnTicketCreated?.Invoke(id, title));

        _hub.On<string>("TicketUpdated",
            id => OnTicketUpdated?.Invoke(id));

        _hub.On<string>("NewMessage",
            id => OnNewMessage?.Invoke(id));

        _hub.On<string[]>("SlaWarning",
            ids => OnSlaWarning?.Invoke(ids));

        try { await _hub.StartAsync(); }
        catch { /* SignalR is optional — app still works without it */ }
    }

    public async Task StopAsync()
    {
        if (_hub != null)
            await _hub.StopAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub != null)
            await _hub.DisposeAsync();
    }
}