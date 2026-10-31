using System.Text.Json.Serialization;

namespace BlackMarketEmpire.Models;

/// <summary>
/// Everything needed to save and resume a run. Plain mutable class so it serialises cleanly
/// to IndexedDB with System.Text.Json.
/// </summary>
public sealed class GameState
{
    public Guid RunId { get; set; } = Guid.NewGuid();
    public int Day { get; set; } = 1;
    public string CityId { get; set; } = "";
    public long Cash { get; set; }
    public long Debt { get; set; }
    public int Capacity { get; set; }
    public int Health { get; set; } = 100;

    /// <summary>Units held, keyed by good id.</summary>
    public Dictionary<string, int> Inventory { get; set; } = [];

    /// <summary>Average price paid per unit currently held, keyed by good id.</summary>
    public Dictionary<string, long> AverageCost { get; set; } = [];

    /// <summary>Today's prices in the current city. Goods missing from the map aren't on sale today.</summary>
    public Dictionary<string, int> Market { get; set; } = [];

    /// <summary>Real price shocks currently (or recently) affecting markets.</summary>
    public List<MarketEvent> Events { get; set; } = [];

    /// <summary>Intel feed, newest first. Includes false rumours.</summary>
    public List<NewsItem> News { get; set; } = [];

    /// <summary>Most recent messages first.</summary>
    public List<string> Log { get; set; } = [];

    /// <summary>A police stop waiting for the player's choice. Blocks all other actions.</summary>
    public Encounter? PendingEncounter { get; set; }

    public RunStats Stats { get; set; } = new();

    /// <summary>How much attention you've drawn in each city (0–100). Cools every day.</summary>
    public Dictionary<string, int> Heat { get; set; } = [];

    /// <summary>Gear owned, by gear id. Consumables hold a count; everything else is 1.</summary>
    public Dictionary<string, int> Gear { get; set; } = [];

    /// <summary>Goods sent with couriers, newest first. Delivered goods wait here until collected.</summary>
    public List<Shipment> Shipments { get; set; } = [];

    /// <summary>Loans taken during the run (the starting debt is <see cref="Debt"/>).</summary>
    public List<Loan> Loans { get; set; } = [];

    /// <summary>Your standing with each lender, 0–10 (5 = average). Missing = 5.</summary>
    public Dictionary<string, int> Reputation { get; set; } = [];

    /// <summary>Cash in the bank: safe from fines and from dying.</summary>
    public long Bank { get; set; }

    public List<Stash> Stashes { get; set; } = [];

    /// <summary>How you travelled on your most recent trip (affects running away).</summary>
    public TravelMethod? LastTravelMethod { get; set; }

    public bool IsOver { get; set; }
    public EndReason EndReason { get; set; }
    public DateTime StartedUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public int UnitsHeld => Inventory.Values.Sum();
    [JsonIgnore]
    public int FreeSpace => Capacity - SpaceUsed;

    /// <summary>Suitcase space used: bulky goods take more than one slot each.</summary>
    [JsonIgnore]
    public int SpaceUsed => Services.GameCatalog.SpaceFor(Inventory);
    /// <summary>Starting debt plus every loan still owed.</summary>
    [JsonIgnore]
    public long TotalDebt => Debt + Loans.Where(l => l.Status == LoanStatus.Active).Sum(l => l.Owed);

    [JsonIgnore]
    public long NetWorth => Cash + Bank - TotalDebt;

    public int ReputationWith(string lenderId) => Reputation.GetValueOrDefault(lenderId, 5);

    public Stash? StashIn(string cityId) => Stashes.FirstOrDefault(s => s.CityId == cityId);

    public bool Has(string gearId) => Gear.GetValueOrDefault(gearId) > 0;

    public int HeatIn(string cityId) => Heat.GetValueOrDefault(cityId);
}
