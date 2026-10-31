namespace BlackMarketEmpire.Models;

public enum EncounterKind
{
    /// <summary>Street police — easier to run from or bribe.</summary>
    Police,
    /// <summary>Customs/border officers after a long-haul trip — stricter.</summary>
    Customs,
    /// <summary>An undercover officer during a big deal.</summary>
    Undercover,
}

public enum EncounterChoice { Run, Bribe, Fight, Surrender }

/// <summary>A police stop the player must deal with before doing anything else.</summary>
public sealed class Encounter
{
    public EncounterKind Kind { get; set; }
    public string Intro { get; set; } = "";
    public long BribeCost { get; set; }
    /// <summary>How many choices have already been made (a failed run means they chase you).</summary>
    public int Round { get; set; }
    /// <summary>What happened on the last attempt, shown in the dialog.</summary>
    public string? LastOutcome { get; set; }
}

public enum EndReason
{
    /// <summary>Still playing.</summary>
    None,
    /// <summary>Made it to the last day.</summary>
    Finished,
    /// <summary>Ended early by choice.</summary>
    Retired,
    /// <summary>Killed in a police encounter.</summary>
    Killed,
}

/// <summary>The run's rap sheet, shown on the game-over screen.</summary>
public sealed class RunStats
{
    public int Stops { get; set; }
    public int Arrests { get; set; }
    public int Escapes { get; set; }
    public int DaysInJail { get; set; }
    public long BribesPaid { get; set; }
    public long FinesPaid { get; set; }
    public int GoodsSeized { get; set; }
    public long GoodsSeizedValue { get; set; }
    public int DamageTaken { get; set; }
    public long FaresPaid { get; set; }
    public long CourierFeesPaid { get; set; }
    public int ShipmentsLost { get; set; }
    public long Borrowed { get; set; }
    public int EnforcerVisits { get; set; }
    public int StashRaids { get; set; }
    /// <summary>Biggest profit from a single sale.</summary>
    public long BestTrade { get; set; }
    public int Trades { get; set; }
    /// <summary>Profit (or loss) across every sale.</summary>
    public long TotalProfit { get; set; }
    public List<string> CitiesVisited { get; set; } = [];
    public int FixerSuccesses { get; set; }
    /// <summary>First day you owed nothing at all.</summary>
    public int? DebtFreeDay { get; set; }
    public int LowestHealth { get; set; } = 100;
}
