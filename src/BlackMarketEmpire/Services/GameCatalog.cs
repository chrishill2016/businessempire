using BlackMarketEmpire.Models;

namespace BlackMarketEmpire.Services;

/// <summary>Static game content: the goods, the cities and how long it takes to travel between them.</summary>
public static class GameCatalog
{
    public static IReadOnlyList<Good> Goods { get; } =
    [
        new("cigs", "Smuggled Cigarettes", "🚬", 15, 60),
        new("whisky", "Bootleg Whisky", "🥃", 40, 150),
        new("trainers", "Knock-off Trainers", "👟", 80, 300),
        new("bags", "Fake Designer Bags", "👜", 200, 700),
        new("phones", "Stolen Phones", "📱", 400, 1_500),
        new("watches", "Counterfeit Watches", "⌚", 1_000, 3_500),
        new("art", "Stolen Art", "🖼️", 4_000, 15_000),
        new("diamonds", "Smuggled Diamonds", "💎", 8_000, 30_000),
    ];

    public static IReadOnlyList<City> Cities { get; } =
    [
        new("london", "London", "UK", new Dictionary<string, double>
            { ["cigs"] = 1.2, ["phones"] = 1.15, ["watches"] = 1.1, ["art"] = 1.25 }),
        new("amsterdam", "Amsterdam", "Netherlands", new Dictionary<string, double>
            { ["diamonds"] = 0.85, ["trainers"] = 0.9, ["cigs"] = 1.1 }),
        new("marseille", "Marseille", "France", new Dictionary<string, double>
            { ["cigs"] = 0.8, ["whisky"] = 0.85, ["art"] = 0.9 }),
        new("istanbul", "Istanbul", "Türkiye", new Dictionary<string, double>
            { ["bags"] = 0.75, ["trainers"] = 0.75, ["watches"] = 0.85 }),
        new("dubai", "Dubai", "UAE", new Dictionary<string, double>
            { ["diamonds"] = 1.2, ["watches"] = 1.25, ["phones"] = 0.8, ["whisky"] = 1.3 }),
        new("hongkong", "Hong Kong", "China", new Dictionary<string, double>
            { ["phones"] = 0.7, ["watches"] = 0.75, ["art"] = 1.15 }),
    ];

    /// <summary>Travel time in days between each pair of cities (symmetric).</summary>
    private static readonly Dictionary<(string, string), int> TravelDays = BuildTravelDays(
    [
        ("london", "amsterdam", 1), ("london", "marseille", 1), ("london", "istanbul", 2),
        ("london", "dubai", 3), ("london", "hongkong", 3),
        ("amsterdam", "marseille", 1), ("amsterdam", "istanbul", 2), ("amsterdam", "dubai", 3),
        ("amsterdam", "hongkong", 3),
        ("marseille", "istanbul", 2), ("marseille", "dubai", 2), ("marseille", "hongkong", 3),
        ("istanbul", "dubai", 1), ("istanbul", "hongkong", 2),
        ("dubai", "hongkong", 2),
    ]);

    public static Good Good(string id) => Goods.First(g => g.Id == id);

    public static City City(string id) => Cities.First(c => c.Id == id);

    public static int DaysBetween(string fromCityId, string toCityId) =>
        fromCityId == toCityId ? 0 : TravelDays[(fromCityId, toCityId)];

    private static Dictionary<(string, string), int> BuildTravelDays(IEnumerable<(string A, string B, int Days)> routes)
    {
        var map = new Dictionary<(string, string), int>();
        foreach (var (a, b, days) in routes)
        {
            map[(a, b)] = days;
            map[(b, a)] = days;
        }
        return map;
    }
}
