using BlackMarketEmpire.Tests.Fakes;

namespace BlackMarketEmpire.Tests.Services;

public class EventServiceTests
{
    private readonly EventService _events = new(new SystemRandomSource(new Random(11)));

    private static readonly GameRules AlwaysReal = new() { EventChancePerDay = 1, FalseRumourChance = 0, HiddenEventChancePerDay = 0, CrackdownChancePerDay = 0 };
    private static readonly GameRules AlwaysFalse = new() { EventChancePerDay = 1, FalseRumourChance = 1, HiddenEventChancePerDay = 0, CrackdownChancePerDay = 0 };

    private static GameState State(int day = 5, string city = "london") => new() { Day = day, CityId = city };

    [Fact]
    public void Real_headline_creates_an_event_within_the_rules()
    {
        var state = State();

        _events.OnNewDay(state, AlwaysReal);

        var ev = Assert.Single(state.Events);
        var news = Assert.Single(state.News);
        Assert.True(news.IsTrue);
        Assert.Equal((ev.CityId, ev.GoodId, ev.Kind), (news.CityId, news.GoodId, news.Kind));
        Assert.Equal(5, ev.StartDay);
        Assert.InRange(ev.EndDay - ev.StartDay + 1, AlwaysReal.MinEventDays, AlwaysReal.MaxEventDays);
        if (ev.Kind == MarketEventKind.Spike)
            Assert.InRange(ev.Multiplier, AlwaysReal.SpikeMinMultiplier, AlwaysReal.SpikeMaxMultiplier);
        else
            Assert.InRange(ev.Multiplier, AlwaysReal.CrashMinMultiplier, AlwaysReal.CrashMaxMultiplier);
        Assert.Contains(GameCatalog.City(ev.CityId).Name, news.Headline);
    }

    [Fact]
    public void False_rumour_creates_news_but_no_event()
    {
        var state = State();

        _events.OnNewDay(state, AlwaysFalse);

        Assert.Empty(state.Events);
        Assert.False(Assert.Single(state.News).IsTrue);
    }

    [Fact]
    public void No_headline_when_chance_is_zero()
    {
        var state = State();

        _events.OnNewDay(state, new GameRules { EventChancePerDay = 0, HiddenEventChancePerDay = 0, CrackdownChancePerDay = 0 });

        Assert.Empty(state.News);
    }

    [Fact]
    public void Finished_events_are_removed()
    {
        var state = State(day: 6);
        state.Events.Add(new MarketEvent("dubai", "phones", MarketEventKind.Spike, 3, StartDay: 2, EndDay: 5));
        state.Events.Add(new MarketEvent("dubai", "bags", MarketEventKind.Crash, 0.3, StartDay: 4, EndDay: 8));

        _events.OnNewDay(state, new GameRules { EventChancePerDay = 0, HiddenEventChancePerDay = 0, CrackdownChancePerDay = 0 });

        Assert.Equal("bags", Assert.Single(state.Events).GoodId);
    }

    [Fact]
    public void Active_event_cap_is_respected()
    {
        var state = State();
        var rules = AlwaysReal with { MaxActiveEvents = 2 };

        for (var i = 0; i < 10; i++) _events.OnNewDay(state, rules);

        Assert.Equal(2, state.Events.Count);
    }

    [Fact]
    public void News_feed_is_capped()
    {
        var state = State();
        var rules = AlwaysFalse with { MaxNewsItems = 4 };

        for (var i = 0; i < 10; i++) _events.OnNewDay(state, rules);

        Assert.Equal(4, state.News.Count);
    }

    [Fact]
    public void Visiting_a_city_settles_its_rumours()
    {
        var state = State(day: 6, city: "dubai");
        state.Events.Add(new MarketEvent("dubai", "phones", MarketEventKind.Spike, 3, 4, 8));
        var confirmed = new NewsItem { CityId = "dubai", GoodId = "phones", IsTrue = true, Day = 4 };
        var expired = new NewsItem { CityId = "dubai", GoodId = "art", IsTrue = true, Day = 1 };
        var fake = new NewsItem { CityId = "dubai", GoodId = "bags", IsTrue = false, Day = 5 };
        var elsewhere = new NewsItem { CityId = "london", GoodId = "bags", IsTrue = true, Day = 5 };
        state.News.AddRange([confirmed, expired, fake, elsewhere]);

        var revealed = _events.RevealCity(state, "dubai");

        Assert.Equal(3, revealed.Count);
        Assert.Equal(NewsStatus.Confirmed, confirmed.Status);
        Assert.Equal(NewsStatus.Expired, expired.Status);
        Assert.Equal(NewsStatus.False, fake.Status);
        Assert.Equal(NewsStatus.Rumour, elsewhere.Status);
    }

    [Fact]
    public void Event_in_your_own_city_is_seen_first_hand()
    {
        // Force the event into London by making it the only city/good combination worth checking.
        var state = State(city: "london");
        for (var i = 0; i < 40 && !state.News.Any(n => n.CityId == "london"); i++)
        {
            _events.OnNewDay(state, AlwaysFalse);
        }

        var local = state.News.First(n => n.CityId == "london");
        Assert.Equal(NewsStatus.False, local.Status);
        Assert.All(state.News.Where(n => n.CityId != "london"), n => Assert.Equal(NewsStatus.Rumour, n.Status));
    }

    [Fact]
    public void While_travelling_local_news_stays_a_rumour()
    {
        var state = State(city: "london");
        for (var i = 0; i < 40; i++) _events.OnNewDay(state, AlwaysFalse, inTransit: true);

        Assert.All(state.News, n => Assert.Equal(NewsStatus.Rumour, n.Status));
    }
}

public class MarketEventPricingTests
{
    [Fact]
    public void Spike_forces_availability_and_raises_price()
    {
        var market = new MarketService(new SystemRandomSource(new Random(5)));
        var rules = new GameRules { AvailabilityChance = 0 };
        var good = GameCatalog.Good("phones");
        var city = GameCatalog.City("dubai");
        MarketEvent[] events = [new("dubai", "phones", MarketEventKind.Spike, 3.0, 1, 5)];

        for (var i = 0; i < 20; i++)
        {
            var prices = market.GenerateMarket("dubai", rules, day: 3, events);
            Assert.True(prices.ContainsKey("phones"));
            Assert.True(prices["phones"] >= good.MinPrice * city.BiasFor("phones") * 3.0 - 1);
        }
    }

    [Fact]
    public void Crash_lowers_price_below_normal_range()
    {
        var market = new MarketService(new SystemRandomSource(new Random(5)));
        var rules = new GameRules { AvailabilityChance = 1 };
        var good = GameCatalog.Good("diamonds");
        MarketEvent[] events = [new("london", "diamonds", MarketEventKind.Crash, 0.25, 1, 5)];

        var prices = market.GenerateMarket("london", rules, day: 2, events);

        Assert.True(prices["diamonds"] <= good.MaxPrice * 0.25 + 1);
    }

    [Fact]
    public void Events_elsewhere_or_on_other_days_have_no_effect()
    {
        var rules = new GameRules { AvailabilityChance = 1 };
        MarketEvent[] events =
        [
            new("dubai", "phones", MarketEventKind.Spike, 100, 1, 5),
            new("london", "phones", MarketEventKind.Spike, 100, 10, 12),
        ];

        var withEvents = new MarketService(new SystemRandomSource(new Random(9))).GenerateMarket("london", rules, 3, events);
        var without = new MarketService(new SystemRandomSource(new Random(9))).GenerateMarket("london", rules, 3);

        Assert.Equal(without, withEvents);
    }
}

public class GameEngineEventTests
{
    [Fact]
    public void New_run_starts_with_a_headline()
    {
        var state = TestGame.Engine().NewGame();

        Assert.Single(state.News);
    }

    [Fact]
    public void Arriving_reveals_rumours_and_reports_them()
    {
        var engine = TestGame.Engine();
        var state = engine.NewGame();
        state.News.Clear();
        state.Events.Clear();
        state.News.Add(new NewsItem { CityId = "amsterdam", GoodId = "art", Kind = MarketEventKind.Spike, IsTrue = false, Day = 1 });

        var result = engine.Travel(state, "amsterdam");

        Assert.Equal(NewsStatus.False, state.News[0].Status);
        Assert.Contains("false rumour", result.Message);
    }

    [Fact]
    public void Active_event_shows_up_in_the_market_on_arrival()
    {
        var engine = TestGame.Engine();
        var state = engine.NewGame();
        state.Events.Add(new MarketEvent("amsterdam", "watches", MarketEventKind.Crash, 0.25, 1, 10));

        engine.Travel(state, "amsterdam");

        Assert.NotNull(engine.EventHere(state, "watches"));
        Assert.True(state.Market["watches"] <= GameCatalog.Good("watches").MaxPrice * 0.25 + 1);
    }

    [Fact]
    public void Events_expire_as_days_pass()
    {
        var engine = TestGame.Engine();
        var state = engine.NewGame();
        state.Events.Clear();
        state.Events.Add(new MarketEvent("london", "cigs", MarketEventKind.Spike, 3, 1, 2));

        engine.Wait(state);
        Assert.NotNull(engine.EventHere(state, "cigs"));
        engine.Wait(state);
        Assert.Null(engine.EventHere(state, "cigs"));
    }
}
