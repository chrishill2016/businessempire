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

    // ----- Police & danger (Phase 3) -----
    public int MaxHealth { get; init; } = 100;
    /// <summary>Health recovered for each day that passes.</summary>
    public int HealthPerDay { get; init; } = 3;
    /// <summary>Chance of being stopped on arrival when carrying goods, before cargo adjustments.</summary>
    public double BaseStopChance { get; init; } = 0.12;
    /// <summary>Extra stop chance per item carried.</summary>
    public double StopChancePerUnit { get; init; } = 0.0015;
    /// <summary>Extra stop chance per £10,000 of cargo (valued at what you paid).</summary>
    public double StopChancePer10k { get; init; } = 0.02;
    public double MaxStopChance { get; init; } = 0.45;
    /// <summary>Trips this long or longer are flights, so you meet customs instead of street police.</summary>
    public int CustomsTripDays { get; init; } = 2;
    /// <summary>Chance a single buy/sell worth at least <see cref="BigDealValue"/> is an undercover sting.</summary>
    public double StingChance { get; init; } = 0.06;
    public long BigDealValue { get; init; } = 10_000;
    /// <summary>Chance a run succeeds, before the weight of your cargo slows you down.</summary>
    public double PoliceRunChance { get; init; } = 0.55;
    public double CustomsRunChance { get; init; } = 0.35;
    public double RunPenaltyPerUnit { get; init; } = 0.002;
    public double PoliceBribeChance { get; init; } = 0.75;
    public double CustomsBribeChance { get; init; } = 0.5;
    public double PoliceFightChance { get; init; } = 0.3;
    public double CustomsFightChance { get; init; } = 0.2;
    public long MinBribe { get; init; } = 300;
    /// <summary>Fine on arrest, as a share of your cash.</summary>
    public double ArrestFine { get; init; } = 0.2;

    public static GameRules Default { get; } = new();
}
