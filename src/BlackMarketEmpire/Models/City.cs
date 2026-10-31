namespace BlackMarketEmpire.Models;

/// <summary>
/// A city on the map. <see cref="PriceBias"/> makes some goods cheaper or dearer here
/// (1.0 = normal) so certain trade routes are worth learning.
/// </summary>
public sealed record City(
    string Id,
    string Name,
    string Country,
    IReadOnlyDictionary<string, double> PriceBias)
{
    public double BiasFor(string goodId) => PriceBias.TryGetValue(goodId, out var b) ? b : 1.0;
}
