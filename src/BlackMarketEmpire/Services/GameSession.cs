using BlackMarketEmpire.Models;

namespace BlackMarketEmpire.Services;

/// <summary>
/// The current run as the UI sees it. Applies actions through <see cref="GameEngine"/>,
/// saves after every action so the game survives the phone closing the app, and records
/// the high score when the run ends.
/// </summary>
public sealed class GameSession(GameEngine engine, ILocalStore store, HighScoreService highScores)
{
    public const string StorageKey = "current-run";

    public GameEngine Engine => engine;
    public GameState? State { get; private set; }
    public int? FinalRank { get; private set; }
    public bool HasActiveRun => State is { IsOver: false };

    public event Action? Changed;

    public async Task LoadAsync()
    {
        State ??= await store.GetAsync<GameState>(StorageKey);
    }

    public async Task StartNewAsync()
    {
        State = engine.NewGame();
        FinalRank = null;
        await SaveAsync();
    }

    public Task<ActionResult> BuyAsync(string goodId, int qty) => ApplyAsync(s => engine.Buy(s, goodId, qty));
    public Task<ActionResult> SellAsync(string goodId, int qty) => ApplyAsync(s => engine.Sell(s, goodId, qty));
    public Task<ActionResult> TravelAsync(string cityId) => ApplyAsync(s => engine.Travel(s, cityId));
    public Task<ActionResult> WaitAsync() => ApplyAsync(engine.Wait);
    public Task<ActionResult> RepayAsync(long amount) => ApplyAsync(s => engine.RepayDebt(s, amount));
    public Task<ActionResult> EndRunAsync() => ApplyAsync(engine.EndRun);

    private async Task<ActionResult> ApplyAsync(Func<GameState, ActionResult> action)
    {
        if (State is null) return ActionResult.Fail("No run in progress.");

        var wasOver = State.IsOver;
        var result = action(State);
        if (result.Success)
        {
            if (!wasOver && State.IsOver)
            {
                FinalRank = await highScores.AddAsync(new HighScore(
                    State.RunId, engine.Score(State), DateTime.UtcNow, GameCatalog.City(State.CityId).Name));
            }
            await SaveAsync();
            Changed?.Invoke();
        }
        return result;
    }

    private Task SaveAsync() => State is null ? Task.CompletedTask : store.SetAsync(StorageKey, State);
}
