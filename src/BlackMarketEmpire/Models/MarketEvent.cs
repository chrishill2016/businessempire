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
    int EndDay)
{
    public bool IsActiveOn(int day) => day >= StartDay && day <= EndDay;
}
