namespace BlackMarketEmpire.Tests.Fakes;

public static class TestGame
{
    /// <summary>Rules where every good is always on sale, so tests aren't at the mercy of availability.</summary>
    public static GameRules AllAvailable { get; } = new() { AvailabilityChance = 1.0 };

    public static GameEngine Engine(int seed = 42, GameRules? rules = null) =>
        new(new MarketService(new SystemRandomSource(new Random(seed))), rules ?? AllAvailable);

    /// <summary>A state with a fixed, known market in London.</summary>
    public static GameState StateWithMarket(GameEngine engine, Dictionary<string, int> market)
    {
        var state = engine.NewGame();
        state.Market = market;
        return state;
    }
}
