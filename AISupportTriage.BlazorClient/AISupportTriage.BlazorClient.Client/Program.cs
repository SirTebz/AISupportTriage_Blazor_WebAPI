using AISupportTriage.BlazorClient.Client.Auth;
using AISupportTriage.BlazorClient.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// ── HTTP Client ───────────────────────────────────────────────────────────────
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7100";

builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
});

// ── Local Storage (JSInterop — no NuGet needed) ───────────────────────────────
builder.Services.AddScoped<LocalStorageService>();

// ── Auth ──────────────────────────────────────────────────────────────────────
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<CustomAuthStateProvider>());

// ── App Services ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<AuthClientService>();
builder.Services.AddScoped<TicketApiService>();
builder.Services.AddScoped<SignalRService>();

await builder.Build().RunAsync();