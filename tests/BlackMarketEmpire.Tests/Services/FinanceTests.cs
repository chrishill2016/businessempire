using BlackMarketEmpire.Tests.Fakes;

namespace BlackMarketEmpire.Tests.Services;

public class LenderOfferTests
{
    private static readonly GameRules Rules = GameRules.Default;
    private static Lender Eddie => GameCatalog.Lender("eddie");

    private static GameState In(string city) => new() { CityId = city, Cash = 1_000, Debt = 5_000, Capacity = 100, Day = 3 };

    [Theory]
    [InlineData(0, 1)]
    [InlineData(5, 3)]
    [InlineData(10, 5)]
    public void Reputation_maps_to_stars(int rep, int stars) => Assert.Equal(stars, FinanceService.Stars(rep));

    [Fact]
    public void Average_reputation_gets_the_base_rate_and_limit()
    {
        var offer = FinanceService.Offer(In("london"), Rules, Eddie);

        Assert.Null(offer.Refusal);
        Assert.Equal(0.08, offer.Rate, 4);
        Assert.Equal(5_000, offer.Limit);
        Assert.Equal(3, offer.Stars);
    }

    [Fact]
    public void Good_reputation_means_cheaper_and_bigger_loans()
    {
        var state = In("london");
        state.Reputation["eddie"] = 10;

        var offer = FinanceService.Offer(state, Rules, Eddie);

        Assert.Equal(0.056, offer.Rate, 4);
        Assert.Equal(7_500, offer.Limit);
    }

    [Fact]
    public void Arrests_and_heat_raise_the_rate()
    {
        var clean = FinanceService.Offer(In("london"), Rules, Eddie).Rate;
        var state = In("london");
        state.Stats.Arrests = 2;
        state.Heat["london"] = 100;

        Assert.Equal(clean * 1.4, FinanceService.Offer(state, Rules, Eddie).Rate, 4);
    }

    [Fact]
    public void Lenders_only_work_in_their_cities()
    {
        var offer = FinanceService.Offer(In("dubai"), Rules, Eddie);

        Assert.False(offer.CanBorrow);
        Assert.Contains("London and Istanbul", offer.Refusal);
    }

    [Fact]
    public void Burned_lenders_refuse()
    {
        var state = In("london");
        state.Reputation["eddie"] = 1;

        Assert.Contains("won't deal", FinanceService.Offer(state, Rules, Eddie).Refusal);
    }

    [Fact]
    public void Bank_needs_net_worth_and_a_clean_record()
    {
        var bank = GameCatalog.Lender("bank");
        var poor = In("london");
        Assert.Contains("net worth", FinanceService.Offer(poor, Rules, bank).Refusal);

        var rich = In("london");
        rich.Cash = 40_000;
        var offer = FinanceService.Offer(rich, Rules, bank);
        Assert.Null(offer.Refusal);
        Assert.Equal(17_500, offer.Limit); // half of 35,000 net worth

        rich.Stats.Arrests = 1;
        Assert.Contains("arrest record", FinanceService.Offer(rich, Rules, bank).Refusal);
    }
}

public class LoanTests
{
    private static GameEngine Engine(params double[] financeRolls)
    {
        var random = new SystemRandomSource(new Random(1));
        return new GameEngine(new MarketService(random), new EventService(random), new EncounterService(random),
            new ShippingService(random), new FinanceService(new ScriptedRandom(financeRolls)), TestGame.AllAvailable);
    }

    [Fact]
    public void Borrowing_adds_cash_and_a_loan()
    {
        var engine = Engine();
        var state = engine.NewGame();

        var result = engine.Borrow(state, "eddie", 3_000);

        Assert.True(result.Success);
        Assert.Equal(5_500, state.Cash);
        var loan = Assert.Single(state.Loans);
        Assert.Equal(3_000, loan.Owed);
        Assert.Equal(8, loan.DueDay);
        Assert.Equal(0.08, loan.Rate, 4);
        Assert.Equal(8_000, state.TotalDebt);
        Assert.Equal(3_000, state.Stats.Borrowed);
    }

    [Fact]
    public void Cannot_borrow_over_the_limit()
    {
        var engine = Engine();
        var state = engine.NewGame();
        engine.Borrow(state, "eddie", 4_000);

        var result = engine.Borrow(state, "eddie", 1_500);

        Assert.False(result.Success);
        Assert.Contains("£1,000 more", result.Message);
    }

    [Fact]
    public void Loans_compound_daily()
    {
        var engine = Engine();
        var state = engine.NewGame();
        engine.Borrow(state, "eddie", 1_000);

        engine.Wait(state);
        engine.Wait(state);

        Assert.Equal(1_167, state.Loans[0].Owed); // 1000 → 1080 → 1166.4 rounded up
    }

    [Fact]
    public void Repaying_on_time_builds_reputation()
    {
        var engine = Engine();
        var state = engine.NewGame();
        engine.Borrow(state, "eddie", 1_000);
        engine.Wait(state);

        var result = engine.RepayLoan(state, state.Loans[0].Id, 5_000);

        Assert.True(result.Success);
        Assert.Equal(LoanStatus.Repaid, state.Loans[0].Status);
        Assert.Equal(7, state.ReputationWith("eddie")); // early (2+ days before due) → +2
        Assert.Equal(state.Debt, state.TotalDebt);       // only the original stake left
    }

    [Fact]
    public void Part_payment_leaves_the_rest_owed()
    {
        var engine = Engine();
        var state = engine.NewGame();
        engine.Borrow(state, "eddie", 1_000);

        engine.RepayLoan(state, state.Loans[0].Id, 400);

        Assert.Equal(600, state.Loans[0].Owed);
        Assert.Equal(LoanStatus.Active, state.Loans[0].Status);
    }

    [Fact]
    public void Must_be_in_the_lenders_city_to_repay()
    {
        var engine = Engine();
        var state = engine.NewGame();
        engine.Borrow(state, "eddie", 1_000);
        engine.Travel(state, "amsterdam", TravelMethod.Train);

        Assert.False(engine.RepayLoan(state, state.Loans[0].Id, 1_000).Success);
    }

    [Fact]
    public void Overdue_loans_cost_more_and_hurt_reputation_once()
    {
        var engine = Engine(0.99, 0.99, 0.99, 0.99, 0.99, 0.99, 0.99, 0.99, 0.99, 0.99, 0.99); // no enforcer visits
        var state = engine.NewGame();
        engine.Borrow(state, "accountant", 100); // fails: not in Amsterdam
        engine.Borrow(state, "eddie", 1_000);     // due day 8, 1 day grace
        for (var i = 0; i < 9; i++) engine.Wait(state); // day 10: overdue from day 10

        Assert.True(FinanceService.IsOverdue(state.Loans[0], state.Day));
        Assert.True(state.Loans[0].MarkedOverdue);
        Assert.Equal(2, state.ReputationWith("eddie"));
        Assert.Contains(state.Log, l => l.Contains("overdue"));
        Assert.Contains("Pay back", engine.LoanOffer(state, "eddie").Refusal);
    }

    [Fact]
    public void Enforcers_take_cash_and_hurt_you()
    {
        var engine = Engine(0.0, 0.0); // visit happens, minimum damage
        var state = engine.NewGame();
        state.Loans.Add(new Loan { LenderId = "eddie", Owed = 1_000, Rate = 0.08, DueDay = 1 });
        state.Day = 5;

        var visit = new FinanceService(new ScriptedRandom(0.0, 0.0)).MaybeEnforcers(state, engine.Rules);

        Assert.NotNull(visit);
        Assert.Equal(10, visit!.Damage);
        Assert.Equal(1_500, state.Cash);
        Assert.Equal(LoanStatus.Repaid, state.Loans[0].Status);
        Assert.Equal(1, state.Stats.EnforcerVisits);
    }

    [Fact]
    public void Syndicate_enforcers_take_goods_too()
    {
        var state = new GameState { CityId = "istanbul", Cash = 100, Day = 9, Health = 100, Capacity = 100 };
        state.Inventory["phones"] = 10;
        state.AverageCost["phones"] = 500;
        state.Loans.Add(new Loan { LenderId = "syndicate", Owed = 20_000, Rate = 0.1, DueDay = 3 });

        var visit = new FinanceService(new ScriptedRandom(0.0, 0.0)).MaybeEnforcers(state, GameRules.Default);

        Assert.Empty(state.Inventory);
        Assert.Equal(0, state.Cash);
        Assert.Equal(20_000 - 100 - 2_500, state.Loans[0].Owed);
        Assert.Equal(30, visit!.Damage);
    }

    [Fact]
    public void No_visit_outside_the_lenders_cities()
    {
        var state = new GameState { CityId = "dubai", Cash = 100, Day = 9 };
        state.Loans.Add(new Loan { LenderId = "eddie", Owed = 1_000, DueDay = 3 });

        Assert.Null(new FinanceService(new ScriptedRandom(0.0)).MaybeEnforcers(state, GameRules.Default));
    }

    [Fact]
    public void Enforcers_can_kill_you()
    {
        var engine = Engine(0.0, 0.99); // visit, max damage
        var state = engine.NewGame();
        state.Health = 10;
        state.Loans.Add(new Loan { LenderId = "eddie", Owed = 100_000, Rate = 0.08, DueDay = 1, MarkedOverdue = true });
        state.Day = 4;

        var result = engine.Wait(state);

        Assert.True(state.IsOver);
        Assert.Equal(EndReason.Killed, state.EndReason);
        Assert.Contains("enforcers", result.Message);
    }
}

public class BankAndScoreTests
{
    private readonly GameEngine _engine = TestGame.Engine();

    [Fact]
    public void Deposit_and_withdraw_only_where_there_is_a_branch()
    {
        var state = _engine.NewGame(); // London has a branch

        Assert.True(_engine.Deposit(state, 1_000).Success);
        Assert.Equal(1_500, state.Cash);
        Assert.Equal(1_000, state.Bank);

        state.CityId = "marseille";
        Assert.False(_engine.Withdraw(state, 500).Success);
        state.CityId = "london";
        Assert.True(_engine.Withdraw(state, 400).Success);
        Assert.Equal(600, state.Bank);
    }

    [Fact]
    public void Savings_earn_interest()
    {
        var state = _engine.NewGame();
        _engine.Deposit(state, 2_000);

        _engine.Wait(state);

        Assert.Equal(2_010, state.Bank);
    }

    [Fact]
    public void Score_counts_bank_and_all_debts()
    {
        var state = _engine.NewGame();
        _engine.Deposit(state, 1_000);
        _engine.Borrow(state, "eddie", 2_000);

        _engine.EndRun(state);

        Assert.Equal(3_500 + 1_000 - 5_000 - 2_000, _engine.Score(state));
    }

    [Fact]
    public void Bank_survives_death_but_cash_does_not()
    {
        var state = _engine.NewGame();
        _engine.Deposit(state, 1_000);
        state.IsOver = true;
        state.EndReason = EndReason.Killed;

        Assert.Equal(1_000 - 5_000, _engine.Score(state));
    }

    [Fact]
    public void Fines_never_touch_the_bank()
    {
        var random = new SystemRandomSource(new Random(1));
        var engine = new GameEngine(new MarketService(random), new EventService(random), new EncounterService(new ScriptedRandom(0)),
            new ShippingService(random), new FinanceService(random), TestGame.AllAvailable);
        var state = engine.NewGame();
        engine.Deposit(state, 2_000);
        state.PendingEncounter = new Encounter { Kind = EncounterKind.Police, BribeCost = 300 };

        engine.ResolveEncounter(state, EncounterChoice.Surrender);

        Assert.Equal(400, state.Cash);
        Assert.True(state.Bank >= 2_000);
    }
}

public class StashTests
{
    private static GameEngine Engine(params double[] financeRolls)
    {
        var random = new SystemRandomSource(new Random(1));
        return new GameEngine(new MarketService(random), new EventService(random), new EncounterService(random),
            new ShippingService(random), new FinanceService(new ScriptedRandom(financeRolls)), TestGame.AllAvailable);
    }

    private static GameState WithGoods(GameEngine engine)
    {
        var state = TestGame.StateWithMarket(engine, new() { ["cigs"] = 10 });
        engine.Buy(state, "cigs", 100);
        return state;
    }

    [Fact]
    public void Rent_store_and_take()
    {
        var engine = Engine(0.99);
        var state = WithGoods(engine);

        Assert.True(engine.RentStash(state, StashTier.Lockup).Success);
        Assert.Equal(500, state.Cash);
        Assert.True(engine.Store(state, "cigs", 60).Success);
        Assert.Equal(40, state.Inventory["cigs"]);
        Assert.Equal(60, state.StashIn("london")!.Goods["cigs"]);
        Assert.Equal(10, state.StashIn("london")!.AverageCost["cigs"]);
        Assert.True(engine.TakeFromStash(state, "cigs", 60).Success);
        Assert.Equal(100, state.Inventory["cigs"]);
        Assert.Empty(state.StashIn("london")!.Goods);
    }

    [Fact]
    public void Stash_only_works_in_its_city()
    {
        var engine = Engine(0.99, 0.99);
        var state = WithGoods(engine);
        engine.RentStash(state, StashTier.Lockup);
        engine.Travel(state, "amsterdam", TravelMethod.Train);

        Assert.False(engine.Store(state, "cigs", 10).Success);
    }

    [Fact]
    public void Stash_capacity_is_enforced()
    {
        var engine = Engine();
        var state = WithGoods(engine);
        state.Cash = 10_000;
        engine.RentStash(state, StashTier.Lockup);
        state.StashIn("london")!.Goods["whisky"] = 140;

        engine.Store(state, "cigs", 100);

        Assert.Equal(10, state.StashIn("london")!.Goods["cigs"]);
        Assert.Equal(90, state.Inventory["cigs"]);
    }

    [Fact]
    public void Upgrading_to_a_warehouse_keeps_the_goods()
    {
        var engine = Engine();
        var state = WithGoods(engine);
        state.Cash = 10_000;
        engine.RentStash(state, StashTier.Lockup);
        engine.Store(state, "cigs", 50);

        Assert.True(engine.RentStash(state, StashTier.Warehouse).Success);
        Assert.False(engine.RentStash(state, StashTier.Lockup).Success);
        Assert.Equal(StashTier.Warehouse, state.StashIn("london")!.Tier);
        Assert.Equal(50, state.StashIn("london")!.Units);
    }

    [Fact]
    public void Stashes_can_be_raided()
    {
        var engine = Engine(0.0);
        var state = WithGoods(engine);
        engine.RentStash(state, StashTier.Lockup);
        engine.Store(state, "cigs", 100);

        engine.Wait(state);

        Assert.Equal(0, state.StashIn("london")!.Units);
        Assert.Equal(1, state.Stats.StashRaids);
        Assert.Contains(state.Log, l => l.Contains("raided your stash"));
    }

    [Fact]
    public void Heat_raises_raid_chance_and_warehouses_are_safer()
    {
        var rules = GameRules.Default;
        var state = new GameState();
        var lockup = new Stash { CityId = "london", Tier = StashTier.Lockup };
        var warehouse = new Stash { CityId = "london", Tier = StashTier.Warehouse };

        Assert.True(FinanceService.RaidChance(state, rules, warehouse) < FinanceService.RaidChance(state, rules, lockup));
        var cold = FinanceService.RaidChance(state, rules, lockup);
        state.Heat["london"] = 50;
        Assert.Equal(cold + 0.04, FinanceService.RaidChance(state, rules, lockup), 6);
    }
}

public class CityDangerTests
{
    [Fact]
    public void Dangerous_cities_stop_you_more()
    {
        var rules = GameRules.Default;
        var state = new GameState { Capacity = 100, Inventory = { ["cigs"] = 10 }, AverageCost = { ["cigs"] = 20 } };

        var low = EncounterService.StopChance(state, rules, TravelMethod.Train, "amsterdam");
        var high = EncounterService.StopChance(state, rules, TravelMethod.Train, "marseille");

        Assert.Equal(DangerLevel.Low, GameCatalog.City("amsterdam").Danger);
        Assert.Equal(DangerLevel.High, GameCatalog.City("marseille").Danger);
        Assert.Equal(1.35 / 0.75, high / low, 6);
    }
}
