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

        if (random.NextDouble() < rules.EventChancePerDay)
        {
            CreateHeadline(state, rules, inTransit);
        }
    }

    /// <summary>
    /// The player is now in <paramref name="cityId"/>: settle any rumours about it.
    /// Returns the news items whose status changed, so the UI can tell the player.
    /// </summary>
    public IReadOnlyList<NewsItem> RevealCity(GameState state, string cityId)
    {
        var revealed = new List<NewsItem>();
        foreach (var item in state.News.Where(n => n.CityId == cityId && n.Status == NewsStatus.Rumour))
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

        // Pick a city/good with nothing already going on.
        string cityId, goodId;
        var attempts = 0;
        do
        {
            cityId = GameCatalog.Cities[random.Next(0, GameCatalog.Cities.Count)].Id;
            var city = cityId;
            var traded = GameCatalog.Goods.Where(g => g.TradedIn(city)).ToList();
            goodId = traded[random.Next(0, traded.Count)].Id;
        }
        while (++attempts < 20 && (ActiveEvent(state, cityId, goodId) is not null
                                   || state.News.Any(n => n.CityId == cityId && n.GoodId == goodId && n.Day == state.Day)));

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
            var (min, max) = kind == MarketEventKind.Spike
                ? (rules.SpikeMinMultiplier, rules.SpikeMaxMultiplier)
                : (rules.CrashMinMultiplier, rules.CrashMaxMultiplier);
            var duration = random.Next(rules.MinEventDays, rules.MaxEventDays + 1);
            state.Events.Add(new MarketEvent(
                cityId, goodId, kind,
                Multiplier: min + random.NextDouble() * (max - min),
                StartDay: state.Day,
                EndDay: state.Day + duration - 1));
        }

        state.News.Insert(0, item);
        if (state.News.Count > rules.MaxNewsItems)
        {
            state.News.RemoveRange(rules.MaxNewsItems, state.News.Count - rules.MaxNewsItems);
        }

        // If it's happening right where you are, you can see it for yourself.
        if (!inTransit && cityId == state.CityId)
        {
            RevealCity(state, cityId);
        }
    }
}
