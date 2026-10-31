namespace BlackMarketEmpire.Models;

/// <summary>A tradeable black-market item and its normal price range.</summary>
/// <param name="Size">Suitcase space one unit takes up (a painting is bulkier than a phone).</param>
/// <param name="CityIds">Cities where it's traded at all. Null means everywhere.</param>
public sealed record Good(
    string Id,
    string Name,
    string Icon,
    int MinPrice,
    int MaxPrice,
    int Size = 1,
    IReadOnlyList<string>? CityIds = null)
{
    public bool TradedIn(string cityId) => CityIds is null || CityIds.Contains(cityId);
}
