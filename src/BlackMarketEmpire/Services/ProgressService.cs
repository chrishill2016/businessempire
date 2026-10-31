using BlackMarketEmpire.Models;

namespace BlackMarketEmpire.Services;

/// <summary>Career statistics and achievements, kept on the device.</summary>
public sealed class ProgressService(ILocalStore store)
{
    public const string CareerKey = "career";
    public const string AchievementsKey = "achievements";

    public static IReadOnlyList<Achievement> All { get; } =
    [
        new("first_run", "First Run", "🏁", "Finish a run, any way at all."),
        new("in_black", "In the Black", "💷", "End a run with a positive score."),
        new("high_roller", "High Roller", "🎰", "Score £100,000 or more."),
        new("millionaire", "Millionaire", "💰", "Score £1,000,000 or more."),
        new("debt_free", "Debt Free", "🧾", "Owe nothing to anyone before day 10."),
        new("globetrotter", "Globetrotter", "🌍", "Visit all 14 cities in one run."),
        new("clean", "Clean Record", "😇", "Last all 30 days without ever being stopped."),
        new("escape_artist", "Escape Artist", "🏃", "Get away from the police 3 times in one run."),
        new("jailbird", "Jailbird", "🔒", "Get arrested. It happens."),
        new("close_call", "Close Call", "🩸", "Survive a run after dropping to 10 health or less."),
        new("hard_lesson", "Hard Lesson", "☠️", "Get killed. Crime doesn't pay."),
        new("big_score", "Big Score", "💎", "Make £50,000 profit on a single sale."),
        new("old_money", "Old Money", "🏦", "Finish with £50,000 or more in the bank."),
        new("favourite", "Favourite Customer", "⭐", "Reach ★★★★★ with any lender."),
        new("warehouse", "Warehouse King", "🏭", "Rent a warehouse."),
        new("fixer", "Made It Happen", "🧩", "Have a fixer's shortage come off."),
        new("wheeler_dealer", "Wheeler-Dealer", "🤝", "Make 50 trades in one run."),
        new("hard_case", "Hard Case", "🔥", "Finish a Hard run with a positive score."),
        new("daily", "Daily Grind", "📅", "Complete a daily challenge."),
    ];

    public static Achievement Get(string id) => All.First(a => a.Id == id);

    /// <summary>Which achievements this finished run earns.</summary>
    public static IEnumerable<string> Earned(GameState state, long score)
    {
        var stats = state.Stats;
        var survived = state.EndReason != EndReason.Killed;
        yield return "first_run";
        if (score > 0) yield return "in_black";
        if (score >= 100_000) yield return "high_roller";
        if (score >= 1_000_000) yield return "millionaire";
        if (stats.DebtFreeDay is < 10) yield return "debt_free";
        if (GameCatalog.Cities.All(c => stats.CitiesVisited.Contains(c.Id))) yield return "globetrotter";
        if (state.EndReason == EndReason.Finished && stats.Stops == 0) yield return "clean";
        if (stats.Escapes >= 3) yield return "escape_artist";
        if (stats.Arrests > 0) yield return "jailbird";
        if (survived && stats.LowestHealth <= 10) yield return "close_call";
        if (!survived) yield return "hard_lesson";
        if (stats.BestTrade >= 50_000) yield return "big_score";
        if (state.Bank >= 50_000) yield return "old_money";
        if (state.Reputation.Values.Any(r => FinanceService.Stars(r) == 5)) yield return "favourite";
        if (state.Stashes.Any(s => s.Tier == StashTier.Warehouse)) yield return "warehouse";
        if (stats.FixerSuccesses > 0) yield return "fixer";
        if (stats.Trades >= 50) yield return "wheeler_dealer";
        if (state.Difficulty == Difficulty.Hard && score > 0 && survived) yield return "hard_case";
        if (state.IsDaily) yield return "daily";
    }

    public async Task<CareerStats> GetCareerAsync() => await store.GetAsync<CareerStats>(CareerKey) ?? new CareerStats();

    /// <summary>Unlocked achievement ids with when they were unlocked.</summary>
    public async Task<Dictionary<string, DateTime>> GetUnlockedAsync() =>
        await store.GetAsync<Dictionary<string, DateTime>>(AchievementsKey) ?? [];

    /// <summary>Adds a finished run to the career totals. Returns any achievements unlocked for the first time.</summary>
    public async Task<IReadOnlyList<Achievement>> RecordRunAsync(GameState state, long score)
    {
        var career = await GetCareerAsync();
        var stats = state.Stats;
        career.RunsPlayed++;
        switch (state.EndReason)
        {
            case EndReason.Killed: career.Killed++; break;
            case EndReason.Retired: career.Retired++; break;
            default: career.Finished++; break;
        }
        career.BestScore = career.BestScore is { } best ? Math.Max(best, score) : score;
        career.BestByDifficulty[state.Difficulty] = career.BestByDifficulty.TryGetValue(state.Difficulty, out var b) ? Math.Max(b, score) : score;
        career.TotalScore += score;
        career.TotalProfit += stats.TotalProfit;
        career.BestTrade = Math.Max(career.BestTrade, stats.BestTrade);
        career.Trades += stats.Trades;
        career.Stops += stats.Stops;
        career.Arrests += stats.Arrests;
        career.Escapes += stats.Escapes;
        career.DaysPlayed += state.Day;
        career.Borrowed += stats.Borrowed;
        foreach (var city in stats.CitiesVisited.Where(c => !career.CitiesVisited.Contains(c))) career.CitiesVisited.Add(city);
        if (state.ChallengeDate is { } date)
        {
            career.DailyBest[date] = career.DailyBest.TryGetValue(date, out var d) ? Math.Max(d, score) : score;
        }
        career.LastPlayedUtc = DateTime.UtcNow;
        await store.SetAsync(CareerKey, career);

        var unlocked = await GetUnlockedAsync();
        var fresh = Earned(state, score).Where(id => !unlocked.ContainsKey(id)).ToList();
        foreach (var id in fresh) unlocked[id] = DateTime.UtcNow;
        if (fresh.Count > 0) await store.SetAsync(AchievementsKey, unlocked);
        return fresh.Select(Get).ToList();
    }

    public async Task ClearAsync()
    {
        await store.DeleteAsync(CareerKey);
        await store.DeleteAsync(AchievementsKey);
    }
}
