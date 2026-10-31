namespace BlackMarketEmpire.Models;

public sealed record Achievement(string Id, string Name, string Icon, string Description);

/// <summary>Lifetime totals across every run on this device.</summary>
public sealed class CareerStats
{
    public int RunsPlayed { get; set; }
    public int Finished { get; set; }
    public int Retired { get; set; }
    public int Killed { get; set; }
    public long? BestScore { get; set; }
    public long TotalScore { get; set; }
    public long TotalProfit { get; set; }
    public long BestTrade { get; set; }
    public int Trades { get; set; }
    public int Stops { get; set; }
    public int Arrests { get; set; }
    public int Escapes { get; set; }
    public int DaysPlayed { get; set; }
    public long Borrowed { get; set; }
    public List<string> CitiesVisited { get; set; } = [];
    /// <summary>Best daily-challenge score per date (yyyy-MM-dd).</summary>
    public Dictionary<string, long> DailyBest { get; set; } = [];
    public Dictionary<Difficulty, long> BestByDifficulty { get; set; } = [];
    public DateTime? LastPlayedUtc { get; set; }
}

public sealed class AppSettings
{
    public bool SoundOn { get; set; } = true;
    public bool VibrationOn { get; set; } = true;
}

public enum SoundCue { Buy, Sell, Travel, Siren, Escape, Arrest, Hurt, Death, Error, Achievement, Money, Tip, Finish }
