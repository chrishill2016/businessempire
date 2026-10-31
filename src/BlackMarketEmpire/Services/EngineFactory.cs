using BlackMarketEmpire.Models;

namespace BlackMarketEmpire.Services;

/// <summary>A game engine plus the random-number generator all its services share.</summary>
public sealed record EngineInstance(GameEngine Engine, RunRandom Random);

/// <summary>Builds an engine for a difficulty, with every service drawing from one saveable <see cref="RunRandom"/>.</summary>
public sealed class EngineFactory(Func<Difficulty, GameRules>? rulesFor = null)
{
    private readonly Func<Difficulty, GameRules> _rulesFor = rulesFor ?? GameRules.For;
    private readonly Dictionary<Difficulty, EngineInstance> _cache = [];

    public EngineInstance For(Difficulty difficulty)
    {
        if (_cache.TryGetValue(difficulty, out var cached)) return cached;

        var random = new RunRandom();
        var events = new EventService(random);
        var engine = new GameEngine(
            new MarketService(random), events, new EncounterService(random), new ShippingService(random),
            new FinanceService(random), _rulesFor(difficulty), new IntelService(random, events));
        return _cache[difficulty] = new EngineInstance(engine, random);
    }
}
