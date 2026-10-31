using BlackMarketEmpire.Tests.Fakes;

namespace BlackMarketEmpire.Tests.Services;

public class EncounterServiceTests
{
    private static GameState Loaded(int units = 20, long avgCost = 100, long cash = 1_000) => new()
    {
        Day = 5,
        CityId = "dubai",
        Cash = cash,
        Debt = 5_000,
        Capacity = 100,
        Health = 100,
        Inventory = { ["cigs"] = units },
        AverageCost = { ["cigs"] = avgCost },
    };

    [Fact]
    public void Never_stopped_with_an_empty_suitcase()
    {
        var state = Loaded(units: 0);
        state.Inventory.Clear();

        Assert.Equal(0, EncounterService.StopChance(state, GameRules.Default, TravelMethod.Flight, "dubai"));
        Assert.False(new EncounterService(new ScriptedRandom(0)).MaybeStopOnArrival(state, GameRules.Default, TravelMethod.Flight));
    }

    [Fact]
    public void Stop_chance_grows_with_cargo_and_is_capped()
    {
        var rules = GameRules.Default;
        var light = EncounterService.StopChance(Loaded(units: 5, avgCost: 20), rules, TravelMethod.Train, "dubai");
        var heavy = EncounterService.StopChance(Loaded(units: 100, avgCost: 2_000), rules, TravelMethod.Train, "dubai");

        Assert.True(heavy > light);
        Assert.Equal(rules.MaxStopChance, heavy);
    }

    [Theory]
    [InlineData(TravelMethod.Train, EncounterKind.Police)]
    [InlineData(TravelMethod.Car, EncounterKind.Police)]
    [InlineData(TravelMethod.Flight, EncounterKind.Customs)]
    [InlineData(TravelMethod.Boat, EncounterKind.Customs)]
    public void Flights_and_boats_meet_customs(TravelMethod method, EncounterKind expected)
    {
        var state = Loaded();

        Assert.True(new EncounterService(new ScriptedRandom(0)).MaybeStopOnArrival(state, GameRules.Default, method));
        Assert.Equal(expected, state.PendingEncounter!.Kind);
        Assert.Equal(1, state.Stats.Stops);
    }

    [Fact]
    public void Bribe_is_at_least_the_minimum_and_rounded_to_50()
    {
        var state = Loaded(units: 1, avgCost: 10, cash: 100);
        new EncounterService(new ScriptedRandom(0)).MaybeStopOnArrival(state, GameRules.Default, TravelMethod.Train);

        Assert.Equal(GameRules.Default.MinBribe, state.PendingEncounter!.BribeCost);
        Assert.Equal(0, state.PendingEncounter.BribeCost % 50);
    }

    [Fact]
    public void Small_deals_are_never_stings()
    {
        var state = Loaded();

        Assert.False(new EncounterService(new ScriptedRandom(0)).MaybeSting(state, GameRules.Default, 9_999));
        Assert.True(new EncounterService(new ScriptedRandom(0)).MaybeSting(state, GameRules.Default, 10_000));
        Assert.Equal(EncounterKind.Undercover, state.PendingEncounter!.Kind);
    }
}

public class GameEngineEncounterTests
{
    /// <summary>Engine whose encounter rolls come from <paramref name="encounterRolls"/>.</summary>
    private static GameEngine Engine(params double[] encounterRolls)
    {
        var random = new SystemRandomSource(new Random(1));
        return new GameEngine(new MarketService(random), new EventService(random),
            new EncounterService(new ScriptedRandom(encounterRolls)), new ShippingService(random), new FinanceService(random), TestGame.AllAvailable);
    }

    private static GameState Stopped(GameEngine engine, EncounterKind kind = EncounterKind.Police, int units = 20)
    {
        var state = engine.NewGame();
        state.Inventory["cigs"] = units;
        state.AverageCost["cigs"] = 50;
        state.PendingEncounter = new Encounter { Kind = kind, BribeCost = 500, Intro = "Stop!" };
        return state;
    }

    [Fact]
    public void Other_actions_are_blocked_during_an_encounter()
    {
        var engine = Engine();
        var state = Stopped(engine);

        Assert.False(engine.Wait(state).Success);
        Assert.False(engine.Travel(state, "amsterdam").Success);
        Assert.False(engine.Sell(state, "cigs", 1).Success);
        Assert.False(engine.EndRun(state).Success);
    }

    [Fact]
    public void Successful_run_keeps_everything()
    {
        var engine = Engine(0.0); // run succeeds
        var state = Stopped(engine);

        var result = engine.ResolveEncounter(state, EncounterChoice.Run);

        Assert.True(result.Success);
        Assert.Null(state.PendingEncounter);
        Assert.Equal(20, state.Inventory["cigs"]);
        Assert.Equal(1, state.Stats.Escapes);
    }

    [Fact]
    public void Failed_run_hurts_and_the_encounter_continues()
    {
        var engine = Engine(0.99, 0.0); // run fails, minimum damage (10)
        var state = Stopped(engine);

        engine.ResolveEncounter(state, EncounterChoice.Run);

        Assert.NotNull(state.PendingEncounter);
        Assert.Equal(90, state.Health);
        Assert.Equal(1, state.PendingEncounter!.Round);
        Assert.NotNull(state.PendingEncounter.LastOutcome);
    }

    [Fact]
    public void Accepted_bribe_costs_cash_only()
    {
        var engine = Engine(0.0);
        var state = Stopped(engine);

        engine.ResolveEncounter(state, EncounterChoice.Bribe);

        Assert.Null(state.PendingEncounter);
        Assert.Equal(2_000, state.Cash);
        Assert.Equal(500, state.Stats.BribesPaid);
        Assert.Equal(20, state.Inventory["cigs"]);
    }

    [Fact]
    public void Bribe_is_refused_if_you_cannot_afford_it()
    {
        var engine = Engine();
        var state = Stopped(engine);
        state.Cash = 100;

        Assert.False(engine.ResolveEncounter(state, EncounterChoice.Bribe).Success);
        Assert.NotNull(state.PendingEncounter);
    }

    [Fact]
    public void Surrender_seizes_goods_fines_and_jails()
    {
        var engine = Engine(0.0); // jail roll → minimum (1 day for police)
        var state = Stopped(engine);

        engine.ResolveEncounter(state, EncounterChoice.Surrender);

        Assert.Null(state.PendingEncounter);
        Assert.Empty(state.Inventory);
        Assert.Equal(2_000, state.Cash);          // 20% of 2,500 fined
        Assert.Equal(2, state.Day);               // 1 day in jail
        Assert.Equal(1, state.Stats.Arrests);
        Assert.Equal(20, state.Stats.GoodsSeized);
        Assert.Equal(1_000, state.Stats.GoodsSeizedValue);
        Assert.False(state.IsOver);
    }

    [Fact]
    public void Refused_bribe_means_arrest_without_paying_it()
    {
        var engine = Engine(0.99, 0.0);
        var state = Stopped(engine);

        engine.ResolveEncounter(state, EncounterChoice.Bribe);

        Assert.Equal(0, state.Stats.BribesPaid);
        Assert.Equal(1, state.Stats.Arrests);
        Assert.Empty(state.Inventory);
    }

    [Fact]
    public void Lost_fight_hurts_then_arrests()
    {
        var engine = Engine(0.99, 0.0, 0.0); // lose, min damage 30, min jail
        var state = Stopped(engine);

        engine.ResolveEncounter(state, EncounterChoice.Fight);

        Assert.Equal(70 + engine.Rules.HealthPerDay, state.Health); // healed a little in jail
        Assert.Equal(1, state.Stats.Arrests);
    }

    [Fact]
    public void Dying_ends_the_run_and_loses_your_cash()
    {
        var engine = Engine(0.99, 0.99); // run fails, max damage
        var state = Stopped(engine);
        state.Health = 5;

        var result = engine.ResolveEncounter(state, EncounterChoice.Run);

        Assert.True(state.IsOver);
        Assert.Equal(EndReason.Killed, state.EndReason);
        Assert.Equal(0, state.Health);
        Assert.Null(state.PendingEncounter);
        Assert.Equal(-state.Debt, engine.Score(state));
        Assert.Contains("didn't survive", result.Message);
    }

    [Fact]
    public void Jail_never_runs_past_the_last_day()
    {
        var engine = Engine(0.99, 0.99); // refused bribe → arrest, max jail
        var state = Stopped(engine, EncounterKind.Customs);
        state.Day = 29;

        engine.ResolveEncounter(state, EncounterChoice.Bribe);

        Assert.Equal(30, state.Day);
        Assert.Equal(1, state.Stats.DaysInJail);
    }

    [Fact]
    public void Health_recovers_each_day_up_to_the_max()
    {
        var engine = Engine();
        var state = engine.NewGame();
        state.Health = 50;

        engine.Wait(state);
        Assert.Equal(53, state.Health);

        state.Health = 99;
        engine.Wait(state);
        Assert.Equal(100, state.Health);
    }

    [Fact]
    public void Travel_with_cargo_can_trigger_a_stop()
    {
        var random = new SystemRandomSource(new Random(1));
        var rules = TestGame.AllAvailable with { MaxStopChance = 1, BaseStopChance = 1 };
        var engine = new GameEngine(new MarketService(random), new EventService(random), new EncounterService(random), new ShippingService(random), new FinanceService(random), rules);
        var state = engine.NewGame();
        state.Inventory["cigs"] = 1;
        state.AverageCost["cigs"] = 10;

        var result = engine.Travel(state, "istanbul", TravelMethod.Flight);

        Assert.Equal(EncounterKind.Customs, state.PendingEncounter?.Kind);
        Assert.Contains("Customs", result.Message);
    }

    [Fact]
    public void End_reason_is_recorded()
    {
        var engine = Engine();
        var retired = engine.NewGame();
        engine.EndRun(retired);
        var finished = engine.NewGame();
        finished.Day = 30;
        engine.Wait(finished);

        Assert.Equal(EndReason.Retired, retired.EndReason);
        Assert.Equal(EndReason.Finished, finished.EndReason);
    }
}
