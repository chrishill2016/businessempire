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
    public async Task Only_top_ten_are_kept()
    {
        for (var i = 1; i <= 10; i++) await _service.AddAsync(Score(i * 100));

        var rank = await _service.AddAsync(Score(50));

        Assert.Null(rank);
        var top = await _service.GetTopAsync();
        Assert.Equal(10, top.Count);
        Assert.DoesNotContain(top, s => s.Score == 50);
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
