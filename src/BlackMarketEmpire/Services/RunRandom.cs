namespace BlackMarketEmpire.Services;

/// <summary>
/// A small seeded random-number generator (SplitMix64) whose whole state is one number, so it can be
/// saved with the run. The same seed and the same choices always play out the same way — that's what
/// makes the daily challenge fair.
/// </summary>
public sealed class RunRandom(ulong state = 0) : IRandomSource
{
    public ulong State { get; set; } = state;

    public ulong NextUInt64()
    {
        var z = State += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    public double NextDouble() => (NextUInt64() >> 11) * (1.0 / (1UL << 53));

    public int Next(int minInclusive, int maxExclusive) =>
        maxExclusive <= minInclusive ? minInclusive : minInclusive + (int)(NextDouble() * (maxExclusive - minInclusive));

    /// <summary>A stable seed from text (FNV-1a), e.g. the daily challenge date.</summary>
    public static ulong SeedFrom(string text)
    {
        var hash = 14695981039346656037UL;
        foreach (var c in text)
        {
            hash ^= c;
            hash *= 1099511628211UL;
        }
        return hash;
    }

    public static ulong RandomSeed() => (ulong)Random.Shared.NextInt64() ^ (ulong)DateTime.UtcNow.Ticks;
}
