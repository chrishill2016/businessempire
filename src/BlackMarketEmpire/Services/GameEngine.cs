using BlackMarketEmpire.Models;

namespace BlackMarketEmpire.Services;

/// <summary>
/// All the game rules. Stateless: every method acts on the <see cref="GameState"/> it's given,
/// which keeps it easy to test and to save/resume.
/// </summary>
public sealed class GameEngine(MarketService market, EventService events, EncounterService encounters, ShippingService shipping, GameRules rules)
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
            Health = rules.MaxHealth,
        };
        events.SeedNewRun(state, rules);
        RefreshMarket(state);
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

        AddToInventory(state, goodId, quantity, price);
        state.Cash -= cost;
        if (cost >= rules.BigDealValue) EncounterService.AddHeat(state, state.CityId, rules.HeatOnBigDeal);
        Touch(state);
        var bought = $"Bought {quantity} × {good.Name} for £{cost:N0}.";
        return Log(state, ActionResult.Ok(encounters.MaybeSting(state, rules, cost) ? $"{bought} {state.PendingEncounter!.Intro}" : bought));
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
        RemoveFromInventory(state, goodId, quantity);
        if (revenue >= rules.BigDealValue) EncounterService.AddHeat(state, state.CityId, rules.HeatOnBigDeal);
        Touch(state);
        var outcome = profit >= 0 ? $"profit £{profit:N0}" : $"loss £{-profit:N0}";
        var sold = $"Sold {quantity} × {good.Name} for £{revenue:N0} ({outcome}).";
        return Log(state, ActionResult.Ok(encounters.MaybeSting(state, rules, revenue) ? $"{sold} {state.PendingEncounter!.Intro}" : sold));
    }

    public IReadOnlyList<TravelOption> TravelOptions(GameState state, string cityId) =>
        TravelService.Options(state, rules, cityId);

    /// <summary>Travel to a city. With no method given, takes the cheapest one available.</summary>
    public ActionResult Travel(GameState state, string cityId, TravelMethod? method = null)
    {
        if (Guard(state) is { } over) return over;
        if (cityId == state.CityId) return ActionResult.Fail("You're already here.");

        var options = TravelOptions(state, cityId);
        var option = method is { } m
            ? options.First(o => o.Method == m)
            : options.Where(o => o.Available).OrderBy(o => o.Fare).FirstOrDefault()
              ?? options.OrderBy(o => o.Days).First();
        if (!option.Available)
        {
            var city = GameCatalog.City(cityId).Name;
            return ActionResult.Fail(option.Reason switch
            {
                "Not enough days left" => $"That trip takes {option.Days} days — you only have {DaysLeft(state)} left.",
                "Can't afford the fare" => $"The {TravelService.Label(option.Method).ToLowerInvariant()} fare to {city} is {Money.Format(option.Fare)}. You can't afford it.",
                _ => $"You can't get to {city} by {TravelService.Label(option.Method).ToLowerInvariant()}: {option.Reason?.ToLowerInvariant()}.",
            });
        }

        state.Cash -= option.Fare;
        state.Stats.FaresPaid += option.Fare;
        state.LastTravelMethod = option.Method;
        var days = option.Days;
        AdvanceDays(state, days, inTransit: true);
        state.CityId = cityId;
        RefreshMarket(state);
        Touch(state);
        var dest = GameCatalog.City(cityId);
        var message = $"Day {state.Day}. Arrived in {dest.Name} by {TravelService.Label(option.Method).ToLowerInvariant()} after {days} day{(days == 1 ? "" : "s")} ({Money.Format(option.Fare)}).";
        var intel = events.RevealCity(state, cityId).Select(DescribeReveal).ToList();
        if (intel.Count > 0) message = $"{message} {string.Join(" ", intel)}";
        var waiting = DeliveriesHere(state).Sum(s => s.Quantity);
        if (waiting > 0) message = $"{message} {waiting} items are waiting for you to collect.";
        if (encounters.MaybeStopOnArrival(state, rules, option.Method)) message = $"{message} {state.PendingEncounter!.Intro}";
        return Log(state, ActionResult.Ok(message));
    }

    // ----- Gear -----

    public ActionResult BuyGear(GameState state, string gearId)
    {
        if (Guard(state) is { } over) return over;
        var item = GameCatalog.GearItem(gearId);
        if (!item.Consumable && state.Has(gearId)) return ActionResult.Fail($"You already have the {item.Name.ToLowerInvariant()}.");
        if (item.Price > state.Cash) return ActionResult.Fail($"The {item.Name.ToLowerInvariant()} costs {Money.Format(item.Price)}. You can't afford it.");

        state.Cash -= item.Price;
        state.Gear[gearId] = state.Gear.GetValueOrDefault(gearId) + 1;
        state.Capacity += gearId switch { "holdall" => 50, "trolley" => 100, _ => 0 };
        Touch(state);
        return Log(state, ActionResult.Ok($"Bought {item.Icon} {item.Name} for {Money.Format(item.Price)}."));
    }

    public ActionResult UseFirstAid(GameState state)
    {
        if (Guard(state) is { } over) return over;
        if (!state.Has("firstaid")) return ActionResult.Fail("You don't have a first-aid kit.");
        if (state.Health >= rules.MaxHealth) return ActionResult.Fail("You're not hurt.");

        state.Gear["firstaid"]--;
        if (state.Gear["firstaid"] == 0) state.Gear.Remove("firstaid");
        var before = state.Health;
        state.Health = Math.Min(rules.MaxHealth, state.Health + 40);
        Touch(state);
        return Log(state, ActionResult.Ok($"Patched yourself up: +{state.Health - before} health."));
    }

    // ----- Shipping -----

    public ShippingQuote QuoteShipping(GameState state, string goodId, int quantity, string toCityId, string courierId) =>
        ShippingService.Quote(state, goodId, quantity, toCityId, GameCatalog.Courier(courierId));

    public ActionResult Ship(GameState state, string goodId, int quantity, string toCityId, string courierId)
    {
        if (Guard(state) is { } over) return over;
        if (toCityId == state.CityId) return ActionResult.Fail("Choose a different city to ship to.");
        var held = Held(state, goodId);
        var good = GameCatalog.Good(goodId);
        if (quantity <= 0 || quantity > held) return ActionResult.Fail($"You have {held} × {good.Name}.");

        var courier = GameCatalog.Courier(courierId);
        var quote = QuoteShipping(state, goodId, quantity, toCityId, courierId);
        if (quote.ArrivalDay > rules.TotalDays) return ActionResult.Fail("It wouldn't arrive before your 30 days are up.");
        if (quote.Fee > state.Cash) return ActionResult.Fail($"The courier wants {Money.Format(quote.Fee)}. You can't afford it.");

        state.Cash -= quote.Fee;
        state.Stats.CourierFeesPaid += quote.Fee;
        state.Shipments.Insert(0, new Shipment
        {
            GoodId = goodId,
            Quantity = quantity,
            AverageCost = state.AverageCost.GetValueOrDefault(goodId),
            FromCityId = state.CityId,
            ToCityId = toCityId,
            CourierId = courierId,
            SentDay = state.Day,
            ArrivalDay = quote.ArrivalDay,
        });
        RemoveFromInventory(state, goodId, quantity);
        Touch(state);
        return Log(state, ActionResult.Ok(
            $"{courier.Name} is taking {quantity} × {good.Name} to {GameCatalog.City(toCityId).Name} for {Money.Format(quote.Fee)}. Due day {quote.ArrivalDay}."));
    }

    public IEnumerable<Shipment> DeliveriesHere(GameState state) =>
        state.Shipments.Where(s => s.Status == ShipmentStatus.Delivered && s.ToCityId == state.CityId);

    /// <summary>Picks up delivered goods in the current city, as far as your suitcase has room.</summary>
    public ActionResult Collect(GameState state)
    {
        if (Guard(state) is { } over) return over;
        var waiting = DeliveriesHere(state).ToList();
        if (waiting.Count == 0) return ActionResult.Fail("Nothing waiting for you here.");
        if (state.FreeSpace == 0) return ActionResult.Fail("Your suitcase is full. Sell something first.");

        var collected = 0;
        foreach (var s in waiting)
        {
            var take = Math.Min(s.Quantity, state.FreeSpace);
            if (take == 0) break;
            AddToInventory(state, s.GoodId, take, s.AverageCost);
            s.Quantity -= take;
            collected += take;
            if (s.Quantity == 0) state.Shipments.Remove(s);
        }
        Touch(state);
        var left = DeliveriesHere(state).Sum(s => s.Quantity);
        return Log(state, ActionResult.Ok(left == 0
            ? $"Collected {collected} items from the drop."
            : $"Collected {collected} items. {left} more are waiting — make some room."));
    }

    private static void AddToInventory(GameState state, string goodId, int quantity, long unitCost)
    {
        var held = state.Inventory.GetValueOrDefault(goodId);
        var oldAvg = state.AverageCost.GetValueOrDefault(goodId);
        state.AverageCost[goodId] = (oldAvg * held + unitCost * quantity) / (held + quantity);
        state.Inventory[goodId] = held + quantity;
    }

    private static void RemoveFromInventory(GameState state, string goodId, int quantity)
    {
        var held = state.Inventory.GetValueOrDefault(goodId);
        if (held <= quantity)
        {
            state.Inventory.Remove(goodId);
            state.AverageCost.Remove(goodId);
        }
        else
        {
            state.Inventory[goodId] = held - quantity;
        }
    }

    /// <summary>Lie low for a day. On the final day this ends the run.</summary>
    public ActionResult Wait(GameState state)
    {
        if (Guard(state) is { } over) return over;
        if (state.Day >= rules.TotalDays) return EndRun(state);

        AdvanceDays(state, 1);
        RefreshMarket(state);
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
        if (state.PendingEncounter is not null) return ActionResult.Fail("Deal with the police first.");
        Finish(state, state.Day >= rules.TotalDays ? EndReason.Finished : EndReason.Retired);
        return Log(state, ActionResult.Ok($"Run over. Final net worth: {Money.Format(Score(state))}."));
    }

    /// <summary>Cash minus debt. If you're killed your cash is lost, but the loan shark still counts the debt.</summary>
    public long Score(GameState state) => state.EndReason == EndReason.Killed ? -state.Debt : state.Cash - state.Debt;

    /// <summary>Make a choice in the current police encounter.</summary>
    public ActionResult ResolveEncounter(GameState state, EncounterChoice choice)
    {
        if (state.IsOver) return ActionResult.Fail("This run is over. Start a new one.");
        var enc = state.PendingEncounter;
        if (enc is null) return ActionResult.Fail("Nobody's stopped you.");
        if (choice == EncounterChoice.Bribe && state.Cash < enc.BribeCost)
            return ActionResult.Fail($"You don't have {Money.Format(enc.BribeCost)} for a bribe.");

        var outcome = encounters.Resolve(state, rules, choice);
        var message = outcome.Message;

        if (outcome.Damage > 0)
        {
            state.Health = Math.Max(0, state.Health - outcome.Damage);
            state.Stats.DamageTaken += outcome.Damage;
        }

        if (state.Health <= 0)
        {
            state.PendingEncounter = null;
            Finish(state, EndReason.Killed);
            Touch(state);
            return Log(state, ActionResult.Ok($"{message} You didn't survive. Your run is over."));
        }

        if (!outcome.Ended)
        {
            enc.LastOutcome = message;
            Touch(state);
            return Log(state, ActionResult.Ok(message));
        }

        state.PendingEncounter = null;
        if (outcome.JailDays > 0)
        {
            var days = Math.Min(outcome.JailDays, rules.TotalDays - state.Day);
            AdvanceDays(state, days, inTransit: true);
            state.Stats.DaysInJail += days;
            RefreshMarket(state);
            message = $"{message} Released on day {state.Day}.";
        }
        Touch(state);
        return Log(state, ActionResult.Ok(message));
    }

    private void Finish(GameState state, EndReason reason)
    {
        state.IsOver = true;
        state.EndReason = reason;
        Touch(state);
    }

    /// <summary>The active price shock for a good in the current city, if any.</summary>
    public MarketEvent? EventHere(GameState state, string goodId) => EventService.ActiveEvent(state, state.CityId, goodId);

    private void RefreshMarket(GameState state) =>
        state.Market = market.GenerateMarket(state.CityId, rules, state.Day, state.Events);

    private void AdvanceDays(GameState state, int days, bool inTransit = false)
    {
        for (var i = 0; i < days; i++)
        {
            state.Day++;
            if (state.Debt > 0)
            {
                state.Debt = (long)Math.Ceiling(state.Debt * (1 + rules.DailyInterest));
            }
            state.Health = Math.Min(rules.MaxHealth, state.Health + rules.HealthPerDay);
            foreach (var city in state.Heat.Keys.ToList())
            {
                EncounterService.AddHeat(state, city, -rules.HeatCoolPerDay);
                if (state.Heat[city] == 0) state.Heat.Remove(city);
            }
            events.OnNewDay(state, rules, inTransit);
            foreach (var message in shipping.OnNewDay(state)) AddLog(state, message);
        }
    }

    private static string DescribeReveal(NewsItem item)
    {
        var good = GameCatalog.Good(item.GoodId).Name;
        var what = item.Kind == MarketEventKind.Spike ? "price spike" : "price crash";
        return item.Status switch
        {
            NewsStatus.Confirmed => $"Intel confirmed: {good} {what}!",
            NewsStatus.Expired => $"Too late: the {good} {what} has blown over.",
            NewsStatus.False => $"The {good} {what} was a false rumour.",
            _ => "",
        };
    }

    private static ActionResult? Guard(GameState state) =>
        state.IsOver ? ActionResult.Fail("This run is over. Start a new one.")
        : state.PendingEncounter is not null ? ActionResult.Fail("Deal with the police first.")
        : null;

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
