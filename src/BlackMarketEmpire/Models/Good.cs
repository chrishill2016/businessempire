namespace BlackMarketEmpire.Models;

/// <summary>A tradeable black-market item and its normal price range.</summary>
public sealed record Good(string Id, string Name, string Icon, int MinPrice, int MaxPrice);
