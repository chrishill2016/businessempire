using Microsoft.JSInterop;

namespace BlackMarketEmpire.Services;

/// <summary>Tracks online/offline state and whether a new app version is waiting (via <c>wwwroot/js/app.js</c>).</summary>
public sealed class AppStatusService(IJSRuntime js) : IAsyncDisposable
{
    private DotNetObjectReference<AppStatusService>? _ref;
    private bool _started;

    public bool IsOnline { get; private set; } = true;
    public bool UpdateAvailable { get; private set; }

    public event Action? Changed;
    public event Action? UpdateReady;

    public async Task StartAsync()
    {
        if (_started) return;
        _started = true;
        _ref = DotNetObjectReference.Create(this);
        IsOnline = await js.InvokeAsync<bool>("appStatus.init", _ref);
        Changed?.Invoke();
    }

    public ValueTask ApplyUpdateAsync() => js.InvokeVoidAsync("appStatus.applyUpdate");

    [JSInvokable]
    public void OnOnlineChanged(bool online)
    {
        IsOnline = online;
        Changed?.Invoke();
    }

    [JSInvokable]
    public void OnUpdateAvailable()
    {
        UpdateAvailable = true;
        Changed?.Invoke();
        UpdateReady?.Invoke();
    }

    public ValueTask DisposeAsync()
    {
        _ref?.Dispose();
        return ValueTask.CompletedTask;
    }
}
