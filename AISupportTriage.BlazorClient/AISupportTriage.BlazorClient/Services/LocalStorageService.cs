using Microsoft.JSInterop;

namespace AISupportTriage.BlazorClient.Services;

/// <summary>
/// Wraps browser localStorage via JSInterop.
/// No NuGet package required.
/// </summary>
public class LocalStorageService
{
    private readonly IJSRuntime _js;

    public LocalStorageService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task<string?> GetAsync(string key)
    {
        try
        {
            return await _js.InvokeAsync<string?>("localStorage.getItem", key);
        }
        catch
        {
            return null;
        }
    }

    public async Task SetAsync(string key, string value)
    {
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", key, value);
        }
        catch { /* ignore */ }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", key);
        }
        catch { /* ignore */ }
    }
}