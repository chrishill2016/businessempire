using BlackMarketEmpire.Models;
using Microsoft.JSInterop;

namespace BlackMarketEmpire.Services;

/// <summary>Sound effects and vibration.</summary>
public interface IFeedback
{
    void Play(SoundCue cue);
}

/// <summary>Sound and vibration settings, saved on the device.</summary>
public sealed class SettingsService(ILocalStore store)
{
    public const string StorageKey = "settings";
    private AppSettings? _settings;

    public AppSettings Current => _settings ?? new AppSettings();

    public async Task<AppSettings> LoadAsync() => _settings ??= await store.GetAsync<AppSettings>(StorageKey) ?? new AppSettings();

    public async Task SaveAsync(AppSettings settings)
    {
        _settings = settings;
        await store.SetAsync(StorageKey, settings);
    }
}

/// <summary>Plays synthesised sounds (Web Audio, no files to download) and vibrates via <c>wwwroot/js/sfx.js</c>.</summary>
public sealed class JsFeedback(IJSRuntime js, SettingsService settings) : IFeedback
{
    public void Play(SoundCue cue) => _ = PlayAsync(cue);

    private async Task PlayAsync(SoundCue cue)
    {
        try
        {
            var s = await settings.LoadAsync();
            if (!s.SoundOn && !s.VibrationOn) return;
            await js.InvokeVoidAsync("bmeSfx.play", cue.ToString().ToLowerInvariant(), s.SoundOn, s.VibrationOn);
        }
        catch (JSException)
        {
            // Sound is a nice-to-have; never let it break the game.
        }
    }
}
