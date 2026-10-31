using BlackMarketEmpire.Models;

namespace BlackMarketEmpire.Services;

/// <summary>
/// All the game rules. Stateless: every method acts on the <see cref="GameState"/> it's given,
/// which keeps it easy to test and to save/resume.
/// </summary>
public sealed class GameEngine(MarketService market, GameRules rules)
{
    public const string StartCityId = "london";

    public GameRules Rules => rules;

    public GameState NewGame()
    {
        var state = new GameState
        {
            CityId = StartCityId,
            Cash = rules.StartingCash,
            Debt = rules.StartingDebt,
            Capacity = rules.Capacity,
        };
        state.Market = market.GenerateMarket(state.CityId, rules);
        AddLog(state, $"Day 1. You land in {GameCatalog.City(state.CityId).Name} with £{state.Cash:N0} and a £{state.Debt:N0} debt to a loan shark. You have {rules.TotalDays} days.");
        return state;
    }

    public int DaysLeft(GameState state) => rules.TotalDays - state.Day;

    public int MaxBuyable(GameState state, string goodId)
    {
        if (!state.Market.TryGetValue(goodId, out var price) || price <= 0)
        {
            return 0;
        }
        var affordable = (int)Math.Min(int.MaxValue, state.Cash / price);
        return Math.Max(0, Math.Min(affordable, state.FreeSpace));
    }

    public int Held(GameState state, string goodId) =>
        state.Inventory.TryGetValue(goodId, out var qty) ? qty : 0;

    public ActionResult Buy(GameState state, string goodId, int quantity)
    {
        if (Guard(state) is { } over) return over;
        if (quantity <= 0) return ActionResult.Fail("Choose how many to buy.");
        if (!state.Market.TryGetValue(goodId, out var price))
            return ActionResult.Fail("Nobody is selling that here today.");

        var good = GameCatalog.Good(goodId);
        var cost = (long)price * quantity;
        if (cost > state.Cash) return ActionResult.Fail($"You can't afford {quantity} × {good.Name}.");
        if (quantity > state.FreeSpace) return ActionResult.Fail($"You only have room for {state.FreeSpace} more.");

        var held = Held(state, goodId);
        var oldAvg = state.AverageCost.GetValueOrDefault(goodId);
        state.AverageCost[goodId] = (oldAvg * held + cost) / (held + quantity);
        state.Inventory[goodId] = held + quantity;
        state.Cash -= cost;
        Touch(state);
        return Log(state, ActionResult.Ok($"Bought {quantity} × {good.Name} for £{cost:N0}."));
    }

    public ActionResult Sell(GameState state, string goodId, int quantity)
    {
        if (Guard(state) is { } over) return over;
        if (quantity <= 0) return ActionResult.Fail("Choose how many to sell.");
        if (!state.Market.TryGetValue(goodId, out var price))
            return ActionResult.Fail("Nobody is buying that here today.");

        var held = Held(state, goodId);
        var good = GameCatalog.Good(goodId);
        if (quantity > held) return ActionResult.Fail($"You only have {held} × {good.Name}.");

        var revenue = (long)price * quantity;
        var profit = revenue - state.AverageCost.GetValueOrDefault(goodId) * quantity;
        state.Cash += revenue;
        if (held == quantity)
        {
            state.Inventory.Remove(goodId);
            state.AverageCost.Remove(goodId);
        }
        else
        {
            state.Inventory[goodId] = held - quantity;
        }
        Touch(state);
        var outcome = profit >= 0 ? $"profit £{profit:N0}" : $"loss £{-profit:N0}";
        return Log(state, ActionResult.Ok($"Sold {quantity} × {good.Name} for £{revenue:N0} ({outcome})."));
    }

    public ActionResult Travel(GameState state, string cityId)
    {
        if (Guard(state) is { } over) return over;
        if (cityId == state.CityId) return ActionResult.Fail("You're already here.");

        var days = GameCatalog.DaysBetween(state.CityId, cityId);
        if (state.Day + days > rules.TotalDays)
            return ActionResult.Fail($"That trip takes {days} days — you only have {DaysLeft(state)} left.");

        AdvanceDays(state, days);
        state.CityId = cityId;
        state.Market = market.GenerateMarket(cityId, rules);
        Touch(state);
        var city = GameCatalog.City(cityId);
        return Log(state, ActionResult.Ok($"Day {state.Day}. Arrived in {city.Name} after {days} day{(days == 1 ? "" : "s")} on the road."));
    }

    /// <summary>Lie low for a day. On the final day this ends the run.</summary>
    public ActionResult Wait(GameState state)
    {
        if (Guard(state) is { } over) return over;
        if (state.Day >= rules.TotalDays) return EndRun(state);

        AdvanceDays(state, 1);
        state.Market = market.GenerateMarket(state.CityId, rules);
        Touch(state);
        return Log(state, ActionResult.Ok($"Day {state.Day}. You lie low for a day. New prices in {GameCatalog.City(state.CityId).Name}."));
    }

    public ActionResult RepayDebt(GameState state, long amount)
    {
        if (Guard(state) is { } over) return over;
        if (state.Debt <= 0) return ActionResult.Fail("You don't owe anything.");
        if (amount <= 0) return ActionResult.Fail("Choose an amount to repay.");

        var paid = Math.Min(amount, Math.Min(state.Cash, state.Debt));
        if (paid <= 0) return ActionResult.Fail("You've no cash to repay with.");

        state.Cash -= paid;
        state.Debt -= paid;
        Touch(state);
        var message = state.Debt == 0
            ? $"Paid £{paid:N0}. The loan shark is off your back — debt cleared!"
            : $"Paid £{paid:N0} off your debt. £{state.Debt:N0} still owed.";
        return Log(state, ActionResult.Ok(message));
    }

    /// <summary>Ends the run. Unsold goods are worth nothing; score is cash minus debt.</summary>
    public ActionResult EndRun(GameState state)
    {
        if (state.IsOver) return ActionResult.Fail("This run is already over.");
        state.IsOver = true;
        Touch(state);
        return Log(state, ActionResult.Ok($"Run over. Final net worth: {Money.Format(Score(state))}."));
    }

    public long Score(GameState state) => state.Cash - state.Debt;

    private void AdvanceDays(GameState state, int days)
    {
        for (var i = 0; i < days; i++)
        {
            state.Day++;
            if (state.Debt > 0)
            {
                state.Debt = (long)Math.Ceiling(state.Debt * (1 + rules.DailyInterest));
            }
        }
    }

    private static ActionResult? Guard(GameState state) =>
        state.IsOver ? ActionResult.Fail("This run is over. Start a new one.") : null;

    private static void Touch(GameState state) => state.LastModified = DateTime.UtcNow;

    private ActionResult Log(GameState state, ActionResult result)
    {
        AddLog(state, result.Message);
        return result;
    }

    private void AddLog(GameState state, string message)
    {
        state.Log.Insert(0, message);
        if (state.Log.Count > rules.MaxLogEntries)
        {
            state.Log.RemoveRange(rules.MaxLogEntries, state.Log.Count - rules.MaxLogEntries);
        }
    }
}
