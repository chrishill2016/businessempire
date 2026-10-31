using BlackMarketEmpire.Tests.Fakes;

namespace BlackMarketEmpire.Tests.Services;

public class TravelServiceTests
{
    private readonly GameEngine _engine = TestGame.Engine();

    private TravelOption Option(GameState state, string to, TravelMethod m) =>
        _engine.TravelOptions(state, to).Single(o => o.Method == m);

    [Fact]
    public void Train_and_car_only_on_overland_routes()
    {
        var state = _engine.NewGame(); // London

        Assert.True(Option(state, "amsterdam", TravelMethod.Train).Available);
        Assert.True(Option(state, "amsterdam", TravelMethod.Car).Available);
        Assert.False(Option(state, "hongkong", TravelMethod.Train).Available);
        Assert.False(Option(state, "dubai", TravelMethod.Car).Available);
    }

    [Fact]
    public void Boats_need_ports_at_both_ends()
    {
        var state = _engine.NewGame();
        Assert.False(Option(state, "amsterdam", TravelMethod.Boat).Available); // London has no port

        state.CityId = "marseille";
        Assert.True(Option(state, "istanbul", TravelMethod.Boat).Available);
    }

    [Fact]
    public void Flights_are_fastest_and_dearest_boats_slowest()
    {
        var state = _engine.NewGame();
        state.CityId = "marseille";
        state.Cash = 100_000;

        var train = Option(state, "istanbul", TravelMethod.Train);
        var flight = Option(state, "istanbul", TravelMethod.Flight);
        var boat = Option(state, "istanbul", TravelMethod.Boat);

        Assert.Equal(2, train.Days);
        Assert.Equal(1, flight.Days);
        Assert.Equal(3, boat.Days);
        Assert.True(flight.Fare > train.Fare);
        Assert.Equal(80, train.Fare);
        Assert.Equal(440, flight.Fare);
    }

    [Fact]
    public void Travel_charges_the_fare()
    {
        var state = _engine.NewGame();

        var result = _engine.Travel(state, "amsterdam", TravelMethod.Car);

        Assert.True(result.Success);
        Assert.Equal(2_500 - 90, state.Cash);
        Assert.Equal(90, state.Stats.FaresPaid);
        Assert.Equal(TravelMethod.Car, state.LastTravelMethod);
        Assert.Contains("by car", result.Message);
    }

    [Fact]
    public void Cannot_travel_without_the_fare()
    {
        var state = _engine.NewGame();
        state.Cash = 100;

        var result = _engine.Travel(state, "hongkong", TravelMethod.Flight);

        Assert.False(result.Success);
        Assert.Contains("can't afford", result.Message);
        Assert.Equal("london", state.CityId);
    }

    [Fact]
    public void Default_travel_takes_the_cheapest_available_method()
    {
        var state = _engine.NewGame();

        _engine.Travel(state, "marseille");

        Assert.Equal(TravelMethod.Train, state.LastTravelMethod);
    }

    [Fact]
    public void Unavailable_method_is_refused_with_a_reason()
    {
        var state = _engine.NewGame();

        var result = _engine.Travel(state, "hongkong", TravelMethod.Train);

        Assert.False(result.Success);
        Assert.Contains("no road or rail route", result.Message);
    }

    [Fact]
    public void Flying_is_riskier_than_a_boat()
    {
        var rules = GameRules.Default;
        var state = new GameState { CityId = "marseille", Capacity = 100, Inventory = { ["cigs"] = 50 }, AverageCost = { ["cigs"] = 40 } };

        var flight = EncounterService.StopChance(state, rules, TravelMethod.Flight, "istanbul");
        var boat = EncounterService.StopChance(state, rules, TravelMethod.Boat, "istanbul");

        Assert.True(flight > boat * 2);
    }
}

public class GearTests
{
    private readonly GameEngine _engine = TestGame.Engine();

    private GameState Rich()
    {
        var s = _engine.NewGame();
        s.Cash = 50_000;
        return s;
    }

    [Fact]
    public void Buying_a_bag_adds_capacity_once()
    {
        var state = Rich();

        Assert.True(_engine.BuyGear(state, "holdall").Success);
        Assert.Equal(150, state.Capacity);
        Assert.Equal(48_500, state.Cash);
        Assert.False(_engine.BuyGear(state, "holdall").Success);
        Assert.True(_engine.BuyGear(state, "trolley").Success);
        Assert.Equal(250, state.Capacity);
    }

    [Fact]
    public void Cannot_buy_gear_you_cannot_afford()
    {
        var state = _engine.NewGame();

        Assert.False(_engine.BuyGear(state, "trolley").Success);
        Assert.Empty(state.Gear);
    }

    [Fact]
    public void First_aid_kits_stack_and_heal()
    {
        var state = Rich();
        _engine.BuyGear(state, "firstaid");
        _engine.BuyGear(state, "firstaid");
        state.Health = 30;

        Assert.True(_engine.UseFirstAid(state).Success);
        Assert.Equal(70, state.Health);
        Assert.Equal(1, state.Gear["firstaid"]);
        _engine.UseFirstAid(state);
        Assert.Equal(100, state.Health);
        Assert.False(state.Has("firstaid"));
        Assert.False(_engine.UseFirstAid(state).Success);
    }

    [Fact]
    public void Concealment_gear_lowers_stop_chance()
    {
        var rules = GameRules.Default;
        var state = new GameState { CityId = "london", Capacity = 100, Inventory = { ["cigs"] = 50 }, AverageCost = { ["cigs"] = 40 } };
        var plain = EncounterService.StopChance(state, rules, TravelMethod.Flight, "dubai");

        state.Gear["lining"] = 1;
        var lined = EncounterService.StopChance(state, rules, TravelMethod.Flight, "dubai");
        state.Gear["docs"] = 1;
        var both = EncounterService.StopChance(state, rules, TravelMethod.Flight, "dubai");
        var trainWithDocs = EncounterService.StopChance(state, rules, TravelMethod.Train, "dubai");

        Assert.Equal(plain * 0.7, lined, 6);
        Assert.Equal(plain * 0.35, both, 6);
        Assert.Equal(EncounterService.StopChance(new GameState { Gear = { ["lining"] = 1 }, Capacity = 100, Inventory = { ["cigs"] = 50 }, AverageCost = { ["cigs"] = 40 } }, rules, TravelMethod.Train, "dubai"), trainWithDocs, 6);
    }

    [Fact]
    public void Shoes_and_car_improve_running()
    {
        var rules = GameRules.Default;
        var state = new GameState { Capacity = 100 };
        var plain = EncounterService.RunChance(state, rules, EncounterKind.Police);

        state.Gear["shoes"] = 1;
        state.LastTravelMethod = TravelMethod.Car;

        Assert.Equal(plain + 0.1 + rules.CarRunBonus, EncounterService.RunChance(state, rules, EncounterKind.Police), 6);
    }

    [Fact]
    public void Armour_and_burners_help()
    {
        var state = new GameState();
        Assert.Equal(20, EncounterService.Hurt(state, 20));
        Assert.Equal(GameRules.Default.StingChance, EncounterService.StingChance(state, GameRules.Default));

        state.Gear["armour"] = 1;
        state.Gear["burners"] = 1;
        Assert.Equal(12, EncounterService.Hurt(state, 20));
        Assert.Equal(GameRules.Default.StingChance * 0.3, EncounterService.StingChance(state, GameRules.Default), 6);
    }

    [Fact]
    public void Arrest_takes_concealment_gear_but_not_the_rest()
    {
        var random = new SystemRandomSource(new Random(1));
        var engine = new GameEngine(new MarketService(random), new EventService(random),
            new EncounterService(new ScriptedRandom(0)), new ShippingService(random), new FinanceService(random), TestGame.AllAvailable);
        var state = engine.NewGame();
        state.Gear["lining"] = 1;
        state.Gear["docs"] = 1;
        state.Gear["armour"] = 1;
        state.PendingEncounter = new Encounter { Kind = EncounterKind.Police, BribeCost = 300 };

        var result = engine.ResolveEncounter(state, EncounterChoice.Surrender);

        Assert.False(state.Has("lining"));
        Assert.False(state.Has("docs"));
        Assert.True(state.Has("armour"));
        Assert.Contains("hidden-lining case", result.Message);
    }
}

public class HeatTests
{
    [Fact]
    public void Heat_raises_stop_chance()
    {
        var rules = GameRules.Default;
        var state = new GameState { Capacity = 100, Inventory = { ["cigs"] = 10 }, AverageCost = { ["cigs"] = 20 } };
        var cold = EncounterService.StopChance(state, rules, TravelMethod.Train, "london");

        state.Heat["london"] = 100;

        Assert.Equal(cold + rules.StopChanceAtMaxHeat, EncounterService.StopChance(state, rules, TravelMethod.Train, "london"), 6);
    }

    [Fact]
    public void Heat_cools_each_day_and_is_capped()
    {
        var engine = TestGame.Engine();
        var state = engine.NewGame();
        EncounterService.AddHeat(state, "london", 500);
        Assert.Equal(100, state.HeatIn("london"));

        engine.Wait(state);

        Assert.Equal(95, state.HeatIn("london"));
    }

    [Fact]
    public void Big_deals_and_stops_draw_heat()
    {
        var engine = TestGame.Engine();
        var state = TestGame.StateWithMarket(engine, new() { ["diamonds"] = 10_000 });
        state.Cash = 20_000;

        engine.Buy(state, "diamonds", 1);

        Assert.Equal(engine.Rules.HeatOnBigDeal, state.HeatIn("london"));
    }

    [Fact]
    public void Arrest_heats_up_the_city()
    {
        var random = new SystemRandomSource(new Random(1));
        var engine = new GameEngine(new MarketService(random), new EventService(random),
            new EncounterService(new ScriptedRandom(0)), new ShippingService(random), new FinanceService(random), TestGame.AllAvailable);
        var state = engine.NewGame();
        state.PendingEncounter = new Encounter { Kind = EncounterKind.Police, BribeCost = 300 };

        engine.ResolveEncounter(state, EncounterChoice.Surrender); // 1 day jail → cools once

        Assert.Equal(engine.Rules.HeatOnArrest - engine.Rules.HeatCoolPerDay, state.HeatIn("london"));
    }
}

public class ShippingTests
{
    private static GameEngine Engine(params double[] shippingRolls)
    {
        var random = new SystemRandomSource(new Random(1));
        return new GameEngine(new MarketService(random), new EventService(random), new EncounterService(random),
            new ShippingService(new ScriptedRandom(shippingRolls)), new FinanceService(random), TestGame.AllAvailable);
    }

    private static GameState WithCigs(GameEngine engine, int qty = 20)
    {
        var state = TestGame.StateWithMarket(engine, new() { ["cigs"] = 50 });
        engine.Buy(state, "cigs", qty); // 50 each
        return state;
    }

    [Fact]
    public void Quote_uses_fee_rate_with_a_minimum()
    {
        var engine = Engine();
        var state = WithCigs(engine);

        var pro = engine.QuoteShipping(state, "cigs", 20, "dubai", "pro");       // 1,000 value × 25%
        var budget = engine.QuoteShipping(state, "cigs", 1, "amsterdam", "budget"); // tiny → minimum

        Assert.Equal(250, pro.Fee);
        Assert.Equal(2, pro.Days);                    // London–Dubai flight is 2 days
        Assert.Equal(ShippingService.MinimumFee, budget.Fee);
        Assert.Equal(2, budget.Days);                 // 1-day flight + 1 extra
    }

    [Fact]
    public void Shipping_takes_goods_and_fee()
    {
        var engine = Engine();
        var state = WithCigs(engine);

        var result = engine.Ship(state, "cigs", 15, "dubai", "pro");

        Assert.True(result.Success);
        Assert.Equal(5, state.Inventory["cigs"]);
        var s = Assert.Single(state.Shipments);
        Assert.Equal(15, s.Quantity);
        Assert.Equal(50, s.AverageCost);
        Assert.Equal(3, s.ArrivalDay);
        Assert.Equal(2_500 - 1_000 - 188, state.Cash);
    }

    [Fact]
    public void Cannot_ship_to_where_you_are_or_more_than_you_have()
    {
        var engine = Engine();
        var state = WithCigs(engine);

        Assert.False(engine.Ship(state, "cigs", 5, "london", "pro").Success);
        Assert.False(engine.Ship(state, "cigs", 21, "dubai", "pro").Success);
    }

    [Fact]
    public void Cannot_ship_if_it_arrives_after_the_last_day()
    {
        var engine = Engine();
        var state = WithCigs(engine);
        state.Day = 29;

        Assert.False(engine.Ship(state, "cigs", 5, "hongkong", "pro").Success);
    }

    [Fact]
    public void Delivered_goods_wait_to_be_collected()
    {
        var engine = Engine(0.99); // safe delivery
        var state = WithCigs(engine);
        engine.Ship(state, "cigs", 20, "amsterdam", "pro");

        engine.Travel(state, "amsterdam", TravelMethod.Train); // 1 day — shipment due day 2

        Assert.Equal(ShipmentStatus.Delivered, state.Shipments[0].Status);
        Assert.Empty(state.Inventory);
        Assert.True(engine.Collect(state).Success);
        Assert.Equal(20, state.Inventory["cigs"]);
        Assert.Equal(50, state.AverageCost["cigs"]);
        Assert.Empty(state.Shipments);
    }

    [Fact]
    public void Collect_respects_suitcase_space()
    {
        var engine = Engine(0.99);
        var state = WithCigs(engine);
        engine.Ship(state, "cigs", 20, "amsterdam", "pro");
        engine.Travel(state, "amsterdam", TravelMethod.Train);
        state.Inventory["whisky"] = 90;
        state.AverageCost["whisky"] = 10;

        var result = engine.Collect(state);

        Assert.Equal(10, state.Inventory["cigs"]);
        Assert.Equal(10, state.Shipments[0].Quantity);
        Assert.Contains("10 more are waiting", result.Message);
    }

    [Fact]
    public void Shipments_can_be_seized_or_stolen()
    {
        var seizedEngine = Engine(0.0);
        var seized = WithCigs(seizedEngine);
        seizedEngine.Ship(seized, "cigs", 20, "amsterdam", "budget");
        seizedEngine.Wait(seized);
        seizedEngine.Wait(seized);

        var stolenEngine = Engine(0.22); // budget: 0.20 seizure, 0.05 theft
        var stolen = WithCigs(stolenEngine);
        stolenEngine.Ship(stolen, "cigs", 20, "amsterdam", "budget");
        stolenEngine.Wait(stolen);
        stolenEngine.Wait(stolen);

        Assert.Equal(ShipmentStatus.Seized, seized.Shipments[0].Status);
        Assert.Equal(1, seized.Stats.ShipmentsLost);
        Assert.Contains(seized.Log, l => l.Contains("seized your shipment"));
        Assert.Equal(ShipmentStatus.Stolen, stolen.Shipments[0].Status);
        Assert.Contains(stolen.Log, l => l.Contains("courier vanished"));
    }

    [Fact]
    public void Lost_shipments_drop_off_the_list_after_a_few_days()
    {
        var engine = Engine(0.0);
        var state = WithCigs(engine);
        engine.Ship(state, "cigs", 20, "amsterdam", "budget");

        for (var i = 0; i < 6; i++) engine.Wait(state);

        Assert.Empty(state.Shipments);
    }
}
