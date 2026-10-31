using BlackMarketEmpire.Models;

namespace BlackMarketEmpire.Services;

/// <summary>Generates the day's prices for a city.</summary>
public sealed class MarketService(IRandomSource random)
{
    /// <summary>
    /// Today's prices in a city. Active <paramref name="events"/> for this city guarantee the good is
    /// traded and multiply its price.
    /// </summary>
    public Dictionary<string, int> GenerateMarket(string cityId, GameRules rules, int day = 0, IEnumerable<MarketEvent>? events = null)
    {
        var city = GameCatalog.City(cityId);
        var market = new Dictionary<string, int>();
        var shocks = (events ?? [])
            .Where(e => e.CityId == cityId && e.IsActiveOn(day))
            .ToDictionary(e => e.GoodId, e => e.Multiplier);

        foreach (var good in GameCatalog.Goods)
        {
            var available = random.NextDouble() < rules.AvailabilityChance;
            if (shocks.TryGetValue(good.Id, out var multiplier))
            {
                market[good.Id] = Math.Max(1, (int)Math.Round(PriceFor(good, city) * multiplier));
            }
            else if (available)
            {
                market[good.Id] = PriceFor(good, city);
            }
        }

        // Always have at least one thing on sale so a day is never a dead end.
        if (market.Count == 0)
        {
            var good = GameCatalog.Goods[random.Next(0, GameCatalog.Goods.Count)];
            market[good.Id] = PriceFor(good, city);
        }

        return market;
    }

    private int PriceFor(Good good, City city)
    {
        var basePrice = good.MinPrice + random.NextDouble() * (good.MaxPrice - good.MinPrice);
        return Math.Max(1, (int)Math.Round(basePrice * city.BiasFor(good.Id)));
    }
}
