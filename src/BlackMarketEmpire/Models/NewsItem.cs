namespace BlackMarketEmpire.Models;

public enum NewsStatus
{
    /// <summary>Heard about it, haven't seen it with your own eyes.</summary>
    Rumour,
    /// <summary>You were there and it was true.</summary>
    Confirmed,
    /// <summary>It was true, but it had already blown over by the time you arrived.</summary>
    Expired,
    /// <summary>You went there and it was nonsense.</summary>
    False,
}

/// <summary>
/// A headline in the intel feed. Some are false rumours — <see cref="IsTrue"/> is hidden from the
/// player until they go to the city and see for themselves.
/// </summary>
public sealed class NewsItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Day { get; set; }
    public string CityId { get; set; } = "";
    public string GoodId { get; set; } = "";
    public MarketEventKind Kind { get; set; }
    public string Headline { get; set; } = "";
    public bool IsTrue { get; set; }
    public NewsStatus Status { get; set; } = NewsStatus.Rumour;
}
