namespace BlackMarketEmpire.Services;

/// <summary>Key/value storage on the device. Backed by IndexedDB in the browser.</summary>
public interface ILocalStore
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value);
    Task DeleteAsync(string key);
}
