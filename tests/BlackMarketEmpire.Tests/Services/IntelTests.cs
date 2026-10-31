using BlackMarketEmpire.Tests.Fakes;

namespace BlackMarketEmpire.Tests.Services;

public class HiddenEventTests
{
    private static readonly GameRules OnlyHidden = new()
    {
        EventChancePerDay = 0, HiddenEventChancePerDay = 1, CrackdownChancePerDay = 0, MaxScheduledEvents = 1,
    };

    [Fact]
    public void Secret_events_are_planned_without_news_and_announced_when_they_start()
    {
        var events = new EventService(new SystemRandomSource(new Random(3)));
        var state = new GameState { Day = 5, CityId = "london" };

        events.OnNewDay(state, OnlyHidden);

        var ev = Assert.Single(state.Events);
        Assert.False(ev.Announced);
        Assert.True(ev.StartDay > 5);
        Assert.Empty(state.News);
        Assert.False(ev.IsActiveOn(5));

        var rules = OnlyHidden with { HiddenEventChancePerDay = 0 };
        while (state.Day < ev.StartDay)
        {
            state.Day++;
            events.OnNewDay(state, rules);
        }

        var news = Assert.Single(state.News);
        Assert.Equal(ev.Id, news.EventId);
        Assert.True(state.Events.Single().Announced);
    }

    [Fact]
    public void Crackdowns_raise_stop_chance()
    {
        var rules = GameRules.Default;
        var state = new GameState { Day = 4, Capacity = 100, Inventory = { ["cigs"] = 10 }, AverageCost = { ["cigs"] = 20 } };
        var before = EncounterService.StopChance(state, rules, TravelMethod.Train, "london");

        state.Crackdowns.Add(new Crackdown { CityId = "london", StartDay = 3, EndDay = 6 });

        Assert.Equal(before * rules.CrackdownStopFactor, EncounterService.StopChance(state, rules, TravelMethod.Train, "london"), 6);
    }

    [Fact]
    public void Scanner_owners_know_about_crackdowns_as_they_are_planned()
    {
        var events = new EventService(new SystemRandomSource(new Random(1)));
        var rules = new GameRules { EventChancePerDay = 0, HiddenEventChancePerDay = 0, CrackdownChancePerDay = 1 };
        var plain = new GameState { Day = 2 };
        var scanner = new GameState { Day = 2, Gear = { ["scanner"] = 1 } };

        events.OnNewDay(plain, rules);
        events.OnNewDay(scanner, rules);

        Assert.False(Assert.Single(plain.Crackdowns).Known);
        Assert.True(Assert.Single(scanner.Crackdowns).Known);
    }

    [Fact]
    public void Arriving_during_a_crackdown_reveals_it()
    {
        var engine = TestGame.Engine();
        var state = engine.NewGame();
        state.Crackdowns.Add(new Crackdown { CityId = "amsterdam", StartDay = 1, EndDay = 9 });

        engine.Travel(state, "amsterdam", TravelMethod.Train);

        Assert.True(state.Crackdowns[0].Known);
    }
}

public class IntelServiceTests
{
    private static (GameEngine Engine, GameState State) Setup(params double[] intelRolls)
    {
        var random = new SystemRandomSource(new Random(1));
        var events = new EventService(random);
        var engine = new GameEngine(new MarketService(random), events, new EncounterService(random), new ShippingService(random),
            new FinanceService(random), TestGame.AllAvailable, new IntelService(new ScriptedRandom(intelRolls), events));
        var state = engine.NewGame();
        state.News.Clear();
        state.Events.Clear();
        state.Cash = 100_000;
        return (engine, state);
    }

    private static NewsItem Rumour(GameState state, bool isTrue, Guid? eventId = null)
    {
        var n = new NewsItem { Day = 1, CityId = "dubai", GoodId = "phones", Kind = MarketEventKind.Spike, Headline = "x", IsTrue = isTrue, EventId = eventId };
        state.News.Add(n);
        return n;
    }

    [Fact]
    public void Informant_who_gets_it_right()
    {
        var (engine, state) = Setup(0.0);
        var rumour = Rumour(state, isTrue: false);

        var result = engine.CheckRumour(state, rumour.Id, InformantTier.Street);

        Assert.True(result.Success);
        Assert.False(rumour.InformantSaysTrue);
        Assert.Equal(0.75, rumour.InformantAccuracy, 6);
        Assert.Equal(100_000 - 150, state.Cash);
        Assert.Equal(1, state.TrustIn("london"));
        Assert.Contains("rubbish", result.Message);
    }

    [Fact]
    public void Informant_who_gets_it_wrong()
    {
        var (engine, state) = Setup(0.99);
        var rumour = Rumour(state, isTrue: false);

        engine.CheckRumour(state, rumour.Id, InformantTier.Street);

        Assert.True(rumour.InformantSaysTrue);
    }

    [Fact]
    public void Connected_informant_reveals_size_and_length()
    {
        var (engine, state) = Setup(0.0);
        var ev = new MarketEvent("dubai", "phones", MarketEventKind.Spike, 3.2, 1, 6, Id: Guid.NewGuid());
        state.Events.Add(ev);
        var rumour = Rumour(state, isTrue: true, ev.Id);

        var result = engine.CheckRumour(state, rumour.Id, InformantTier.Connected);

        Assert.Equal(3.2, rumour.KnownMultiplier);
        Assert.Equal(6, rumour.KnownEndDay);
        Assert.Contains("×3.2", result.Message);
        Assert.Equal(100_000 - 600, state.Cash);
    }

    [Fact]
    public void Trust_improves_accuracy_up_to_a_cap()
    {
        var (_, state) = Setup();
        var rules = GameRules.Default;
        var start = IntelService.Accuracy(state, rules, InformantTier.Street);

        state.InformantTrust["london"] = 5;

        Assert.Equal(start + 0.15, IntelService.Accuracy(state, rules, InformantTier.Street), 6);
        Assert.Equal(0.99, IntelService.Accuracy(state, rules, InformantTier.Connected), 6);
    }

    [Fact]
    public void Cannot_check_settled_news()
    {
        var (engine, state) = Setup();
        var rumour = Rumour(state, isTrue: true);
        rumour.Status = NewsStatus.Confirmed;

        Assert.False(engine.CheckRumour(state, rumour.Id, InformantTier.Street).Success);
        Assert.Equal(100_000, state.Cash);
    }

    [Fact]
    public void Tip_off_reveals_the_next_secret_event_once()
    {
        var (engine, state) = Setup();
        var ev = new MarketEvent("bangkok", "games", MarketEventKind.Crash, 0.3, 4, 6, Announced: false, Id: Guid.NewGuid());
        state.Events.Add(ev);

        var result = engine.BuyTipOff(state);

        Assert.True(result.Success);
        var tip = Assert.Single(state.News);
        Assert.Equal(NewsStatus.Tip, tip.Status);
        Assert.Equal(4, tip.StartsDay);
        Assert.Equal(ev.Id, tip.EventId);
        Assert.Equal(100_000 - 1_000, state.Cash);

        var again = engine.BuyTipOff(state);
        Assert.False(again.Success);
        Assert.Contains("no charge", again.Message);
        Assert.Equal(100_000 - 1_000, state.Cash);
    }

    [Fact]
    public void Tip_off_falls_back_to_a_crackdown()
    {
        var (engine, state) = Setup();
        state.Crackdowns.Add(new Crackdown { CityId = "miami", StartDay = 3, EndDay = 5 });

        var result = engine.BuyTipOff(state);

        Assert.True(result.Success);
        Assert.True(state.Crackdowns[0].Known);
        Assert.Contains("Miami", result.Message);
    }

    [Fact]
    public void Tipped_event_does_not_get_a_second_headline_when_it_starts()
    {
        var (engine, state) = Setup();
        state.Events.Add(new MarketEvent("bangkok", "games", MarketEventKind.Crash, 0.3, 3, 6, Announced: false, Id: Guid.NewGuid()));
        engine.BuyTipOff(state);

        engine.Wait(state);
        engine.Wait(state);

        Assert.Single(state.News);
        Assert.True(state.Events.Single().Announced);
    }

    [Fact]
    public void Tip_is_confirmed_when_you_get_there_after_it_starts()
    {
        var (engine, state) = Setup();
        state.Events.Add(new MarketEvent("amsterdam", "games", MarketEventKind.Crash, 0.3, 2, 6, Announced: false, Id: Guid.NewGuid()));
        engine.BuyTipOff(state);

        engine.Travel(state, "amsterdam", TravelMethod.Train);

        Assert.Equal(NewsStatus.Confirmed, state.News.Single().Status);
        Assert.NotNull(engine.EventHere(state, "games"));
    }

    [Fact]
    public void Contact_reports_prices_events_and_crackdowns()
    {
        var (engine, state) = Setup();
        state.Events.Add(new MarketEvent("dubai", "phones", MarketEventKind.Spike, 3, 1, 5, Id: Guid.NewGuid()));
        state.Crackdowns.Add(new Crackdown { CityId = "dubai", StartDay = 1, EndDay = 3 });

        var result = engine.BuyContactReport(state, "dubai");

        Assert.True(result.Success);
        var report = state.ContactReports["dubai"];
        Assert.Equal(1, report.Day);
        Assert.True(report.Prices.ContainsKey("phones"));
        Assert.Single(report.Events);
        Assert.True(report.Crackdown);
        Assert.True(state.Crackdowns[0].Known);
        Assert.Equal(100_000 - 400, state.Cash);
        Assert.False(engine.BuyContactReport(state, "london").Success);
    }

    [Fact]
    public void Fixer_who_delivers_creates_a_real_spike()
    {
        var (engine, state) = Setup(0.0);

        var result = engine.ArrangeShortage(state, "dubai", "watches");

        Assert.True(result.Success);
        var ev = Assert.Single(state.Events);
        Assert.Equal(MarketEventKind.Spike, ev.Kind);
        Assert.Equal(3, ev.StartDay);
        Assert.False(ev.Announced);
        var tip = Assert.Single(state.News);
        Assert.True(tip.IsTrue);
        Assert.Equal(NewsStatus.Tip, tip.Status);
        Assert.Equal(100_000 - IntelService.FixerPrice(engine.Rules, "watches"), state.Cash);
    }

    [Fact]
    public void Fixer_who_does_not_deliver_leaves_a_false_tip()
    {
        var (engine, state) = Setup(0.99);

        engine.ArrangeShortage(state, "amsterdam", "games");
        Assert.Empty(state.Events);

        engine.Travel(state, "amsterdam", TravelMethod.Train); // day 2 — not due yet
        Assert.Equal(NewsStatus.Tip, state.News.Single().Status);
        var result = engine.Wait(state); // day 3 — due, and you're there to see it didn't happen

        Assert.Equal(NewsStatus.False, state.News.Single().Status);
        Assert.Contains("false rumour", result.Message);
    }

    [Fact]
    public void Fixer_refuses_goods_not_traded_there_or_markets_already_moving()
    {
        var (engine, state) = Setup(0.0);

        Assert.False(engine.ArrangeShortage(state, "sydney", "fuel").Success);
        engine.ArrangeShortage(state, "dubai", "watches");
        Assert.False(engine.ArrangeShortage(state, "dubai", "watches").Success);
    }

    [Fact]
    public void Fixer_price_scales_with_the_good()
    {
        var rules = GameRules.Default;

        Assert.True(IntelService.FixerPrice(rules, "diamonds") > IntelService.FixerPrice(rules, "cigs"));
        Assert.Equal(1_500, IntelService.FixerPrice(rules, "cigs"));
    }
}
