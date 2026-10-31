namespace BlackMarketEmpire.Tests.Fakes;

public static class TestGame
{
    /// <summary>Rules where every good is always on sale, so tests aren't at the mercy of availability.</summary>
    /// <summary>
    /// Rules where every good is always on sale and no new events appear after day one,
    /// so tests aren't at the mercy of randomness.
    /// </summary>
    public static GameRules AllAvailable { get; } = new()
    {
        AvailabilityChance = 1.0,
        EventChancePerDay = 0,
        HiddenEventChancePerDay = 0,
        CrackdownChancePerDay = 0,
        MaxStopChance = 0,
        StingChance = 0,
    };

    public static GameEngine Engine(int seed = 42, GameRules? rules = null)
    {
        var random = new SystemRandomSource(new Random(seed));
        return new(new MarketService(random), new EventService(random), new EncounterService(random), new ShippingService(random), new FinanceService(random), rules ?? AllAvailable);
    }

    /// <summary>Engine factory whose engines all use <see cref="AllAvailable"/> rules (or the given ones).</summary>
    public static EngineFactory Factory(Func<Difficulty, GameRules>? rulesFor = null) => new(rulesFor ?? (_ => AllAvailable));

    /// <summary>A state with a fixed, known market in London.</summary>
    public static GameState StateWithMarket(GameEngine engine, Dictionary<string, int> market)
    {
        var state = engine.NewGame();
        state.Market = market;
        return state;
    }
}
