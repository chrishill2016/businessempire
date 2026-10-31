using BlackMarketEmpire.Tests.Fakes;

namespace BlackMarketEmpire.Tests.Services;

public class HighScoreServiceTests
{
    private readonly HighScoreService _service = new(new InMemoryStore());

    private static HighScore Score(long value) => new(Guid.NewGuid(), value, DateTime.UtcNow, "London");

    [Fact]
    public async Task Empty_by_default()
    {
        Assert.Empty(await _service.GetTopAsync());
    }

    [Fact]
    public async Task Scores_are_sorted_highest_first_and_rank_returned()
    {
        await _service.AddAsync(Score(100));
        await _service.AddAsync(Score(300));

        var rank = await _service.AddAsync(Score(200));

        Assert.Equal(2, rank);
        Assert.Equal([300L, 200L, 100L], (await _service.GetTopAsync()).Select(s => s.Score));
    }

    [Fact]
    public async Task Only_top_hundred_are_kept()
    {
        for (var i = 1; i <= 100; i++) await _service.AddAsync(Score(i * 100));

        var rank = await _service.AddAsync(Score(50));

        Assert.Null(rank);
        var top = await _service.GetTopAsync();
        Assert.Equal(100, top.Count);
        Assert.DoesNotContain(top, s => s.Score == 50);
    }

    [Fact]
    public async Task Run_details_are_kept()
    {
        var started = new DateTime(2026, 10, 31, 9, 0, 0, DateTimeKind.Utc);
        await _service.AddAsync(new HighScore(Guid.NewGuid(), 9_000, started.AddMinutes(40), "Dubai", EndReason.Retired, started, 21, 4_500));

        var s = Assert.Single(await _service.GetTopAsync());
        Assert.Equal(started, s.StartedUtc);
        Assert.Equal(21, s.DaysPlayed);
        Assert.Equal(4_500, s.BestTrade);
        Assert.Equal(EndReason.Retired, s.Reason);
    }

    [Fact]
    public async Task Clear_removes_everything()
    {
        await _service.AddAsync(Score(1));
        await _service.ClearAsync();

        Assert.Empty(await _service.GetTopAsync());
    }

    [Fact]
    public async Task Same_run_is_not_recorded_twice()
    {
        var score = Score(500);
        await _service.AddAsync(score);
        await _service.AddAsync(score);

        Assert.Single(await _service.GetTopAsync());
    }
}
