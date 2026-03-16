using AISupportTriage.BlazorClient.Client.Auth;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;

namespace AISupportTriage.BlazorClient.Client.Services;

public class SignalRService : IAsyncDisposable
{
    private readonly CustomAuthStateProvider _auth;
    private readonly string _apiBaseUrl;
    private HubConnection? _hub;

    public event Action<string, string>? OnTicketCreated;
    public event Action<string>? OnTicketUpdated;
    public event Action<string>? OnNewMessage;
    public event Action<string[]>? OnSlaWarning;

    public bool IsConnected =>
        _hub?.State == HubConnectionState.Connected;

    public SignalRService(CustomAuthStateProvider auth, IConfiguration config)
    {
        _auth = auth;
        _apiBaseUrl = config["ApiBaseUrl"] ?? "https://localhost:7100";
    }

    public async Task StartAsync()
    {
        if (_hub != null) return;

        var token = await _auth.GetTokenAsync();

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
        catch { /* SignalR failure won't break the app */ }
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