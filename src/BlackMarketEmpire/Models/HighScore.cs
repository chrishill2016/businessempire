namespace BlackMarketEmpire.Models;

/// <summary>A finished run on the device's high-score table.</summary>
/// <param name="AchievedUtc">When the run ended.</param>
/// <param name="StartedUtc">When the run started (missing for runs saved before Phase 3).</param>
/// <param name="BestTrade">Biggest profit from a single sale.</param>
public sealed record HighScore(
    Guid RunId,
    long Score,
    DateTime AchievedUtc,
    string FinalCity,
    EndReason Reason = EndReason.Finished,
    DateTime? StartedUtc = null,
    int DaysPlayed = 0,
    long BestTrade = 0);
