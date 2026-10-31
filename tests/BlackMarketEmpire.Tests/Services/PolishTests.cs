using BlackMarketEmpire.Tests.Fakes;

namespace BlackMarketEmpire.Tests.Services;

public class RunRandomTests
{
    [Fact]
    public void Same_seed_same_sequence()
    {
        var a = new RunRandom(42);
        var b = new RunRandom(42);

        Assert.Equal(Enumerable.Range(0, 20).Select(_ => a.NextDouble()), Enumerable.Range(0, 20).Select(_ => b.NextDouble()));
    }

    [Fact]
    public void Saving_and_restoring_state_continues_the_sequence()
    {
        var a = new RunRandom(7);
        a.NextDouble();
        var saved = a.State;
        var expected = a.NextDouble();

        Assert.Equal(expected, new RunRandom(saved).NextDouble());
    }

    [Fact]
    public void Values_stay_in_range()
    {
        var r = new RunRandom(1);
        for (var i = 0; i < 10_000; i++)
        {
            Assert.InRange(r.NextDouble(), 0, 0.9999999999);
            Assert.InRange(r.Next(3, 9), 3, 8);
        }
        Assert.Equal(5, r.Next(5, 5));
    }

    [Fact]
    public void Seeds_from_text_are_stable_and_distinct()
    {
        Assert.Equal(RunRandom.SeedFrom("daily-2026-10-31"), RunRandom.SeedFrom("daily-2026-10-31"));
        Assert.NotEqual(RunRandom.SeedFrom("daily-2026-10-31"), RunRandom.SeedFrom("daily-2026-11-01"));
    }
}

public class DifficultyTests
{
    [Fact]
    public void Presets_scale_the_challenge()
    {
        var easy = GameRules.For(Difficulty.Easy);
        var normal = GameRules.For(Difficulty.Normal);
        var hard = GameRules.For(Difficulty.Hard);

        Assert.Same(GameRules.Default, normal);
        Assert.True(easy.StartingCash > normal.StartingCash && normal.StartingCash > hard.StartingCash);
        Assert.True(easy.StartingDebt < normal.StartingDebt && normal.StartingDebt < hard.StartingDebt);
        Assert.True(easy.DailyInterest < normal.DailyInterest && normal.DailyInterest < hard.DailyInterest);
        Assert.True(easy.BaseStopChance < hard.BaseStopChance);
        Assert.True(easy.Capacity > hard.Capacity);
    }

    [Fact]
    public async Task Starting_a_hard_run_uses_hard_rules()
    {
        var store = new InMemoryStore();
        var session = new GameSession(new EngineFactory(), store, new HighScoreService(store));

        await session.StartNewAsync(Difficulty.Hard);

        Assert.Equal(Difficulty.Hard, session.State!.Difficulty);
        Assert.Equal(1_500, session.State.Cash);
        Assert.Equal(7_500, session.State.Debt);
        Assert.Equal(80, session.State.Capacity);
        Assert.Same(GameRules.For(Difficulty.Hard), session.Engine.Rules);
    }
}

public class DailyChallengeTests
{
    private static GameSession Session(InMemoryStore store) => new(new EngineFactory(), store, new HighScoreService(store));

    [Fact]
    public async Task Everyone_gets_the_same_start()
    {
        var a = Session(new InMemoryStore());
        var b = Session(new InMemoryStore());

        await a.StartNewAsync(daily: true);
        await b.StartNewAsync(daily: true);

        Assert.Equal(GameSession.TodaysChallenge, a.State!.ChallengeDate);
        Assert.Equal(Difficulty.Normal, a.State.Difficulty);
        Assert.Equal(a.State.Market, b.State!.Market);
        Assert.Equal(a.State.News.Select(n => n.Headline), b.State.News.Select(n => n.Headline));
    }

    [Fact]
    public async Task Daily_ignores_the_chosen_difficulty()
    {
        var a = Session(new InMemoryStore());

        await a.StartNewAsync(Difficulty.Hard, daily: true);

        Assert.Equal(Difficulty.Normal, a.State!.Difficulty);
    }

    [Fact]
    public async Task Same_moves_play_out_the_same_even_across_an_app_restart()
    {
        var straight = Session(new InMemoryStore());
        await straight.StartNewAsync(daily: true);
        for (var i = 0; i < 4; i++) await straight.WaitAsync();

        var store = new InMemoryStore();
        var first = Session(store);
        await first.StartNewAsync(daily: true);
        await first.WaitAsync();
        await first.WaitAsync();
        var resumed = Session(store); // app closed and reopened
        await resumed.LoadAsync();
        await resumed.WaitAsync();
        await resumed.WaitAsync();

        Assert.Equal(straight.State!.Day, resumed.State!.Day);
        Assert.Equal(straight.State.Market, resumed.State.Market);
        Assert.Equal(straight.State.News.Select(n => n.Headline), resumed.State.News.Select(n => n.Headline));
        Assert.Equal(straight.State.RngState, resumed.State.RngState);
    }

    [Fact]
    public async Task Normal_runs_are_not_all_the_same()
    {
        var a = Session(new InMemoryStore());
        var b = Session(new InMemoryStore());

        await a.StartNewAsync();
        await b.StartNewAsync();

        Assert.NotEqual(a.State!.RngState, b.State!.RngState);
        Assert.Null(a.State.ChallengeDate);
    }
}

public class RunStatsTrackingTests
{
    [Fact]
    public void Trades_profit_and_cities_are_tracked()
    {
        var engine = TestGame.Engine();
        var state = TestGame.StateWithMarket(engine, new() { ["cigs"] = 10 });
        engine.Buy(state, "cigs", 20);
        state.Market["cigs"] = 15;
        engine.Sell(state, "cigs", 20);
        engine.Travel(state, "amsterdam", TravelMethod.Train);

        Assert.Equal(2, state.Stats.Trades);
        Assert.Equal(100, state.Stats.TotalProfit);
        Assert.Equal(["london", "amsterdam"], state.Stats.CitiesVisited);
    }

    [Fact]
    public async Task Session_tracks_debt_free_day_and_lowest_health()
    {
        var store = new InMemoryStore();
        var session = new GameSession(TestGame.Factory(), store, new HighScoreService(store));
        await session.StartNewAsync();
        session.State!.Cash = 10_000;
        session.State.Health = 40;

        await session.RepayAsync(5_000);

        Assert.Equal(1, session.State.Stats.DebtFreeDay);
        Assert.Equal(40, session.State.Stats.LowestHealth);
    }
}

public class AchievementTests
{
    private static GameState Finished(Action<GameState>? tweak = null)
    {
        var s = new GameState { Day = 30, IsOver = true, EndReason = EndReason.Finished, CityId = "london" };
        tweak?.Invoke(s);
        return s;
    }

    private static List<string> Earned(GameState s, long score) => ProgressService.Earned(s, score).ToList();

    [Fact]
    public void Every_achievement_id_is_unique_and_reachable()
    {
        Assert.Equal(ProgressService.All.Count, ProgressService.All.Select(a => a.Id).Distinct().Count());
    }

    [Fact]
    public void Basic_run_achievements()
    {
        var earned = Earned(Finished(), 150_000);

        Assert.Contains("first_run", earned);
        Assert.Contains("in_black", earned);
        Assert.Contains("high_roller", earned);
        Assert.Contains("clean", earned);
        Assert.DoesNotContain("millionaire", earned);
        Assert.DoesNotContain("hard_lesson", earned);
    }

    [Fact]
    public void Dying_teaches_a_hard_lesson_not_a_close_call()
    {
        var earned = Earned(Finished(s => { s.EndReason = EndReason.Killed; s.Stats.LowestHealth = 0; s.Difficulty = Difficulty.Hard; }), 5_000);

        Assert.Contains("hard_lesson", earned);
        Assert.DoesNotContain("close_call", earned);
        Assert.DoesNotContain("hard_case", earned);
    }

    [Fact]
    public void Situational_achievements()
    {
        var all = GameCatalog.Cities.Select(c => c.Id).ToList();
        var earned = Earned(Finished(s =>
        {
            s.Stats.CitiesVisited = all;
            s.Stats.Stops = 4;
            s.Stats.Escapes = 3;
            s.Stats.Arrests = 1;
            s.Stats.LowestHealth = 8;
            s.Stats.BestTrade = 60_000;
            s.Stats.DebtFreeDay = 6;
            s.Stats.FixerSuccesses = 1;
            s.Stats.Trades = 50;
            s.Bank = 50_000;
            s.Reputation["eddie"] = 10;
            s.Stashes.Add(new Stash { CityId = "london", Tier = StashTier.Warehouse });
            s.Difficulty = Difficulty.Hard;
            s.ChallengeDate = "2026-10-31";
        }), 2_000_000);

        foreach (var id in new[] { "globetrotter", "escape_artist", "jailbird", "close_call", "big_score", "debt_free", "fixer",
                     "wheeler_dealer", "old_money", "favourite", "warehouse", "hard_case", "daily", "millionaire" })
        {
            Assert.Contains(id, earned);
        }
        Assert.DoesNotContain("clean", earned);
    }

    [Fact]
    public void Retiring_early_is_not_a_clean_record()
    {
        Assert.DoesNotContain("clean", Earned(Finished(s => s.EndReason = EndReason.Retired), 10));
    }
}

public class ProgressServiceTests
{
    [Fact]
    public async Task Records_career_totals_and_unlocks_once()
    {
        var progress = new ProgressService(new InMemoryStore());
        var run = new GameState { Day = 30, EndReason = EndReason.Finished, ChallengeDate = "2026-10-31", Difficulty = Difficulty.Easy };
        run.Stats.Trades = 12;
        run.Stats.TotalProfit = 4_000;
        run.Stats.CitiesVisited = ["london", "dubai"];

        var first = await progress.RecordRunAsync(run, 9_000);
        var second = await progress.RecordRunAsync(run, 3_000);

        Assert.Contains(first, a => a.Id == "first_run");
        Assert.Contains(first, a => a.Id == "daily");
        Assert.Empty(second);
        var career = await progress.GetCareerAsync();
        Assert.Equal(2, career.RunsPlayed);
        Assert.Equal(2, career.Finished);
        Assert.Equal(9_000, career.BestScore);
        Assert.Equal(12_000, career.TotalScore);
        Assert.Equal(24, career.Trades);
        Assert.Equal(60, career.DaysPlayed);
        Assert.Equal(9_000, career.DailyBest["2026-10-31"]);
        Assert.Equal(9_000, career.BestByDifficulty[Difficulty.Easy]);
        Assert.Equal(["london", "dubai"], career.CitiesVisited);
    }

    [Fact]
    public async Task Clear_resets_everything()
    {
        var progress = new ProgressService(new InMemoryStore());
        await progress.RecordRunAsync(new GameState { EndReason = EndReason.Retired }, 1);

        await progress.ClearAsync();

        Assert.Equal(0, (await progress.GetCareerAsync()).RunsPlayed);
        Assert.Empty(await progress.GetUnlockedAsync());
    }
}

public class SessionPolishTests
{
    private readonly InMemoryStore _store = new();
    private readonly RecordingFeedback _feedback = new();

    private GameSession NewSession(EngineFactory? factory = null) =>
        new(factory ?? TestGame.Factory(), _store, new HighScoreService(_store), new ProgressService(_store), _feedback);

    [Fact]
    public async Task Ending_a_run_records_score_details_and_achievements()
    {
        var session = NewSession();
        await session.StartNewAsync(Difficulty.Easy);

        await session.EndRunAsync();

        Assert.Contains(session.NewAchievements, a => a.Id == "first_run");
        var score = Assert.Single(await new HighScoreService(_store).GetTopAsync());
        Assert.Equal(Difficulty.Easy, score.Difficulty);
        Assert.Null(score.ChallengeDate);
        Assert.Equal(SoundCue.Achievement, _feedback.Played[^1]);
    }

    [Fact]
    public async Task Sounds_match_what_happened()
    {
        var session = NewSession();
        await session.StartNewAsync();
        session.State!.Market["cigs"] = 10;

        await session.BuyAsync("cigs", 5);
        await session.BuyAsync("cigs", 100_000);
        await session.SellAsync("cigs", 5);
        session.State.PendingEncounter = new Encounter { Kind = EncounterKind.Police, BribeCost = 300 };
        await session.ResolveEncounterAsync(EncounterChoice.Surrender);

        Assert.Equal([SoundCue.Buy, SoundCue.Error, SoundCue.Sell, SoundCue.Arrest], _feedback.Played);
    }

    [Fact]
    public async Task Second_finished_run_does_not_repeat_achievements()
    {
        var session = NewSession();
        await session.StartNewAsync();
        await session.EndRunAsync();
        await session.StartNewAsync();

        await session.EndRunAsync();

        Assert.DoesNotContain(session.NewAchievements, a => a.Id == "first_run");
        Assert.Equal(SoundCue.Finish, _feedback.Played[^1]);
    }
}
