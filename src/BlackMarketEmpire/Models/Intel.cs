namespace BlackMarketEmpire.Models;

/// <summary>A police crackdown: stops are more likely in that city for a few days.</summary>
public sealed class Crackdown
{
    public string CityId { get; set; } = "";
    public int StartDay { get; set; }
    public int EndDay { get; set; }
    /// <summary>Whether the player knows about it.</summary>
    public bool Known { get; set; }

    public bool IsActiveOn(int day) => day >= StartDay && day <= EndDay;
}

public enum InformantTier { Street, Connected }

/// <summary>What a market contact reported about a city on a given day.</summary>
public sealed class ContactReport
{
    public string CityId { get; set; } = "";
    public int Day { get; set; }
    public Dictionary<string, int> Prices { get; set; } = [];
    public List<MarketEvent> Events { get; set; } = [];
    public bool Crackdown { get; set; }
}
