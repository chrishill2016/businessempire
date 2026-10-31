using BlackMarketEmpire.Tests.Fakes;

namespace BlackMarketEmpire.Tests.Services;

public class GameEngineTests
{
    private readonly GameEngine _engine = TestGame.Engine();

    [Fact]
    public void NewGame_starts_in_London_on_day_one_with_cash_and_debt()
    {
        var state = _engine.NewGame();

        Assert.Equal(1, state.Day);
        Assert.Equal(GameEngine.StartCityId, state.CityId);
        Assert.Equal(2_500, state.Cash);
        Assert.Equal(5_000, state.Debt);
        Assert.Equal(100, state.Capacity);
        Assert.NotEmpty(state.Market);
        Assert.False(state.IsOver);
    }

    [Fact]
    public void Buy_deducts_cash_and_adds_inventory()
    {
        var state = TestGame.StateWithMarket(_engine, new() { ["cigs"] = 20 });

        var result = _engine.Buy(state, "cigs", 10);

        Assert.True(result.Success);
        Assert.Equal(2_300, state.Cash);
        Assert.Equal(10, state.Inventory["cigs"]);
        Assert.Equal(20, state.AverageCost["cigs"]);
    }

    [Fact]
    public void Buy_tracks_average_cost_across_purchases()
    {
        var state = TestGame.StateWithMarket(_engine, new() { ["cigs"] = 20 });
        _engine.Buy(state, "cigs", 10);
        state.Market["cigs"] = 40;

        _engine.Buy(state, "cigs", 10);

        Assert.Equal(30, state.AverageCost["cigs"]);
    }

    [Fact]
    public void Buy_fails_when_unaffordable()
    {
        var state = TestGame.StateWithMarket(_engine, new() { ["diamonds"] = 10_000 });

        var result = _engine.Buy(state, "diamonds", 1);

        Assert.False(result.Success);
        Assert.Equal(2_500, state.Cash);
        Assert.Empty(state.Inventory);
    }

    [Fact]
    public void Buy_fails_when_suitcase_is_full()
    {
        var state = TestGame.StateWithMarket(_engine, new() { ["cigs"] = 1 });

        var result = _engine.Buy(state, "cigs", 101);

        Assert.False(result.Success);
        Assert.Empty(state.Inventory);
    }

    [Fact]
    public void Buy_fails_when_good_not_on_sale()
    {
        var state = TestGame.StateWithMarket(_engine, new() { ["cigs"] = 20 });

        Assert.False(_engine.Buy(state, "art", 1).Success);
    }

    [Fact]
    public void MaxBuyable_is_limited_by_cash_and_space()
    {
        var state = TestGame.StateWithMarket(_engine, new() { ["cigs"] = 10, ["phones"] = 1_000 });

        Assert.Equal(100, _engine.MaxBuyable(state, "cigs"));   // space-limited (cash allows 250)
        Assert.Equal(2, _engine.MaxBuyable(state, "phones"));   // cash-limited
        Assert.Equal(0, _engine.MaxBuyable(state, "art"));      // not on sale
    }

    [Fact]
    public void Sell_adds_cash_and_removes_inventory()
    {
        var state = TestGame.StateWithMarket(_engine, new() { ["cigs"] = 20 });
        _engine.Buy(state, "cigs", 10);
        state.Market["cigs"] = 50;

        var result = _engine.Sell(state, "cigs", 10);

        Assert.True(result.Success);
        Assert.Equal(2_800, state.Cash);
        Assert.False(state.Inventory.ContainsKey("cigs"));
        Assert.False(state.AverageCost.ContainsKey("cigs"));
        Assert.Contains("profit £300", result.Message);
    }

    [Fact]
    public void Sell_part_keeps_remaining_units()
    {
        var state = TestGame.StateWithMarket(_engine, new() { ["cigs"] = 20 });
        _engine.Buy(state, "cigs", 10);

        _engine.Sell(state, "cigs", 4);

        Assert.Equal(6, state.Inventory["cigs"]);
        Assert.Equal(20, state.AverageCost["cigs"]);
    }

    [Fact]
    public void Sell_fails_when_selling_more_than_held()
    {
        var state = TestGame.StateWithMarket(_engine, new() { ["cigs"] = 20 });
        _engine.Buy(state, "cigs", 2);

        Assert.False(_engine.Sell(state, "cigs", 3).Success);
        Assert.Equal(2, state.Inventory["cigs"]);
    }

    [Fact]
    public void Travel_moves_city_uses_days_and_charges_interest()
    {
        var state = _engine.NewGame();

        var result = _engine.Travel(state, "istanbul"); // 2 days from London

        Assert.True(result.Success);
        Assert.Equal("istanbul", state.CityId);
        Assert.Equal(3, state.Day);
        Assert.Equal(5_513, state.Debt); // 5000 → 5250 → 5512.5 rounded up
    }

    [Fact]
    public void Travel_to_current_city_fails()
    {
        var state = _engine.NewGame();

        Assert.False(_engine.Travel(state, "london").Success);
        Assert.Equal(1, state.Day);
    }

    [Fact]
    public void Travel_that_would_overrun_the_last_day_is_refused()
    {
        var state = _engine.NewGame();
        state.Day = 28;

        var result = _engine.Travel(state, "hongkong"); // 3 days

        Assert.False(result.Success);
        Assert.Equal(28, state.Day);
        Assert.Equal("london", state.CityId);
    }

    [Fact]
    public void Travel_keeps_inventory()
    {
        var state = TestGame.StateWithMarket(_engine, new() { ["cigs"] = 20 });
        _engine.Buy(state, "cigs", 5);

        _engine.Travel(state, "amsterdam");

        Assert.Equal(5, state.Inventory["cigs"]);
    }

    [Fact]
    public void Wait_advances_one_day()
    {
        var state = _engine.NewGame();

        _engine.Wait(state);

        Assert.Equal(2, state.Day);
        Assert.Equal(5_250, state.Debt);
        Assert.False(state.IsOver);
    }

    [Fact]
    public void Wait_on_last_day_ends_the_run()
    {
        var state = _engine.NewGame();
        state.Day = 30;

        _engine.Wait(state);

        Assert.True(state.IsOver);
        Assert.Equal(30, state.Day);
    }

    [Fact]
    public void No_interest_charged_once_debt_is_cleared()
    {
        var state = _engine.NewGame();
        state.Cash = 10_000;
        _engine.RepayDebt(state, 5_000);

        _engine.Wait(state);

        Assert.Equal(0, state.Debt);
    }

    [Fact]
    public void RepayDebt_is_capped_by_cash_and_debt()
    {
        var state = _engine.NewGame();

        _engine.RepayDebt(state, 1_000_000);

        Assert.Equal(0, state.Cash);
        Assert.Equal(2_500, state.Debt);
    }

    [Fact]
    public void Actions_are_refused_after_the_run_ends()
    {
        var state = TestGame.StateWithMarket(_engine, new() { ["cigs"] = 20 });
        _engine.EndRun(state);

        Assert.False(_engine.Buy(state, "cigs", 1).Success);
        Assert.False(_engine.Travel(state, "amsterdam").Success);
        Assert.False(_engine.Wait(state).Success);
        Assert.False(_engine.EndRun(state).Success);
    }

    [Fact]
    public void Score_is_cash_minus_debt_and_ignores_unsold_goods()
    {
        var state = TestGame.StateWithMarket(_engine, new() { ["cigs"] = 20 });
        _engine.Buy(state, "cigs", 10);

        _engine.EndRun(state);

        Assert.Equal(2_300 - 5_000, _engine.Score(state));
    }

    [Fact]
    public void Log_is_capped_and_newest_first()
    {
        var state = _engine.NewGame();
        for (var i = 0; i < 29; i++) _engine.Wait(state);

        Assert.True(state.Log.Count <= _engine.Rules.MaxLogEntries);
        Assert.StartsWith("Day 30", state.Log[0]);
    }

    [Fact]
    public void Same_seed_gives_same_prices()
    {
        var a = TestGame.Engine(seed: 7, GameRules.Default).NewGame();
        var b = TestGame.Engine(seed: 7, GameRules.Default).NewGame();

        Assert.Equal(a.Market, b.Market);
    }
}
