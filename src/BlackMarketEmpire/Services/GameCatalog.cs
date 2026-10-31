using BlackMarketEmpire.Models;

namespace BlackMarketEmpire.Services;

/// <summary>Static game content: the goods, the cities and how long it takes to travel between them.</summary>
public static class GameCatalog
{
    public static IReadOnlyList<Good> Goods { get; } =
    [
        new("games", "Pirated Games", "🎮", 10, 45),
        new("cigs", "Smuggled Cigarettes", "🚬", 15, 60),
        new("fuel", "Untaxed Fuel", "⛽", 30, 120, Size: 3,
            CityIds: ["lagos", "moscow", "dubai", "bogota", "istanbul", "naples"]),
        new("whisky", "Bootleg Whisky", "🥃", 40, 150),
        new("meds", "Fake Medicines", "💊", 60, 250),
        new("trainers", "Knock-off Trainers", "👟", 80, 300),
        new("cards", "Cloned Bank Cards", "💳", 150, 600,
            CityIds: ["moscow", "bangkok", "lagos", "newyork", "london", "miami"]),
        new("bags", "Fake Designer Bags", "👜", 200, 700),
        new("carparts", "Stolen Car Parts", "🔧", 300, 1_200, Size: 4,
            CityIds: ["naples", "moscow", "miami", "lagos", "london", "bogota", "dubai"]),
        new("phones", "Stolen Phones", "📱", 400, 1_500),
        new("cash", "Counterfeit Cash", "💵", 500, 2_000,
            CityIds: ["bogota", "miami", "newyork", "naples", "lagos", "bangkok", "london"]),
        new("watches", "Counterfeit Watches", "⌚", 1_000, 3_500),
        new("art", "Stolen Art", "🖼️", 4_000, 15_000, Size: 3),
        new("diamonds", "Smuggled Diamonds", "💎", 8_000, 30_000),
    ];

    public static IReadOnlyList<City> Cities { get; } =
    [
        // Europe
        new("london", "London", "UK", new Dictionary<string, double>
            { ["cigs"] = 1.2, ["phones"] = 1.15, ["watches"] = 1.1, ["art"] = 1.25, ["cards"] = 1.2 },
            DangerLevel.Medium, "Europe", 51.5, -0.1),
        new("amsterdam", "Amsterdam", "Netherlands", new Dictionary<string, double>
            { ["diamonds"] = 0.85, ["trainers"] = 0.9, ["cigs"] = 1.1, ["games"] = 1.2 },
            DangerLevel.Low, "Europe", 52.4, 4.9),
        new("marseille", "Marseille", "France", new Dictionary<string, double>
            { ["cigs"] = 0.8, ["whisky"] = 0.85, ["art"] = 0.9, ["meds"] = 1.2 },
            DangerLevel.High, "Europe", 43.3, 5.4),
        new("naples", "Naples", "Italy", new Dictionary<string, double>
            { ["bags"] = 0.7, ["carparts"] = 0.75, ["cash"] = 0.8, ["art"] = 1.1 },
            DangerLevel.Medium, "Europe", 40.85, 14.3),
        new("moscow", "Moscow", "Russia", new Dictionary<string, double>
            { ["fuel"] = 0.6, ["cards"] = 0.7, ["whisky"] = 1.4, ["watches"] = 1.2, ["diamonds"] = 1.15 },
            DangerLevel.High, "Europe", 55.8, 37.6),
        new("istanbul", "Istanbul", "Türkiye", new Dictionary<string, double>
            { ["bags"] = 0.75, ["trainers"] = 0.75, ["watches"] = 0.85, ["fuel"] = 1.2 },
            DangerLevel.Medium, "Europe", 41.0, 29.0),
        // Middle East & Africa
        new("dubai", "Dubai", "UAE", new Dictionary<string, double>
            { ["diamonds"] = 1.2, ["watches"] = 1.25, ["phones"] = 0.8, ["whisky"] = 1.3, ["fuel"] = 0.7, ["carparts"] = 1.3 },
            DangerLevel.High, "Middle East & Africa", 25.2, 55.3),
        new("lagos", "Lagos", "Nigeria", new Dictionary<string, double>
            { ["fuel"] = 1.4, ["meds"] = 1.4, ["phones"] = 1.25, ["cards"] = 0.75, ["diamonds"] = 0.8 },
            DangerLevel.High, "Middle East & Africa", 6.5, 3.4),
        // Asia-Pacific
        new("hongkong", "Hong Kong", "China", new Dictionary<string, double>
            { ["phones"] = 0.7, ["watches"] = 0.75, ["art"] = 1.15, ["games"] = 0.8 },
            DangerLevel.Low, "Asia-Pacific", 22.3, 114.2),
        new("bangkok", "Bangkok", "Thailand", new Dictionary<string, double>
            { ["games"] = 0.6, ["meds"] = 0.65, ["bags"] = 0.8, ["cards"] = 0.85, ["cash"] = 1.25 },
            DangerLevel.Medium, "Asia-Pacific", 13.8, 100.5),
        new("sydney", "Sydney", "Australia", new Dictionary<string, double>
            { ["cigs"] = 1.5, ["games"] = 1.3, ["meds"] = 1.3, ["phones"] = 1.2, ["trainers"] = 1.2 },
            DangerLevel.Low, "Asia-Pacific", -33.9, 151.2),
        // Americas
        new("newyork", "New York", "USA", new Dictionary<string, double>
            { ["art"] = 1.3, ["diamonds"] = 1.15, ["cash"] = 1.2, ["cards"] = 1.25, ["games"] = 0.9 },
            DangerLevel.Medium, "Americas", 40.7, -74.0),
        new("miami", "Miami", "USA", new Dictionary<string, double>
            { ["cash"] = 1.1, ["watches"] = 1.15, ["carparts"] = 1.25, ["cigs"] = 1.1 },
            DangerLevel.High, "Americas", 25.8, -80.2),
        new("bogota", "Bogotá", "Colombia", new Dictionary<string, double>
            { ["cash"] = 0.6, ["meds"] = 0.8, ["carparts"] = 0.7, ["whisky"] = 1.2, ["phones"] = 1.2 },
            DangerLevel.High, "Americas", 4.7, -74.1),
    ];

    /// <summary>Travel time in days between each pair of cities, worked out from the distance between them.</summary>
    private static readonly Dictionary<(string, string), int> TravelDays = BuildTravelDays();

    /// <summary>Cities with a cargo port.</summary>
    public static IReadOnlySet<string> Ports { get; } = new HashSet<string>
    {
        "amsterdam", "marseille", "naples", "istanbul", "dubai", "lagos", "hongkong", "bangkok", "sydney", "newyork", "miami",
    };

    /// <summary>Groups of cities linked by road and rail. Everything else needs a flight or a boat.</summary>
    private static readonly string[][] OverlandGroups =
    [
        ["london", "amsterdam", "marseille", "naples", "moscow", "istanbul"],
        ["istanbul", "dubai"],
        ["hongkong", "bangkok"],
        ["newyork", "miami"],
    ];

    private static readonly HashSet<(string, string)> OverlandRoutes = BuildPairs(
        OverlandGroups.SelectMany(g => g.SelectMany(a => g.Where(b => b != a).Select(b => (a, b)))));

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
        new("eddie", "Fast Eddie", LenderKind.LoanShark, ["london", "istanbul", "naples"],
            BaseRate: 0.08, BaseLimit: 5_000, TermDays: 7, GraceDays: 1, MinEnforcerDamage: 10, MaxEnforcerDamage: 20, TakesGoods: false,
            "Small loans, steep rate. Lets it slide if you're a day late."),
        new("accountant", "The Accountant", LenderKind.LoanShark, ["amsterdam", "dubai", "newyork"],
            BaseRate: 0.04, BaseLimit: 15_000, TermDays: 5, GraceDays: 0, MinEnforcerDamage: 5, MaxEnforcerDamage: 15, TakesGoods: true,
            "Bigger loans at a fair rate. Strict deadlines, no extensions. Takes goods if you're late."),
        new("rossi", "Mama Rossi", LenderKind.LoanShark, ["marseille", "hongkong", "sydney"],
            BaseRate: 0.06, BaseLimit: 10_000, TermDays: 6, GraceDays: 0, MinEnforcerDamage: 20, MaxEnforcerDamage: 35, TakesGoods: false,
            "Mid-size loans. Her boys come round the day you're overdue."),
        new("bank", "First Meridian Bank", LenderKind.Bank, ["london", "amsterdam", "dubai", "hongkong", "newyork", "sydney"],
            BaseRate: 0.01, BaseLimit: 0, TermDays: 10, GraceDays: 2, MinEnforcerDamage: 0, MaxEnforcerDamage: 0, TakesGoods: false,
            "Cheap money, if you look respectable: a decent net worth and no arrests. Lends up to half your net worth."),
        new("carlos", "Don Carlos", LenderKind.LoanShark, ["miami", "bogota", "lagos"],
            BaseRate: 0.07, BaseLimit: 20_000, TermDays: 6, GraceDays: 0, MinEnforcerDamage: 25, MaxEnforcerDamage: 40, TakesGoods: true,
            "Generous limits for a loan shark. Charming until you're late."),
        new("syndicate", "The Syndicate", LenderKind.Underground, ["istanbul", "hongkong", "moscow", "bangkok"],
            BaseRate: 0.10, BaseLimit: 50_000, TermDays: 5, GraceDays: 0, MinEnforcerDamage: 30, MaxEnforcerDamage: 50, TakesGoods: true,
            "Huge sums, no questions. Miss a payment and they take everything — maybe you too."),
    ];

    /// <summary>Cities with a branch of the bank, where you can deposit and withdraw.</summary>
    public static IReadOnlySet<string> BankCities { get; } = new HashSet<string> { "london", "amsterdam", "dubai", "hongkong", "newyork", "sydney" };

    public static Lender Lender(string id) => Lenders.First(l => l.Id == id);

    public static Good Good(string id) => Goods.First(g => g.Id == id);

    /// <summary>Suitcase space taken by a set of goods.</summary>
    public static int SpaceFor(IReadOnlyDictionary<string, int> goods) => goods.Sum(kv => kv.Value * Good(kv.Key).Size);

    public static GearItem GearItem(string id) => Gear.First(g => g.Id == id);

    public static Courier Courier(string id) => Couriers.First(c => c.Id == id);

    public static bool HasOverlandRoute(string a, string b) => OverlandRoutes.Contains((a, b));

    public static bool IsPort(string cityId) => Ports.Contains(cityId);

    public static City City(string id) => Cities.First(c => c.Id == id);

    public static int DaysBetween(string fromCityId, string toCityId) =>
        fromCityId == toCityId ? 0 : TravelDays[(fromCityId, toCityId)];

    public static IReadOnlyList<string> Regions { get; } = ["Europe", "Middle East & Africa", "Asia-Pacific", "Americas"];

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

    /// <summary>Great-circle distance in km.</summary>
    public static double DistanceKm(City a, City b)
    {
        static double Rad(double deg) => deg * Math.PI / 180;
        var dLat = Rad(b.Latitude - a.Latitude);
        var dLon = Rad(b.Longitude - a.Longitude);
        var h = Math.Pow(Math.Sin(dLat / 2), 2) +
                Math.Cos(Rad(a.Latitude)) * Math.Cos(Rad(b.Latitude)) * Math.Pow(Math.Sin(dLon / 2), 2);
        return 2 * 6371 * Math.Asin(Math.Sqrt(h));
    }

    /// <summary>Under 1,500 km is a day, under 4,000 two, under 10,000 three, anything further four.</summary>
    public static int DaysForDistance(double km) => km switch
    {
        < 1_500 => 1,
        < 4_000 => 2,
        < 10_000 => 3,
        _ => 4,
    };

    private static Dictionary<(string, string), int> BuildTravelDays()
    {
        var map = new Dictionary<(string, string), int>();
        foreach (var a in Cities)
        foreach (var b in Cities.Where(c => c.Id != a.Id))
        {
            map[(a.Id, b.Id)] = DaysForDistance(DistanceKm(a, b));
        }
        return map;
    }
}
