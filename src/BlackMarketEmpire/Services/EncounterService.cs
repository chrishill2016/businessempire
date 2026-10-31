using BlackMarketEmpire.Models;

namespace BlackMarketEmpire.Services;

/// <summary>What happened when the player made a choice in a police encounter.</summary>
public sealed record EncounterOutcome(string Message, int Damage, bool Ended, int JailDays);

/// <summary>Decides when the police stop you, and what happens when you run, bribe, fight or surrender.</summary>
public sealed class EncounterService(IRandomSource random)
{
    /// <summary>Value of what you're carrying, at the price you paid.</summary>
    public static long CargoValue(GameState state) =>
        state.Inventory.Sum(kv => kv.Value * state.AverageCost.GetValueOrDefault(kv.Key));

    public static double StopChance(GameState state, GameRules rules)
    {
        if (state.UnitsHeld == 0) return 0;
        var chance = rules.BaseStopChance
                     + state.UnitsHeld * rules.StopChancePerUnit
                     + CargoValue(state) / 10_000.0 * rules.StopChancePer10k;
        return Math.Min(chance, rules.MaxStopChance);
    }

    public static double RunChance(GameState state, GameRules rules, EncounterKind kind)
    {
        var baseChance = kind == EncounterKind.Police ? rules.PoliceRunChance : rules.CustomsRunChance;
        return Math.Max(0.1, baseChance - state.UnitsHeld * rules.RunPenaltyPerUnit);
    }

    public static double BribeChance(GameRules rules, EncounterKind kind) =>
        kind == EncounterKind.Police ? rules.PoliceBribeChance : rules.CustomsBribeChance;

    public static double FightChance(GameRules rules, EncounterKind kind) =>
        kind == EncounterKind.Police ? rules.PoliceFightChance : rules.CustomsFightChance;

    /// <summary>Called on arrival in a city. Returns true if you've been stopped.</summary>
    public bool MaybeStopOnArrival(GameState state, GameRules rules, int tripDays)
    {
        if (random.NextDouble() >= StopChance(state, rules)) return false;

        var city = GameCatalog.City(state.CityId).Name;
        var kind = tripDays >= rules.CustomsTripDays ? EncounterKind.Customs : EncounterKind.Police;
        var intro = kind == EncounterKind.Customs
            ? $"Customs officers at {city} airport pull you aside and want to search your suitcase."
            : $"Two police officers stop you outside {city} station. \"Mind if we look in the bag?\"";
        Start(state, rules, kind, intro);
        return true;
    }

    /// <summary>Called after a big buy or sell. Returns true if the deal was a sting.</summary>
    public bool MaybeSting(GameState state, GameRules rules, long dealValue)
    {
        if (dealValue < rules.BigDealValue || random.NextDouble() >= rules.StingChance) return false;

        Start(state, rules, EncounterKind.Undercover,
            "As the deal closes, your contact flashes a badge. It was a sting — undercover police!");
        return true;
    }

    public EncounterOutcome Resolve(GameState state, GameRules rules, EncounterChoice choice)
    {
        var enc = state.PendingEncounter ?? throw new InvalidOperationException("No encounter in progress.");
        enc.Round++;

        return choice switch
        {
            EncounterChoice.Run => Run(state, rules, enc),
            EncounterChoice.Bribe => Bribe(state, rules, enc),
            EncounterChoice.Fight => Fight(state, rules, enc),
            _ => Arrest(state, rules, enc, "You put your hands up."),
        };
    }

    private void Start(GameState state, GameRules rules, EncounterKind kind, string intro)
    {
        var bribe = Math.Max(rules.MinBribe, (long)(state.Cash * 0.1 + CargoValue(state) * 0.15));
        state.PendingEncounter = new Encounter
        {
            Kind = kind,
            Intro = intro,
            BribeCost = (long)Math.Ceiling(bribe / 50.0) * 50,
        };
        state.Stats.Stops++;
    }

    private EncounterOutcome Run(GameState state, GameRules rules, Encounter enc)
    {
        if (random.NextDouble() < RunChance(state, rules, enc.Kind))
        {
            state.Stats.Escapes++;
            return new("You bolt through the crowd and lose them. You got away!", 0, Ended: true, JailDays: 0);
        }

        var damage = random.Next(10, 26);
        return new($"They chase you down. You're hit and take {damage} damage — they're still on you!", damage, Ended: false, JailDays: 0);
    }

    private EncounterOutcome Bribe(GameState state, GameRules rules, Encounter enc)
    {
        if (random.NextDouble() < BribeChance(rules, enc.Kind))
        {
            state.Cash -= enc.BribeCost;
            state.Stats.BribesPaid += enc.BribeCost;
            return new($"The officer pockets {Money.Format(enc.BribeCost)} and looks the other way.", 0, Ended: true, JailDays: 0);
        }

        return Arrest(state, rules, enc, "\"Trying to bribe an officer? That's another charge.\"");
    }

    private EncounterOutcome Fight(GameState state, GameRules rules, Encounter enc)
    {
        if (random.NextDouble() < FightChance(rules, enc.Kind))
        {
            var hurt = random.Next(10, 26);
            state.Stats.Escapes++;
            return new($"You fight your way out, taking {hurt} damage. You got away — this time.", hurt, Ended: true, JailDays: 0);
        }

        var damage = random.Next(30, 61);
        return Arrest(state, rules, enc, $"You lose the fight badly and take {damage} damage.") with { Damage = damage };
    }

    private EncounterOutcome Arrest(GameState state, GameRules rules, Encounter enc, string lead)
    {
        var seizedUnits = state.UnitsHeld;
        var seizedValue = CargoValue(state);
        state.Inventory.Clear();
        state.AverageCost.Clear();

        var fine = (long)(state.Cash * rules.ArrestFine);
        state.Cash -= fine;

        var jail = enc.Kind == EncounterKind.Police ? random.Next(1, 4) : random.Next(2, 5);

        state.Stats.Arrests++;
        state.Stats.GoodsSeized += seizedUnits;
        state.Stats.GoodsSeizedValue += seizedValue;
        state.Stats.FinesPaid += fine;

        var seized = seizedUnits > 0 ? $"They seize {seizedUnits} items worth {Money.Format(seizedValue)}" : "They find nothing to seize";
        var fined = fine > 0 ? $", fine you {Money.Format(fine)}" : "";
        return new($"{lead} Arrested! {seized}{fined} and lock you up for {jail} day{(jail == 1 ? "" : "s")}.",
            0, Ended: true, JailDays: jail);
    }
}
