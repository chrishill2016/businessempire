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

    public static GameRules Default { get; } = new();
}
