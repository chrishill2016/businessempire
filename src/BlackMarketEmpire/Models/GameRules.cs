namespace BlackMarketEmpire.Models;

/// <summary>Tunable numbers for a run. Kept in one place so balancing is easy.</summary>
public sealed record GameRules
{
    public int TotalDays { get; init; } = 30;
    public long StartingCash { get; init; } = 2_500;
    public long StartingDebt { get; init; } = 5_000;
    /// <summary>Daily compound interest on the debt (0.05 = 5% per day).</summary>
    public double DailyInterest { get; init; } = 0.05;
    public int Capacity { get; init; } = 100;
    /// <summary>Chance each good is on sale in a city on a given day.</summary>
    public double AvailabilityChance { get; init; } = 0.8;
    public int MaxLogEntries { get; init; } = 30;

    // ----- Market events (Phase 2) -----
    /// <summary>Chance each new day that a new headline (real event or false rumour) appears.</summary>
    public double EventChancePerDay { get; init; } = 0.5;
    /// <summary>Chance a headline is a false rumour rather than a real event.</summary>
    public double FalseRumourChance { get; init; } = 0.25;
    public int MaxActiveEvents { get; init; } = 3;
    public int MinEventDays { get; init; } = 2;
    public int MaxEventDays { get; init; } = 5;
    public double SpikeMinMultiplier { get; init; } = 2.0;
    public double SpikeMaxMultiplier { get; init; } = 4.0;
    public double CrashMinMultiplier { get; init; } = 0.25;
    public double CrashMaxMultiplier { get; init; } = 0.5;
    public int MaxNewsItems { get; init; } = 20;

    public static GameRules Default { get; } = new();
}
