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
    public Task<ActionResult> TravelAsync(string cityId, TravelMethod? method = null) => ApplyAsync(s => engine.Travel(s, cityId, method));
    public Task<ActionResult> BuyGearAsync(string gearId) => ApplyAsync(s => engine.BuyGear(s, gearId));
    public Task<ActionResult> UseFirstAidAsync() => ApplyAsync(engine.UseFirstAid);
    public Task<ActionResult> ShipAsync(string goodId, int qty, string toCityId, string courierId) =>
        ApplyAsync(s => engine.Ship(s, goodId, qty, toCityId, courierId));
    public Task<ActionResult> CollectAsync() => ApplyAsync(engine.Collect);
    public Task<ActionResult> BorrowAsync(string lenderId, long amount) => ApplyAsync(s => engine.Borrow(s, lenderId, amount));
    public Task<ActionResult> RepayLoanAsync(Guid loanId, long amount) => ApplyAsync(s => engine.RepayLoan(s, loanId, amount));
    public Task<ActionResult> DepositAsync(long amount) => ApplyAsync(s => engine.Deposit(s, amount));
    public Task<ActionResult> WithdrawAsync(long amount) => ApplyAsync(s => engine.Withdraw(s, amount));
    public Task<ActionResult> RentStashAsync(StashTier tier) => ApplyAsync(s => engine.RentStash(s, tier));
    public Task<ActionResult> StoreAsync(string goodId, int qty) => ApplyAsync(s => engine.Store(s, goodId, qty));
    public Task<ActionResult> CheckRumourAsync(Guid newsId, InformantTier tier) => ApplyAsync(s => engine.CheckRumour(s, newsId, tier));
    public Task<ActionResult> BuyTipOffAsync() => ApplyAsync(engine.BuyTipOff);
    public Task<ActionResult> BuyContactReportAsync(string cityId) => ApplyAsync(s => engine.BuyContactReport(s, cityId));
    public Task<ActionResult> ArrangeShortageAsync(string cityId, string goodId) => ApplyAsync(s => engine.ArrangeShortage(s, cityId, goodId));
    public Task<ActionResult> TakeFromStashAsync(string goodId, int qty) => ApplyAsync(s => engine.TakeFromStash(s, goodId, qty));
    public Task<ActionResult> WaitAsync() => ApplyAsync(engine.Wait);
    public Task<ActionResult> RepayAsync(long amount) => ApplyAsync(s => engine.RepayDebt(s, amount));
    public Task<ActionResult> EndRunAsync() => ApplyAsync(engine.EndRun);
    public Task<ActionResult> ResolveEncounterAsync(EncounterChoice choice) => ApplyAsync(s => engine.ResolveEncounter(s, choice));

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
                    State.RunId, engine.Score(State), DateTime.UtcNow, GameCatalog.City(State.CityId).Name,
                    State.EndReason, State.StartedUtc, State.Day, State.Stats.BestTrade));
            }
            await SaveAsync();
            Changed?.Invoke();
        }
        return result;
    }

    private Task SaveAsync() => State is null ? Task.CompletedTask : store.SetAsync(StorageKey, State);
}
