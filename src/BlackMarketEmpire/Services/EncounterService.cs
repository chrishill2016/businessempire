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

    /// <summary>
    /// Chance of being stopped on arriving in <paramref name="cityId"/> by <paramref name="method"/>,
    /// given what you're carrying, the heat there and your gear.
    /// </summary>
    public static double StopChance(GameState state, GameRules rules, TravelMethod method, string cityId)
    {
        if (state.UnitsHeld == 0) return 0;
        var chance = rules.BaseStopChance
                     + state.UnitsHeld * rules.StopChancePerUnit
                     + CargoValue(state) / 10_000.0 * rules.StopChancePer10k
                     + state.HeatIn(cityId) / 100.0 * rules.StopChanceAtMaxHeat;
        chance *= TravelService.StopFactor(method, rules);
        chance *= GameCatalog.Cities.FirstOrDefault(c => c.Id == cityId)?.PoliceFactor ?? 1.0;
        if (state.Has("lining")) chance *= 0.7;
        if (state.Has("docs") && TravelService.IsCustoms(method)) chance *= 0.5;
        return Math.Min(chance, rules.MaxStopChance);
    }

    public static double RunChance(GameState state, GameRules rules, EncounterKind kind)
    {
        var baseChance = kind == EncounterKind.Police ? rules.PoliceRunChance : rules.CustomsRunChance;
        if (kind == EncounterKind.Police && state.LastTravelMethod == TravelMethod.Car) baseChance += rules.CarRunBonus;
        if (state.Has("shoes")) baseChance += 0.1;
        return Math.Clamp(baseChance - state.UnitsHeld * rules.RunPenaltyPerUnit, 0.1, 0.95);
    }

    public static double StingChance(GameState state, GameRules rules) =>
        rules.StingChance * (state.Has("burners") ? 0.3 : 1.0);

    /// <summary>Applies body armour to raw damage.</summary>
    public static int Hurt(GameState state, int rawDamage) =>
        state.Has("armour") ? (int)Math.Round(rawDamage * 0.6) : rawDamage;

    public static void AddHeat(GameState state, string cityId, int amount) =>
        state.Heat[cityId] = Math.Clamp(state.HeatIn(cityId) + amount, 0, 100);

    public static double BribeChance(GameRules rules, EncounterKind kind) =>
        kind == EncounterKind.Police ? rules.PoliceBribeChance : rules.CustomsBribeChance;

    public static double FightChance(GameRules rules, EncounterKind kind) =>
        kind == EncounterKind.Police ? rules.PoliceFightChance : rules.CustomsFightChance;

    /// <summary>Called on arrival in a city. Returns true if you've been stopped.</summary>
    public bool MaybeStopOnArrival(GameState state, GameRules rules, TravelMethod method)
    {
        if (random.NextDouble() >= StopChance(state, rules, method, state.CityId)) return false;

        var city = GameCatalog.City(state.CityId).Name;
        var kind = TravelService.IsCustoms(method) ? EncounterKind.Customs : EncounterKind.Police;
        var intro = method switch
        {
            TravelMethod.Flight => $"Customs officers at {city} airport pull you aside and want to search your suitcase.",
            TravelMethod.Boat => $"Port customs in {city} board the boat with sniffer dogs. They head straight for your cabin.",
            TravelMethod.Car => $"Police roadblock on the way into {city}. \"Pop the boot, please.\"",
            _ => $"Two police officers stop you outside {city} station. \"Mind if we look in the bag?\"",
        };
        Start(state, rules, kind, intro);
        return true;
    }

    /// <summary>Called after a big buy or sell. Returns true if the deal was a sting.</summary>
    public bool MaybeSting(GameState state, GameRules rules, long dealValue)
    {
        if (dealValue < rules.BigDealValue || random.NextDouble() >= StingChance(state, rules)) return false;

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
        AddHeat(state, state.CityId, rules.HeatOnStop);
    }

    private EncounterOutcome Run(GameState state, GameRules rules, Encounter enc)
    {
        if (random.NextDouble() < RunChance(state, rules, enc.Kind))
        {
            state.Stats.Escapes++;
            AddHeat(state, state.CityId, rules.HeatOnEscape);
            return new("You bolt and lose them. You got away! The police here will remember you.", 0, Ended: true, JailDays: 0);
        }

        var damage = Hurt(state, random.Next(10, 26));
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
            var hurt = Hurt(state, random.Next(10, 26));
            state.Stats.Escapes++;
            AddHeat(state, state.CityId, rules.HeatOnEscape);
            return new($"You fight your way out, taking {hurt} damage. You got away — this time.", hurt, Ended: true, JailDays: 0);
        }

        var damage = Hurt(state, random.Next(30, 61));
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
        AddHeat(state, state.CityId, rules.HeatOnArrest);

        var lostGear = GameCatalog.Gear.Where(g => g.SeizedOnArrest && state.Has(g.Id)).ToList();
        foreach (var g in lostGear) state.Gear.Remove(g.Id);
        var gearText = lostGear.Count > 0 ? $" They also take your {string.Join(" and ", lostGear.Select(g => g.Name.ToLowerInvariant()))}." : "";

        var seized = seizedUnits > 0 ? $"They seize {seizedUnits} items worth {Money.Format(seizedValue)}" : "They find nothing to seize";
        var fined = fine > 0 ? $", fine you {Money.Format(fine)}" : "";
        return new($"{lead} Arrested! {seized}{fined} and lock you up for {jail} day{(jail == 1 ? "" : "s")}.{gearText}",
            0, Ended: true, JailDays: jail);
    }
}
