using BlackMarketEmpire.Models;

namespace BlackMarketEmpire.Services;

/// <summary>
/// The current run as the UI sees it. Applies actions through the run's <see cref="GameEngine"/>,
/// saves after every action (including the random-number state, so a resumed run carries on exactly),
/// plays sound and vibration, and records scores, career stats and achievements when the run ends.
/// </summary>
public sealed class GameSession(
    EngineFactory engines,
    ILocalStore store,
    HighScoreService highScores,
    ProgressService? progress = null,
    IFeedback? feedback = null)
{
    public const string StorageKey = "current-run";

    /// <summary>The engine for the current run's difficulty (Normal when there's no run).</summary>
    public GameEngine Engine => engines.For(State?.Difficulty ?? Difficulty.Normal).Engine;
    public GameState? State { get; private set; }
    public int? FinalRank { get; private set; }
    /// <summary>Achievements unlocked by the run that just ended.</summary>
    public IReadOnlyList<Achievement> NewAchievements { get; private set; } = [];
    public bool HasActiveRun => State is { IsOver: false };

    public event Action? Changed;

    /// <summary>Today's daily challenge id (local date).</summary>
    public static string TodaysChallenge => DateTime.Now.ToString("yyyy-MM-dd");

    public async Task LoadAsync()
    {
        State ??= await store.GetAsync<GameState>(StorageKey);
    }

    public async Task StartNewAsync(Difficulty difficulty = Difficulty.Normal, bool daily = false)
    {
        if (daily) difficulty = Difficulty.Normal;
        var (engine, random) = engines.For(difficulty);
        var challenge = daily ? TodaysChallenge : null;
        random.State = challenge is null ? RunRandom.RandomSeed() : RunRandom.SeedFrom($"daily-{challenge}");

        var state = engine.NewGame();
        state.Difficulty = difficulty;
        state.ChallengeDate = challenge;
        state.RngState = random.State;
        State = state;
        FinalRank = null;
        NewAchievements = [];
        await SaveAsync();
    }

    public Task<ActionResult> BuyAsync(string goodId, int qty) => ApplyAsync((e, s) => e.Buy(s, goodId, qty), SoundCue.Buy);
    public Task<ActionResult> SellAsync(string goodId, int qty) => ApplyAsync((e, s) => e.Sell(s, goodId, qty), SoundCue.Sell);
    public Task<ActionResult> TravelAsync(string cityId, TravelMethod? method = null) => ApplyAsync((e, s) => e.Travel(s, cityId, method), SoundCue.Travel);
    public Task<ActionResult> BuyGearAsync(string gearId) => ApplyAsync((e, s) => e.BuyGear(s, gearId), SoundCue.Buy);
    public Task<ActionResult> UseFirstAidAsync() => ApplyAsync((e, s) => e.UseFirstAid(s), SoundCue.Tip);
    public Task<ActionResult> ShipAsync(string goodId, int qty, string toCityId, string courierId) =>
        ApplyAsync((e, s) => e.Ship(s, goodId, qty, toCityId, courierId), SoundCue.Travel);
    public Task<ActionResult> CollectAsync() => ApplyAsync((e, s) => e.Collect(s), SoundCue.Buy);
    public Task<ActionResult> BorrowAsync(string lenderId, long amount) => ApplyAsync((e, s) => e.Borrow(s, lenderId, amount), SoundCue.Money);
    public Task<ActionResult> RepayLoanAsync(Guid loanId, long amount) => ApplyAsync((e, s) => e.RepayLoan(s, loanId, amount), SoundCue.Money);
    public Task<ActionResult> DepositAsync(long amount) => ApplyAsync((e, s) => e.Deposit(s, amount), SoundCue.Money);
    public Task<ActionResult> WithdrawAsync(long amount) => ApplyAsync((e, s) => e.Withdraw(s, amount), SoundCue.Money);
    public Task<ActionResult> RentStashAsync(StashTier tier) => ApplyAsync((e, s) => e.RentStash(s, tier), SoundCue.Money);
    public Task<ActionResult> StoreAsync(string goodId, int qty) => ApplyAsync((e, s) => e.Store(s, goodId, qty), SoundCue.Buy);
    public Task<ActionResult> TakeFromStashAsync(string goodId, int qty) => ApplyAsync((e, s) => e.TakeFromStash(s, goodId, qty), SoundCue.Buy);
    public Task<ActionResult> CheckRumourAsync(Guid newsId, InformantTier tier) => ApplyAsync((e, s) => e.CheckRumour(s, newsId, tier), SoundCue.Tip);
    public Task<ActionResult> BuyTipOffAsync() => ApplyAsync((e, s) => e.BuyTipOff(s), SoundCue.Tip);
    public Task<ActionResult> BuyContactReportAsync(string cityId) => ApplyAsync((e, s) => e.BuyContactReport(s, cityId), SoundCue.Tip);
    public Task<ActionResult> ArrangeShortageAsync(string cityId, string goodId) => ApplyAsync((e, s) => e.ArrangeShortage(s, cityId, goodId), SoundCue.Tip);
    public Task<ActionResult> WaitAsync() => ApplyAsync((e, s) => e.Wait(s), null);
    public Task<ActionResult> RepayAsync(long amount) => ApplyAsync((e, s) => e.RepayDebt(s, amount), SoundCue.Money);
    public Task<ActionResult> EndRunAsync() => ApplyAsync((e, s) => e.EndRun(s), null);
    public Task<ActionResult> ResolveEncounterAsync(EncounterChoice choice) => ApplyAsync((e, s) => e.ResolveEncounter(s, choice), null);

    private async Task<ActionResult> ApplyAsync(Func<GameEngine, GameState, ActionResult> action, SoundCue? successCue)
    {
        if (State is null) return ActionResult.Fail("No run in progress.");

        var state = State;
        var (engine, random) = engines.For(state.Difficulty);
        random.State = state.RngState;

        var before = (state.IsOver, Encounter: state.PendingEncounter is not null, state.Stats.Arrests, state.Stats.Escapes, state.Health);
        var result = action(engine, state);
        state.RngState = random.State;

        if (!result.Success)
        {
            feedback?.Play(SoundCue.Error);
            return result;
        }

        state.Stats.LowestHealth = Math.Min(state.Stats.LowestHealth, state.Health);
        if (state.TotalDebt == 0 && state.Stats.DebtFreeDay is null) state.Stats.DebtFreeDay = state.Day;

        var cue = successCue;
        if (state.EndReason == EndReason.Killed) cue = SoundCue.Death;
        else if (state.Stats.Arrests > before.Arrests) cue = SoundCue.Arrest;
        else if (!before.Encounter && state.PendingEncounter is not null) cue = SoundCue.Siren;
        else if (state.Health < before.Health) cue = SoundCue.Hurt;
        else if (state.Stats.Escapes > before.Escapes) cue = SoundCue.Escape;
        else if (!before.IsOver && state.IsOver) cue = SoundCue.Finish;

        if (!before.IsOver && state.IsOver)
        {
            var score = engine.Score(state);
            FinalRank = await highScores.AddAsync(new HighScore(
                state.RunId, score, DateTime.UtcNow, GameCatalog.City(state.CityId).Name,
                state.EndReason, state.StartedUtc, state.Day, state.Stats.BestTrade, state.Difficulty, state.ChallengeDate));
            if (progress is not null)
            {
                NewAchievements = await progress.RecordRunAsync(state, score);
            }
        }

        // A finished run that unlocked something gets the achievement fanfare instead (death keeps its own sound).
        if (!before.IsOver && state.IsOver && NewAchievements.Count > 0 && cue != SoundCue.Death) cue = SoundCue.Achievement;

        await SaveAsync();
        if (cue is { } c) feedback?.Play(c);
        Changed?.Invoke();
        return result;
    }

    private Task SaveAsync() => State is null ? Task.CompletedTask : store.SetAsync(StorageKey, State);
}
