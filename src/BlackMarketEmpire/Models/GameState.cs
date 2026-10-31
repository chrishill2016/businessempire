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

    public bool IsOver { get; set; }
    public DateTime StartedUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public int UnitsHeld => Inventory.Values.Sum();
    [JsonIgnore]
    public int FreeSpace => Capacity - UnitsHeld;
    [JsonIgnore]
    public long NetWorth => Cash - Debt;
}
