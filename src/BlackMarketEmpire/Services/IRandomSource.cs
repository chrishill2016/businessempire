namespace BlackMarketEmpire.Services;

/// <summary>Abstraction over randomness so game logic can be tested with a fixed seed.</summary>
public interface IRandomSource
{
    /// <summary>Returns a value in [0, 1).</summary>
    double NextDouble();

    /// <summary>Returns an integer in [minInclusive, maxExclusive).</summary>
    int Next(int minInclusive, int maxExclusive);
}

public sealed class SystemRandomSource(Random? random = null) : IRandomSource
{
    private readonly Random _random = random ?? Random.Shared;

    public double NextDouble() => _random.NextDouble();

    public int Next(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
}
