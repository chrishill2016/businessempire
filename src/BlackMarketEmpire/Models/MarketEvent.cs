namespace BlackMarketEmpire.Models;

public enum MarketEventKind
{
    /// <summary>Supply dried up — prices well above normal.</summary>
    Spike,
    /// <summary>Market flooded — prices well below normal.</summary>
    Crash,
}

/// <summary>
/// A real price shock for one good in one city, active from <see cref="StartDay"/> to <see cref="EndDay"/> inclusive.
/// </summary>
public sealed record MarketEvent(
    string CityId,
    string GoodId,
    MarketEventKind Kind,
    double Multiplier,
    int StartDay,
    int EndDay,
    /// <summary>False for events planned in secret: nobody's heard about them until they start (or someone sells you a tip).</summary>
    bool Announced = true,
    Guid Id = default)
{
    public int Days => EndDay - StartDay + 1;

    public bool IsActiveOn(int day) => day >= StartDay && day <= EndDay;
}
