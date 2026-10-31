using System.Text.Json;
using Microsoft.JSInterop;

namespace BlackMarketEmpire.Services;

/// <summary><see cref="ILocalStore"/> backed by IndexedDB via <c>wwwroot/js/idb.js</c>.</summary>
public sealed class IndexedDbStore(IJSRuntime js) : ILocalStore, IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private IJSObjectReference? _module;

    public async Task<T?> GetAsync<T>(string key)
    {
        var module = await GetModuleAsync();
        var json = await module.InvokeAsync<string?>("get", key);
        return json is null ? default : JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public async Task SetAsync<T>(string key, T value)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("set", key, JsonSerializer.Serialize(value, JsonOptions));
    }

    public async Task DeleteAsync(string key)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("del", key);
    }

    private async Task<IJSObjectReference> GetModuleAsync() =>
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/idb.js");

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try { await _module.DisposeAsync(); } catch (JSDisconnectedException) { }
        }
    }
}
