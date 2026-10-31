using System.Text.Json;

namespace BlackMarketEmpire.Tests.Fakes;

/// <summary>ILocalStore that round-trips through JSON, like the real IndexedDB store.</summary>
public sealed class InMemoryStore : ILocalStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly Dictionary<string, string> _data = [];

    public Task<T?> GetAsync<T>(string key) =>
        Task.FromResult(_data.TryGetValue(key, out var json) ? JsonSerializer.Deserialize<T>(json, Json) : default);

    public Task SetAsync<T>(string key, T value)
    {
        _data[key] = JsonSerializer.Serialize(value, Json);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string key)
    {
        _data.Remove(key);
        return Task.CompletedTask;
    }

    public bool Contains(string key) => _data.ContainsKey(key);
}
