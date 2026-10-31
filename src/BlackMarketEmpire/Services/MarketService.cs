using BlackMarketEmpire.Models;

namespace BlackMarketEmpire.Services;

/// <summary>Generates the day's prices for a city.</summary>
public sealed class MarketService(IRandomSource random)
{
    public Dictionary<string, int> GenerateMarket(string cityId, GameRules rules)
    {
        var city = GameCatalog.City(cityId);
        var market = new Dictionary<string, int>();

        foreach (var good in GameCatalog.Goods)
        {
            if (random.NextDouble() >= rules.AvailabilityChance)
            {
                continue;
            }

            market[good.Id] = PriceFor(good, city);
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
