using BlackMarketEmpire.Models;

namespace BlackMarketEmpire.Services;

/// <summary>
/// Creates market events (price spikes and crashes) and the headlines about them, including
/// false rumours. Truth is only revealed when the player visits the city.
/// </summary>
public sealed class EventService(IRandomSource random)
{
    private static readonly string[] SpikeHeadlines =
    [
        "Customs crackdown in {city}: {good} supply has dried up. Prices soaring.",
        "Collectors in {city} are paying crazy money for {good}.",
        "Rival crew busted in {city}. Nobody's selling {good} and buyers are desperate.",
        "Port strike in {city}. {good} can't get through and prices are through the roof.",
    ];

    private static readonly string[] CrashHeadlines =
    [
        "A container of {good} just landed in {city}. Prices crashing.",
        "Warehouse fire sale in {city}: {good} going dirt cheap.",
        "Police auction of seized {good} has flooded the {city} market.",
        "Everyone in {city} is dumping {good}. Bargains for anyone buying.",
    ];

    /// <summary>Gives a new run something on the wire from day one.</summary>
    public void SeedNewRun(GameState state, GameRules rules) => CreateHeadline(state, rules, inTransit: false);

    /// <summary>
    /// Called once for each day that passes: retires old events and maybe adds a headline.
    /// <paramref name="inTransit"/> means the player is on the road, so can't see local news first-hand.
    /// </summary>
    public void OnNewDay(GameState state, GameRules rules, bool inTransit = false)
    {
        state.Events.RemoveAll(e => e.EndDay < state.Day);
        state.Crackdowns.RemoveAll(c => c.EndDay < state.Day);

        AnnounceStartingEvents(state, rules, inTransit);

        if (random.NextDouble() < rules.EventChancePerDay)
        {
            CreateHeadline(state, rules, inTransit);
        }

        if (random.NextDouble() < rules.HiddenEventChancePerDay
            && state.Events.Count(e => e.StartDay > state.Day) < rules.MaxScheduledEvents)
        {
            var (cityId, goodId) = PickCityAndGood(state);
            var kind = random.NextDouble() < 0.5 ? MarketEventKind.Spike : MarketEventKind.Crash;
            ScheduleEvent(state, rules, cityId, goodId, kind, state.Day + random.Next(1, 4));
        }

        if (random.NextDouble() < rules.CrackdownChancePerDay)
        {
            var cityId = GameCatalog.Cities[random.Next(0, GameCatalog.Cities.Count)].Id;
            if (state.Crackdowns.All(c => c.CityId != cityId))
            {
                var start = state.Day + 1;
                state.Crackdowns.Add(new Crackdown
                {
                    CityId = cityId,
                    StartDay = start,
                    EndDay = start + random.Next(1, 4),
                    Known = state.Has("scanner"),
                });
            }
        }
    }

    /// <summary>Plans a real event that nobody knows about yet. Returns it so a tip can point at it.</summary>
    public MarketEvent ScheduleEvent(GameState state, GameRules rules, string cityId, string goodId, MarketEventKind kind, int startDay)
    {
        var (min, max) = kind == MarketEventKind.Spike
            ? (rules.SpikeMinMultiplier, rules.SpikeMaxMultiplier)
            : (rules.CrashMinMultiplier, rules.CrashMaxMultiplier);
        var duration = random.Next(rules.MinEventDays, rules.MaxEventDays + 1);
        var ev = new MarketEvent(cityId, goodId, kind,
            Multiplier: min + random.NextDouble() * (max - min),
            StartDay: startDay,
            EndDay: startDay + duration - 1,
            Announced: false,
            Id: Guid.NewGuid());
        state.Events.Add(ev);
        return ev;
    }

    /// <summary>Secret events that start today hit the news (unless you already had a tip about them).</summary>
    private void AnnounceStartingEvents(GameState state, GameRules rules, bool inTransit)
    {
        for (var i = 0; i < state.Events.Count; i++)
        {
            var ev = state.Events[i];
            if (ev.Announced || ev.StartDay > state.Day) continue;
            state.Events[i] = ev with { Announced = true };
            if (state.News.Any(n => n.EventId == ev.Id)) continue;

            var templates = ev.Kind == MarketEventKind.Spike ? SpikeHeadlines : CrashHeadlines;
            AddNews(state, new NewsItem
            {
                Day = state.Day,
                CityId = ev.CityId,
                GoodId = ev.GoodId,
                Kind = ev.Kind,
                Headline = Headline(templates[random.Next(0, templates.Length)], ev.CityId, ev.GoodId),
                IsTrue = true,
                EventId = ev.Id,
            }, rules, inTransit);
        }
    }

    public static string KindWord(MarketEventKind kind) => kind == MarketEventKind.Spike ? "spike" : "crash";

    /// <summary>
    /// The player is now in <paramref name="cityId"/>: settle any rumours about it.
    /// Returns the news items whose status changed, so the UI can tell the player.
    /// </summary>
    public IReadOnlyList<NewsItem> RevealCity(GameState state, string cityId)
    {
        if (state.CrackdownIn(cityId) is { } crackdown) crackdown.Known = true;

        var revealed = new List<NewsItem>();
        var unsettled = state.News.Where(n => n.CityId == cityId
            && (n.Status == NewsStatus.Rumour || (n.Status == NewsStatus.Tip && n.StartsDay <= state.Day)));
        foreach (var item in unsettled)
        {
            item.Status = !item.IsTrue
                ? NewsStatus.False
                : ActiveEvent(state, cityId, item.GoodId) is not null ? NewsStatus.Confirmed : NewsStatus.Expired;
            revealed.Add(item);
        }
        return revealed;
    }

    public static MarketEvent? ActiveEvent(GameState state, string cityId, string goodId) =>
        state.Events.FirstOrDefault(e => e.CityId == cityId && e.GoodId == goodId && e.IsActiveOn(state.Day));

    public static string Headline(string template, string cityId, string goodId) =>
        template
            .Replace("{city}", GameCatalog.City(cityId).Name)
            .Replace("{good}", GameCatalog.Good(goodId).Name.ToLowerInvariant());

    private void CreateHeadline(GameState state, GameRules rules, bool inTransit)
    {
        var isFalse = random.NextDouble() < rules.FalseRumourChance;
        var activeCount = state.Events.Count(e => e.IsActiveOn(state.Day));
        if (!isFalse && activeCount >= rules.MaxActiveEvents)
        {
            return;
        }

        var (cityId, goodId) = PickCityAndGood(state);

        var kind = random.NextDouble() < 0.5 ? MarketEventKind.Spike : MarketEventKind.Crash;
        var templates = kind == MarketEventKind.Spike ? SpikeHeadlines : CrashHeadlines;
        var item = new NewsItem
        {
            Day = state.Day,
            CityId = cityId,
            GoodId = goodId,
            Kind = kind,
            Headline = Headline(templates[random.Next(0, templates.Length)], cityId, goodId),
            IsTrue = !isFalse,
        };

        if (!isFalse)
        {
            var ev = ScheduleEvent(state, rules, cityId, goodId, kind, state.Day);
            state.Events[^1] = ev with { Announced = true };
            item.EventId = ev.Id;
        }

        AddNews(state, item, rules, inTransit);
    }

    /// <summary>Adds a headline to the feed. If it's about where you are, you see the truth straight away.</summary>
    public void AddNews(GameState state, NewsItem item, GameRules rules, bool inTransit)
    {
        state.News.Insert(0, item);
        if (state.News.Count > rules.MaxNewsItems)
        {
            state.News.RemoveRange(rules.MaxNewsItems, state.News.Count - rules.MaxNewsItems);
        }

        if (!inTransit && item.CityId == state.CityId && item.Status != NewsStatus.Tip)
        {
            RevealCity(state, item.CityId);
        }
    }

    /// <summary>A random city and a good traded there, with nothing already going on for it.</summary>
    private (string CityId, string GoodId) PickCityAndGood(GameState state)
    {
        string cityId, goodId;
        var attempts = 0;
        do
        {
            cityId = GameCatalog.Cities[random.Next(0, GameCatalog.Cities.Count)].Id;
            var city = cityId;
            var traded = GameCatalog.Goods.Where(g => g.TradedIn(city)).ToList();
            goodId = traded[random.Next(0, traded.Count)].Id;
        }
        while (++attempts < 20 && (state.Events.Any(e => e.CityId == cityId && e.GoodId == goodId && e.EndDay >= state.Day)
                                   || state.News.Any(n => n.CityId == cityId && n.GoodId == goodId && n.Day == state.Day)));
        return (cityId, goodId);
    }
}
