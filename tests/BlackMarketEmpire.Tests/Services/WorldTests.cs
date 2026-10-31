using BlackMarketEmpire.Tests.Fakes;

namespace BlackMarketEmpire.Tests.Services;

public class WorldTests
{
    [Fact]
    public void World_has_fourteen_cities_in_four_regions()
    {
        Assert.Equal(14, GameCatalog.Cities.Count);
        Assert.All(GameCatalog.Cities, c => Assert.Contains(c.Region, GameCatalog.Regions));
        Assert.All(GameCatalog.Regions, r => Assert.Contains(GameCatalog.Cities, c => c.Region == r));
    }

    [Theory]
    [InlineData("london", "amsterdam", 1)]
    [InlineData("london", "istanbul", 2)]
    [InlineData("london", "newyork", 3)]
    [InlineData("london", "sydney", 4)]
    [InlineData("newyork", "miami", 2)]
    [InlineData("hongkong", "bangkok", 2)]
    public void Travel_days_come_from_distance(string a, string b, int days)
    {
        Assert.Equal(days, GameCatalog.DaysBetween(a, b));
        Assert.Equal(days, GameCatalog.DaysBetween(b, a));
    }

    [Fact]
    public void Overland_only_within_linked_groups()
    {
        Assert.True(GameCatalog.HasOverlandRoute("london", "moscow"));
        Assert.True(GameCatalog.HasOverlandRoute("newyork", "miami"));
        Assert.True(GameCatalog.HasOverlandRoute("istanbul", "dubai"));
        Assert.False(GameCatalog.HasOverlandRoute("london", "newyork"));
        Assert.False(GameCatalog.HasOverlandRoute("miami", "bogota"));
        Assert.False(GameCatalog.HasOverlandRoute("bangkok", "sydney"));
    }

    [Fact]
    public void Every_city_can_be_reached_by_air_from_every_other()
    {
        var engine = TestGame.Engine();
        var state = engine.NewGame();
        state.Cash = 1_000_000;
        foreach (var from in GameCatalog.Cities)
        {
            state.CityId = from.Id;
            foreach (var to in GameCatalog.Cities.Where(c => c.Id != from.Id))
            {
                Assert.True(engine.TravelOptions(state, to.Id).Single(o => o.Method == TravelMethod.Flight).Available, $"{from.Id}->{to.Id}");
            }
        }
    }

    [Fact]
    public void Bogota_is_flights_only()
    {
        var engine = TestGame.Engine();
        var state = engine.NewGame();
        state.CityId = "miami";
        state.Cash = 100_000;

        var available = engine.TravelOptions(state, "bogota").Where(o => o.Available).Select(o => o.Method);

        Assert.Equal([TravelMethod.Flight], available);
    }

    [Fact]
    public void Every_lender_and_bank_city_exists()
    {
        var ids = GameCatalog.Cities.Select(c => c.Id).ToHashSet();
        Assert.All(GameCatalog.Lenders.SelectMany(l => l.CityIds), id => Assert.Contains(id, ids));
        Assert.All(GameCatalog.BankCities, id => Assert.Contains(id, ids));
        Assert.All(GameCatalog.Ports, id => Assert.Contains(id, ids));
        Assert.All(GameCatalog.Goods.Where(g => g.CityIds is not null).SelectMany(g => g.CityIds!), id => Assert.Contains(id, ids));
    }

    [Fact]
    public void Every_city_bias_names_a_real_good()
    {
        var goods = GameCatalog.Goods.Select(g => g.Id).ToHashSet();
        Assert.All(GameCatalog.Cities.SelectMany(c => c.PriceBias.Keys), id => Assert.Contains(id, goods));
    }
}

public class GoodsTests
{
    [Fact]
    public void Fourteen_goods_with_unique_ids()
    {
        Assert.Equal(14, GameCatalog.Goods.Count);
        Assert.Equal(14, GameCatalog.Goods.Select(g => g.Id).Distinct().Count());
    }

    [Fact]
    public void Regional_goods_only_trade_in_their_cities()
    {
        var market = new MarketService(new SystemRandomSource(new Random(2)));
        var rules = new GameRules { AvailabilityChance = 1 };

        var sydney = market.GenerateMarket("sydney", rules);
        var lagos = market.GenerateMarket("lagos", rules);

        Assert.DoesNotContain("fuel", sydney.Keys);
        Assert.DoesNotContain("cards", sydney.Keys);
        Assert.Contains("fuel", lagos.Keys);
        Assert.Contains("cards", lagos.Keys);
    }

    [Fact]
    public void Bulky_goods_use_more_space()
    {
        var engine = TestGame.Engine();
        var state = TestGame.StateWithMarket(engine, new() { ["art"] = 10, ["cigs"] = 1 });
        state.Cash = 1_000_000;

        Assert.Equal(33, engine.MaxBuyable(state, "art")); // 100 space / 3
        engine.Buy(state, "art", 30);

        Assert.Equal(90, state.SpaceUsed);
        Assert.Equal(30, state.UnitsHeld);
        Assert.Equal(10, state.FreeSpace);
        Assert.Equal(3, engine.MaxBuyable(state, "art"));
        Assert.Equal(10, engine.MaxBuyable(state, "cigs"));
        var result = engine.Buy(state, "art", 4);
        Assert.False(result.Success);
        Assert.Contains("3 spaces", result.Message);
    }

    [Fact]
    public void Bulky_goods_raise_stop_chance_by_space_not_count()
    {
        var rules = GameRules.Default;
        var phones = new GameState { Capacity = 100, Inventory = { ["phones"] = 10 } };
        var art = new GameState { Capacity = 100, Inventory = { ["art"] = 10 } };

        Assert.True(EncounterService.StopChance(art, rules, TravelMethod.Train, "london")
                    > EncounterService.StopChance(phones, rules, TravelMethod.Train, "london"));
    }

    [Fact]
    public void Stash_capacity_counts_space()
    {
        var engine = TestGame.Engine();
        var state = TestGame.StateWithMarket(engine, new() { ["art"] = 1 });
        state.Cash = 1_000_000;
        engine.Buy(state, "art", 33);
        engine.RentStash(state, StashTier.Lockup); // 150 space

        engine.Store(state, "art", 33);

        Assert.Equal(99, state.StashIn("london")!.SpaceUsed);
        Assert.Equal(33, state.StashIn("london")!.Units);
    }

    [Fact]
    public void Events_only_hit_goods_traded_in_that_city()
    {
        var events = new EventService(new SystemRandomSource(new Random(8)));
        var rules = new GameRules { EventChancePerDay = 1, FalseRumourChance = 0, MaxActiveEvents = 1_000 };
        var state = new GameState { Day = 1, CityId = "london" };

        for (var i = 0; i < 300; i++) events.OnNewDay(state, rules);

        Assert.All(state.News, n => Assert.True(GameCatalog.Good(n.GoodId).TradedIn(n.CityId), $"{n.GoodId} in {n.CityId}"));
    }

    [Fact]
    public void Best_trade_is_tracked()
    {
        var engine = TestGame.Engine();
        var state = TestGame.StateWithMarket(engine, new() { ["cigs"] = 10 });
        engine.Buy(state, "cigs", 50);
        state.Market["cigs"] = 30;
        engine.Sell(state, "cigs", 10); // +200
        state.Market["cigs"] = 50;
        engine.Sell(state, "cigs", 40); // +1,600
        engine.Buy(state, "cigs", 1);
        state.Market["cigs"] = 1;
        engine.Sell(state, "cigs", 1);  // loss

        Assert.Equal(1_600, state.Stats.BestTrade);
    }
}
