using BlackMarketEmpire.Models;

namespace BlackMarketEmpire.Services;

/// <summary>Paid information: informants, tip-offs, market contacts and fixers.</summary>
public sealed class IntelService(IRandomSource random, EventService events)
{
    public const int MaxTrust = 5;

    public static long CheckPrice(GameRules rules, InformantTier tier) =>
        tier == InformantTier.Connected ? rules.ConnectedCheckPrice : rules.StreetCheckPrice;

    /// <summary>How often this city's informant gets it right, given how much they trust you.</summary>
    public static double Accuracy(GameState state, GameRules rules, InformantTier tier)
    {
        var trust = state.TrustIn(state.CityId);
        return tier == InformantTier.Connected
            ? Math.Min(0.99, rules.ConnectedAccuracy + trust * 0.01)
            : Math.Min(0.95, rules.StreetAccuracy + trust * rules.AccuracyPerTrust);
    }

    public static long FixerPrice(GameRules rules, string goodId) =>
        rules.FixerBasePrice + (long)(GameCatalog.Good(goodId).MaxPrice * rules.FixerPriceFactor) / 100 * 100;

    public static double FixerChance(GameState state, GameRules rules) =>
        Math.Min(0.9, rules.FixerSuccessChance + state.TrustIn(state.CityId) * rules.FixerSuccessPerTrust);

    /// <summary>Ask the local informant whether a rumour is real.</summary>
    public ActionResult CheckRumour(GameState state, GameRules rules, Guid newsId, InformantTier tier)
    {
        var item = state.News.FirstOrDefault(n => n.Id == newsId);
        if (item is null || item.Status != NewsStatus.Rumour) return ActionResult.Fail("There's nothing left to check about that.");
        var price = CheckPrice(rules, tier);
        if (state.Cash < price) return ActionResult.Fail($"The informant wants {Money.Format(price)}.");

        var accuracy = Accuracy(state, rules, tier);
        state.Cash -= price;
        Trust(state);

        var correct = random.NextDouble() < accuracy;
        item.InformantSaysTrue = correct ? item.IsTrue : !item.IsTrue;
        item.InformantAccuracy = accuracy;

        var good = GameCatalog.Good(item.GoodId).Name.ToLowerInvariant();
        var city = GameCatalog.City(item.CityId).Name;
        if (item.InformantSaysTrue == false)
            return ActionResult.Ok($"Your informant says the {good} {EventService.KindWord(item.Kind)} in {city} is rubbish.");

        if (tier == InformantTier.Connected && item.IsTrue && correct
            && state.Events.FirstOrDefault(e => e.Id == item.EventId) is { } ev)
        {
            item.KnownMultiplier = ev.Multiplier;
            item.KnownEndDay = ev.EndDay;
            return ActionResult.Ok($"Your informant says it's real: {good} at ×{ev.Multiplier:0.0} normal price in {city} until day {ev.EndDay}.");
        }
        return ActionResult.Ok($"Your informant says the {good} {EventService.KindWord(item.Kind)} in {city} is real.");
    }

    /// <summary>Buy word of the next big thing before it happens: a secret market event, or failing that a crackdown.</summary>
    public ActionResult BuyTipOff(GameState state, GameRules rules)
    {
        if (state.Cash < rules.TipOffPrice) return ActionResult.Fail($"A tip-off costs {Money.Format(rules.TipOffPrice)}.");

        var tipped = state.News.Where(n => n.EventId is not null).Select(n => n.EventId).ToHashSet();
        var upcoming = state.Events
            .Where(e => !e.Announced && e.StartDay > state.Day && !tipped.Contains(e.Id))
            .OrderBy(e => e.StartDay)
            .FirstOrDefault();

        if (upcoming is not null)
        {
            state.Cash -= rules.TipOffPrice;
            Trust(state);
            var good = GameCatalog.Good(upcoming.GoodId).Name;
            var city = GameCatalog.City(upcoming.CityId).Name;
            events.AddNews(state, new NewsItem
            {
                Day = state.Day,
                CityId = upcoming.CityId,
                GoodId = upcoming.GoodId,
                Kind = upcoming.Kind,
                Headline = $"Inside word: {good} will {(upcoming.Kind == MarketEventKind.Spike ? "spike" : "crash")} in {city} from day {upcoming.StartDay}.",
                IsTrue = true,
                Status = NewsStatus.Tip,
                StartsDay = upcoming.StartDay,
                EventId = upcoming.Id,
            }, rules, inTransit: false);
            return ActionResult.Ok($"Tip-off: {good} {EventService.KindWord(upcoming.Kind)} in {city}, starting day {upcoming.StartDay}.");
        }

        var crackdown = state.Crackdowns.Where(c => !c.Known).OrderBy(c => c.StartDay).FirstOrDefault();
        if (crackdown is not null)
        {
            state.Cash -= rules.TipOffPrice;
            Trust(state);
            crackdown.Known = true;
            return ActionResult.Ok($"Tip-off: police crackdown in {GameCatalog.City(crackdown.CityId).Name}, days {crackdown.StartDay}–{crackdown.EndDay}. Stay away or travel light.");
        }

        return ActionResult.Fail("Nobody's heard of anything big coming. Try again tomorrow — no charge.");
    }

    /// <summary>Pay a contact in another city for today's prices, what's going on there and whether the police are active.</summary>
    public ActionResult BuyContactReport(GameState state, GameRules rules, MarketService market, string cityId)
    {
        if (cityId == state.CityId) return ActionResult.Fail("You can see this market for yourself.");
        if (state.Cash < rules.ContactPrice) return ActionResult.Fail($"A contact costs {Money.Format(rules.ContactPrice)}.");

        state.Cash -= rules.ContactPrice;
        var crackdown = state.CrackdownIn(cityId);
        if (crackdown is not null) crackdown.Known = true;
        var report = new ContactReport
        {
            CityId = cityId,
            Day = state.Day,
            Prices = market.GenerateMarket(cityId, rules, state.Day, state.Events),
            Events = state.Events.Where(e => e.CityId == cityId && e.IsActiveOn(state.Day)).ToList(),
            Crackdown = crackdown is not null,
        };
        state.ContactReports[cityId] = report;

        var city = GameCatalog.City(cityId).Name;
        var happening = report.Events.Count == 0
            ? "nothing unusual"
            : string.Join(", ", report.Events.Select(e => $"{GameCatalog.Good(e.GoodId).Name.ToLowerInvariant()} {EventService.KindWord(e.Kind)}"));
        return ActionResult.Ok($"Your contact in {city} reports {happening}{(report.Crackdown ? " — and a police crackdown" : "")}.");
    }

    /// <summary>Pay a fixer to arrange a shortage of a good in a city, starting in two days. It might not come off.</summary>
    public ActionResult ArrangeShortage(GameState state, GameRules rules, string cityId, string goodId)
    {
        var good = GameCatalog.Good(goodId);
        if (!good.TradedIn(cityId)) return ActionResult.Fail($"{good.Name} isn't traded in {GameCatalog.City(cityId).Name}.");
        var startDay = state.Day + 2;
        if (startDay > rules.TotalDays) return ActionResult.Fail("Too late in the run to set that up.");
        if (state.Events.Any(e => e.CityId == cityId && e.GoodId == goodId && e.EndDay >= state.Day))
            return ActionResult.Fail("Something's already going on with that market.");
        var price = FixerPrice(rules, goodId);
        if (state.Cash < price) return ActionResult.Fail($"The fixer wants {Money.Format(price)}.");

        var chance = FixerChance(state, rules);
        state.Cash -= price;
        Trust(state);

        var city = GameCatalog.City(cityId).Name;
        var item = new NewsItem
        {
            Day = state.Day,
            CityId = cityId,
            GoodId = goodId,
            Kind = MarketEventKind.Spike,
            Headline = $"Your fixer is arranging a {good.Name.ToLowerInvariant()} shortage in {city} from day {startDay}.",
            Status = NewsStatus.Tip,
            StartsDay = startDay,
        };
        if (random.NextDouble() < chance)
        {
            var ev = events.ScheduleEvent(state, rules, cityId, goodId, MarketEventKind.Spike, startDay);
            item.IsTrue = true;
            item.EventId = ev.Id;
            state.Stats.FixerSuccesses++;
        }
        events.AddNews(state, item, rules, inTransit: false);
        return ActionResult.Ok($"Paid the fixer {Money.Format(price)}. A {good.Name.ToLowerInvariant()} shortage in {city} from day {startDay} — if they deliver.");
    }

    private static void Trust(GameState state) =>
        state.InformantTrust[state.CityId] = Math.Min(MaxTrust, state.TrustIn(state.CityId) + 1);
}
