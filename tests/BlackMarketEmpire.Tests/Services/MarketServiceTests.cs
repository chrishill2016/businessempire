namespace BlackMarketEmpire.Tests.Services;

public class MarketServiceTests
{
    [Fact]
    public void Prices_stay_within_range_after_city_bias()
    {
        var market = new MarketService(new SystemRandomSource(new Random(1)));
        var rules = GameRules.Default;

        foreach (var city in GameCatalog.Cities)
        {
            for (var day = 0; day < 50; day++)
            {
                foreach (var (goodId, price) in market.GenerateMarket(city.Id, rules))
                {
                    var good = GameCatalog.Good(goodId);
                    var bias = city.BiasFor(goodId);
                    Assert.InRange(price, (int)Math.Floor(good.MinPrice * bias), (int)Math.Ceiling(good.MaxPrice * bias));
                }
            }
        }
    }

    [Fact]
    public void At_least_one_good_is_always_on_sale()
    {
        var market = new MarketService(new SystemRandomSource(new Random(3)));
        var rules = new GameRules { AvailabilityChance = 0 };

        Assert.Single(market.GenerateMarket("london", rules));
    }

    [Fact]
    public void Every_city_pair_has_a_travel_time_between_one_and_three_days()
    {
        foreach (var a in GameCatalog.Cities)
        foreach (var b in GameCatalog.Cities.Where(c => c.Id != a.Id))
        {
            Assert.InRange(GameCatalog.DaysBetween(a.Id, b.Id), 1, 3);
            Assert.Equal(GameCatalog.DaysBetween(a.Id, b.Id), GameCatalog.DaysBetween(b.Id, a.Id));
        }
    }
}

public class MoneyTests
{
    [Theory]
    [InlineData(1234, "£1,234")]
    [InlineData(-5210, "-£5,210")]
    [InlineData(0, "£0")]
    public void Formats_with_sign_before_pound(long amount, string expected) =>
        Assert.Equal(expected, Money.Format(amount));
}
