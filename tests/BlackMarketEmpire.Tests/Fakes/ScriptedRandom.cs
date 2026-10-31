namespace BlackMarketEmpire.Tests.Fakes;

/// <summary>
/// Returns the given values in order (then 0.5 forever). <see cref="Next"/> maps the next value
/// into the requested range, so 0 = minimum and 0.99 = maximum.
/// </summary>
public sealed class ScriptedRandom(params double[] values) : IRandomSource
{
    private readonly Queue<double> _values = new(values);

    public double NextDouble() => _values.TryDequeue(out var v) ? v : 0.5;

    public int Next(int minInclusive, int maxExclusive) =>
        minInclusive + (int)(NextDouble() * (maxExclusive - minInclusive));
}
