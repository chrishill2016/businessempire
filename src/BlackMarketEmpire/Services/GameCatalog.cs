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
            { ["cigs"] = 1.2, ["phones"] = 1.15, ["watches"] = 1.1, ["art"] = 1.25 }, DangerLevel.Medium),
        new("amsterdam", "Amsterdam", "Netherlands", new Dictionary<string, double>
            { ["diamonds"] = 0.85, ["trainers"] = 0.9, ["cigs"] = 1.1 }, DangerLevel.Low),
        new("marseille", "Marseille", "France", new Dictionary<string, double>
            { ["cigs"] = 0.8, ["whisky"] = 0.85, ["art"] = 0.9 }, DangerLevel.High),
        new("istanbul", "Istanbul", "Türkiye", new Dictionary<string, double>
            { ["bags"] = 0.75, ["trainers"] = 0.75, ["watches"] = 0.85 }, DangerLevel.Medium),
        new("dubai", "Dubai", "UAE", new Dictionary<string, double>
            { ["diamonds"] = 1.2, ["watches"] = 1.25, ["phones"] = 0.8, ["whisky"] = 1.3 }, DangerLevel.High),
        new("hongkong", "Hong Kong", "China", new Dictionary<string, double>
            { ["phones"] = 0.7, ["watches"] = 0.75, ["art"] = 1.15 }, DangerLevel.Low),
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

    /// <summary>Cities with a cargo port.</summary>
    public static IReadOnlySet<string> Ports { get; } = new HashSet<string> { "amsterdam", "marseille", "istanbul", "dubai", "hongkong" };

    /// <summary>Routes you can do by train or car. Everything else needs a flight or a boat.</summary>
    private static readonly HashSet<(string, string)> OverlandRoutes = BuildPairs(
    [
        ("london", "amsterdam"), ("london", "marseille"), ("london", "istanbul"),
        ("amsterdam", "marseille"), ("amsterdam", "istanbul"),
        ("marseille", "istanbul"),
        ("istanbul", "dubai"),
    ]);

    public static IReadOnlyList<GearItem> Gear { get; } =
    [
        new("holdall", "Big holdall", "🎒", 1_500, "+50 suitcase space."),
        new("trolley", "Wheeled trunk", "🧳", 4_000, "+100 suitcase space."),
        new("lining", "Hidden-lining case", "🕳️", 2_500, "Stopped 30% less often. Seized if you're arrested.", SeizedOnArrest: true),
        new("docs", "Fake documents", "🪪", 3_000, "Halves the chance of being stopped when flying or by boat. Seized if you're arrested.", SeizedOnArrest: true),
        new("scanner", "Police scanner app", "📡", 800, "Shows the exact chance of being stopped on every route, and the heat in every city."),
        new("burners", "Burner phones", "📵", 600, "Big deals are far less likely to be stings."),
        new("shoes", "Running shoes", "👟", 300, "+10% chance when you run."),
        new("armour", "Body armour", "🦺", 2_000, "Take 40% less damage."),
        new("firstaid", "First-aid kit", "🩹", 400, "Use any time to heal 40 health.", Consumable: true),
    ];

    public static IReadOnlyList<Courier> Couriers { get; } =
    [
        new("budget", "Budget courier", FeeRate: 0.10, ExtraDays: 1, SeizureChance: 0.20, TheftChance: 0.05,
            "Cheap and slow. Customs catch a lot of their parcels."),
        new("pro", "Professional smuggler", FeeRate: 0.25, ExtraDays: 0, SeizureChance: 0.07, TheftChance: 0.01,
            "Expensive, but fast and careful."),
    ];

    public static IReadOnlyList<Lender> Lenders { get; } =
    [
        new("eddie", "Fast Eddie", LenderKind.LoanShark, ["london", "istanbul"],
            BaseRate: 0.08, BaseLimit: 5_000, TermDays: 7, GraceDays: 1, MinEnforcerDamage: 10, MaxEnforcerDamage: 20, TakesGoods: false,
            "Small loans, steep rate. Lets it slide if you're a day late."),
        new("accountant", "The Accountant", LenderKind.LoanShark, ["amsterdam", "dubai"],
            BaseRate: 0.04, BaseLimit: 15_000, TermDays: 5, GraceDays: 0, MinEnforcerDamage: 5, MaxEnforcerDamage: 15, TakesGoods: true,
            "Bigger loans at a fair rate. Strict deadlines, no extensions. Takes goods if you're late."),
        new("rossi", "Mama Rossi", LenderKind.LoanShark, ["marseille", "hongkong"],
            BaseRate: 0.06, BaseLimit: 10_000, TermDays: 6, GraceDays: 0, MinEnforcerDamage: 20, MaxEnforcerDamage: 35, TakesGoods: false,
            "Mid-size loans. Her boys come round the day you're overdue."),
        new("bank", "First Meridian Bank", LenderKind.Bank, ["london", "amsterdam", "dubai", "hongkong"],
            BaseRate: 0.01, BaseLimit: 0, TermDays: 10, GraceDays: 2, MinEnforcerDamage: 0, MaxEnforcerDamage: 0, TakesGoods: false,
            "Cheap money, if you look respectable: a decent net worth and no arrests. Lends up to half your net worth."),
        new("syndicate", "The Syndicate", LenderKind.Underground, ["istanbul", "hongkong"],
            BaseRate: 0.10, BaseLimit: 50_000, TermDays: 5, GraceDays: 0, MinEnforcerDamage: 30, MaxEnforcerDamage: 50, TakesGoods: true,
            "Huge sums, no questions. Miss a payment and they take everything — maybe you too."),
    ];

    /// <summary>Cities with a branch of the bank, where you can deposit and withdraw.</summary>
    public static IReadOnlySet<string> BankCities { get; } = new HashSet<string> { "london", "amsterdam", "dubai", "hongkong" };

    public static Lender Lender(string id) => Lenders.First(l => l.Id == id);

    public static Good Good(string id) => Goods.First(g => g.Id == id);

    public static GearItem GearItem(string id) => Gear.First(g => g.Id == id);

    public static Courier Courier(string id) => Couriers.First(c => c.Id == id);

    public static bool HasOverlandRoute(string a, string b) => OverlandRoutes.Contains((a, b));

    public static bool IsPort(string cityId) => Ports.Contains(cityId);

    public static City City(string id) => Cities.First(c => c.Id == id);

    public static int DaysBetween(string fromCityId, string toCityId) =>
        fromCityId == toCityId ? 0 : TravelDays[(fromCityId, toCityId)];

    private static HashSet<(string, string)> BuildPairs(IEnumerable<(string A, string B)> pairs)
    {
        var set = new HashSet<(string, string)>();
        foreach (var (a, b) in pairs)
        {
            set.Add((a, b));
            set.Add((b, a));
        }
        return set;
    }

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
