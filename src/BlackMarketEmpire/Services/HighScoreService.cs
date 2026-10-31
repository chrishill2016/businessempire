using BlackMarketEmpire.Models;

namespace BlackMarketEmpire.Services;

/// <summary>Keeps the top scores on this device.</summary>
public sealed class HighScoreService(ILocalStore store)
{
    public const string StorageKey = "highscores";
    public const int MaxEntries = 100;

    public async Task<IReadOnlyList<HighScore>> GetTopAsync() =>
        await store.GetAsync<List<HighScore>>(StorageKey) ?? [];

    /// <summary>Records a score. Returns its 1-based rank, or null if it didn't make the table.</summary>
    public async Task<int?> AddAsync(HighScore score)
    {
        var scores = (await GetTopAsync()).Where(s => s.RunId != score.RunId).ToList();
        scores.Add(score);
        var top = scores
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.AchievedUtc)
            .Take(MaxEntries)
            .ToList();
        await store.SetAsync(StorageKey, top);

        var index = top.FindIndex(s => s.RunId == score.RunId);
        return index < 0 ? null : index + 1;
    }

    public Task ClearAsync() => store.DeleteAsync(StorageKey);
}
