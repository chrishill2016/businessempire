namespace BlackMarketEmpire.Models;

public sealed record HighScore(
    Guid RunId,
    long Score,
    DateTime AchievedUtc,
    string FinalCity,
    EndReason Reason = EndReason.Finished,
    DateTime? StartedUtc = null);
