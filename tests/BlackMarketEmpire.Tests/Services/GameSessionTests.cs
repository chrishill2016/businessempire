using BlackMarketEmpire.Tests.Fakes;

namespace BlackMarketEmpire.Tests.Services;

public class GameSessionTests
{
    private readonly InMemoryStore _store = new();
    private readonly HighScoreService _highScores;

    public GameSessionTests() => _highScores = new HighScoreService(_store);

    private GameSession NewSession() => new(TestGame.Engine(), _store, _highScores);

    [Fact]
    public async Task Starting_a_run_saves_it()
    {
        var session = NewSession();

        await session.StartNewAsync();

        Assert.True(session.HasActiveRun);
        Assert.True(_store.Contains(GameSession.StorageKey));
    }

    [Fact]
    public async Task Run_is_resumed_after_app_restart()
    {
        var first = NewSession();
        await first.StartNewAsync();
        await first.WaitAsync();
        first.State!.Market["cigs"] = 20;
        await first.BuyAsync("cigs", 3);

        var second = NewSession();
        await second.LoadAsync();

        Assert.NotNull(second.State);
        Assert.Equal(first.State.RunId, second.State!.RunId);
        Assert.Equal(2, second.State.Day);
        Assert.Equal(3, second.State.Inventory["cigs"]);
        Assert.Equal(first.State.News.Select(n => n.Headline), second.State.News.Select(n => n.Headline));
        Assert.Equal(first.State.Events, second.State.Events);
    }

    [Fact]
    public async Task Ending_a_run_records_a_high_score_once()
    {
        var session = NewSession();
        await session.StartNewAsync();

        await session.EndRunAsync();
        await session.EndRunAsync(); // second call is refused

        Assert.False(session.HasActiveRun);
        Assert.Equal(1, session.FinalRank);
        var scores = await _highScores.GetTopAsync();
        Assert.Single(scores);
        Assert.Equal(-2_500, scores[0].Score);
    }

    [Fact]
    public async Task Changed_fires_only_on_successful_actions()
    {
        var session = NewSession();
        await session.StartNewAsync();
        var fired = 0;
        session.Changed += () => fired++;

        await session.TravelAsync("london"); // already here → fails
        await session.WaitAsync();

        Assert.Equal(1, fired);
    }

    [Fact]
    public async Task Actions_without_a_run_fail_gracefully()
    {
        var session = NewSession();

        var result = await session.WaitAsync();

        Assert.False(result.Success);
    }
}
