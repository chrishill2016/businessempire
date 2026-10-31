namespace BlackMarketEmpire.Models;

/// <summary>
/// A city on the map. <see cref="PriceBias"/> makes some goods cheaper or dearer here
/// (1.0 = normal) so certain trade routes are worth learning.
/// </summary>
public sealed record City(
    string Id,
    string Name,
    string Country,
    IReadOnlyDictionary<string, double> PriceBias,
    DangerLevel Danger = DangerLevel.Medium,
    string Region = "Europe",
    double Latitude = 0,
    double Longitude = 0)
{
    /// <summary>How much more (or less) likely police are to stop you here.</summary>
    public double PoliceFactor => Danger switch
    {
        DangerLevel.Low => 0.75,
        DangerLevel.High => 1.35,
        _ => 1.0,
    };

    public double BiasFor(string goodId) => PriceBias.TryGetValue(goodId, out var b) ? b : 1.0;
}
