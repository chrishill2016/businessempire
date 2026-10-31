using BlackMarketEmpire.Models;

namespace BlackMarketEmpire.Services;

/// <summary>What a lender would offer you right now.</summary>
public sealed record LoanOffer(Lender Lender, int Stars, double Rate, long Limit, long Outstanding, string? Refusal)
{
    public long Available => Math.Max(0, Limit - Outstanding);
    public bool CanBorrow => Refusal is null && Available > 0;
}

/// <summary>Result of a lender's enforcers catching up with you.</summary>
public sealed record EnforcerVisit(string Message, int Damage);

/// <summary>Loans, reputation, the bank and stash houses.</summary>
public sealed class FinanceService(IRandomSource random)
{
    // ----- Reputation & offers -----

    public static int Stars(int reputation) => Math.Clamp((int)Math.Round(1 + reputation * 0.4), 1, 5);

    public static void AdjustReputation(GameState state, string lenderId, int delta) =>
        state.Reputation[lenderId] = Math.Clamp(state.ReputationWith(lenderId) + delta, 0, 10);

    public static long Outstanding(GameState state, string lenderId) =>
        state.Loans.Where(l => l.LenderId == lenderId && l.Status == LoanStatus.Active).Sum(l => l.Owed);

    public static bool IsOverdue(Loan loan, int day) =>
        loan.Status == LoanStatus.Active && day > loan.DueDay + GameCatalog.Lender(loan.LenderId).GraceDays;

    public static LoanOffer Offer(GameState state, GameRules rules, Lender lender)
    {
        var rep = state.ReputationWith(lender.Id);
        var stars = Stars(rep);
        var multiplier = 1.3 - 0.06 * rep                      // 10 rep → ×0.7, 0 rep → ×1.3
                         + 0.1 * state.Stats.Arrests            // a record makes everyone nervous
                         + state.HeatIn(state.CityId) / 100.0 * 0.2;
        var rate = Math.Round(lender.BaseRate * multiplier, 4);
        var outstanding = Outstanding(state, lender.Id);

        long limit;
        string? refusal = null;
        if (lender.Kind == LenderKind.Bank)
        {
            limit = Math.Max(0, state.NetWorth / 2);
            if (state.Stats.Arrests > 0) refusal = "The bank won't lend to someone with an arrest record.";
            else if (state.NetWorth < rules.BankLoanMinNetWorth) refusal = $"The bank wants to see a net worth of at least {Money.Format(rules.BankLoanMinNetWorth)}.";
        }
        else
        {
            limit = (long)(lender.BaseLimit * (0.5 + 0.1 * rep)) / 100 * 100;
        }

        if (refusal is null && !lender.CityIds.Contains(state.CityId))
            refusal = $"{lender.Name} only does business in {CityList(lender)}.";
        if (refusal is null && rep <= 1)
            refusal = $"{lender.Name} won't deal with you any more.";
        if (refusal is null && state.Loans.Any(l => l.LenderId == lender.Id && IsOverdue(l, state.Day)))
            refusal = "Pay back what you owe first.";

        return new LoanOffer(lender, stars, rate, limit, outstanding, refusal);
    }

    public static string CityList(Lender lender)
    {
        var names = lender.CityIds.Select(id => GameCatalog.City(id).Name).ToList();
        return names.Count <= 1 ? string.Join("", names) : $"{string.Join(", ", names[..^1])} and {names[^1]}";
    }

    // ----- Borrowing & repaying -----

    public ActionResult Borrow(GameState state, GameRules rules, string lenderId, long amount)
    {
        var offer = Offer(state, rules, GameCatalog.Lender(lenderId));
        if (offer.Refusal is { } refusal) return ActionResult.Fail(refusal);
        if (amount <= 0) return ActionResult.Fail("Choose how much to borrow.");
        if (amount > offer.Available) return ActionResult.Fail($"{offer.Lender.Name} will only lend you {Money.Format(offer.Available)} more.");

        var loan = new Loan
        {
            LenderId = lenderId,
            Principal = amount,
            Owed = amount,
            Rate = offer.Rate,
            TakenDay = state.Day,
            DueDay = Math.Min(state.Day + offer.Lender.TermDays, rules.TotalDays),
        };
        state.Loans.Insert(0, loan);
        state.Cash += amount;
        state.Stats.Borrowed += amount;
        return ActionResult.Ok(
            $"Borrowed {Money.Format(amount)} from {offer.Lender.Name} at {offer.Rate:P1} a day. Due back on day {loan.DueDay}.");
    }

    public ActionResult Repay(GameState state, Guid loanId, long amount)
    {
        var loan = state.Loans.FirstOrDefault(l => l.Id == loanId && l.Status == LoanStatus.Active);
        if (loan is null) return ActionResult.Fail("That loan's already settled.");
        var lender = GameCatalog.Lender(loan.LenderId);
        if (!lender.CityIds.Contains(state.CityId))
            return ActionResult.Fail($"You can only pay {lender.Name} in {CityList(lender)}.");
        var paid = Math.Min(amount, Math.Min(state.Cash, loan.Owed));
        if (paid <= 0) return ActionResult.Fail("You've no cash to pay with.");

        state.Cash -= paid;
        loan.Owed -= paid;
        if (loan.Owed > 0)
            return ActionResult.Ok($"Paid {Money.Format(paid)} to {lender.Name}. {Money.Format(loan.Owed)} still owed.");

        loan.Status = LoanStatus.Repaid;
        if (state.Day <= loan.DueDay)
        {
            AdjustReputation(state, lender.Id, state.Day <= loan.DueDay - 2 ? 2 : 1);
            return ActionResult.Ok($"Loan from {lender.Name} paid off on time. They'll remember that. ({new string('★', Stars(state.ReputationWith(lender.Id)))})");
        }
        return ActionResult.Ok($"Loan from {lender.Name} finally paid off. Late, but paid.");
    }

    // ----- Bank -----

    public static bool HasBankHere(GameState state) => GameCatalog.BankCities.Contains(state.CityId);

    public static ActionResult Deposit(GameState state, long amount)
    {
        if (!HasBankHere(state)) return ActionResult.Fail("There's no branch here.");
        var paid = Math.Min(amount, state.Cash);
        if (paid <= 0) return ActionResult.Fail("Choose an amount to deposit.");
        state.Cash -= paid;
        state.Bank += paid;
        return ActionResult.Ok($"Deposited {Money.Format(paid)}. Balance {Money.Format(state.Bank)}.");
    }

    public static ActionResult Withdraw(GameState state, long amount)
    {
        if (!HasBankHere(state)) return ActionResult.Fail("There's no branch here.");
        var taken = Math.Min(amount, state.Bank);
        if (taken <= 0) return ActionResult.Fail("Nothing to withdraw.");
        state.Bank -= taken;
        state.Cash += taken;
        return ActionResult.Ok($"Withdrew {Money.Format(taken)}. Balance {Money.Format(state.Bank)}.");
    }

    // ----- Stash houses -----

    public static int Capacity(GameRules rules, StashTier tier) => tier switch
    {
        StashTier.Lockup => rules.LockupCapacity,
        StashTier.Warehouse => rules.WarehouseCapacity,
        _ => 0,
    };

    public static long Price(GameRules rules, StashTier tier) =>
        tier == StashTier.Warehouse ? rules.WarehousePrice : rules.LockupPrice;

    public static double RaidChance(GameState state, GameRules rules, Stash stash) =>
        (stash.Tier == StashTier.Warehouse ? rules.WarehouseRaidChance : rules.LockupRaidChance)
        + state.HeatIn(stash.CityId) / 100.0 * rules.RaidChanceAtMaxHeat;

    public static ActionResult RentStash(GameState state, GameRules rules, StashTier tier)
    {
        var existing = state.StashIn(state.CityId);
        if (existing is not null && existing.Tier >= tier) return ActionResult.Fail("You already have that here.");
        var price = Price(rules, tier);
        if (price > state.Cash) return ActionResult.Fail($"It costs {Money.Format(price)}. You can't afford it.");

        state.Cash -= price;
        if (existing is null) state.Stashes.Add(new Stash { CityId = state.CityId, Tier = tier });
        else existing.Tier = tier;
        var name = tier == StashTier.Warehouse ? "a warehouse" : "a lock-up";
        return ActionResult.Ok($"Rented {name} in {GameCatalog.City(state.CityId).Name} for {Money.Format(price)}. Space for {Capacity(rules, tier)} items.");
    }

    public static ActionResult Store(GameState state, GameRules rules, string goodId, int quantity)
    {
        var stash = state.StashIn(state.CityId);
        if (stash is null) return ActionResult.Fail("You don't have a stash here.");
        var held = state.Inventory.GetValueOrDefault(goodId);
        var room = Capacity(rules, stash.Tier) - stash.Units;
        quantity = Math.Min(quantity, Math.Min(held, room));
        if (quantity <= 0) return ActionResult.Fail(room <= 0 ? "Your stash is full." : "You don't have any of that.");

        Move(state.Inventory, state.AverageCost, stash.Goods, stash.AverageCost, goodId, quantity);
        return ActionResult.Ok($"Stashed {quantity} × {GameCatalog.Good(goodId).Name}.");
    }

    public static ActionResult Take(GameState state, string goodId, int quantity)
    {
        var stash = state.StashIn(state.CityId);
        if (stash is null) return ActionResult.Fail("You don't have a stash here.");
        quantity = Math.Min(quantity, Math.Min(stash.Goods.GetValueOrDefault(goodId), state.FreeSpace));
        if (quantity <= 0) return ActionResult.Fail(state.FreeSpace == 0 ? "Your suitcase is full." : "None of that in the stash.");

        Move(stash.Goods, stash.AverageCost, state.Inventory, state.AverageCost, goodId, quantity);
        return ActionResult.Ok($"Took {quantity} × {GameCatalog.Good(goodId).Name} from the stash.");
    }

    private static void Move(Dictionary<string, int> fromGoods, Dictionary<string, long> fromCost,
        Dictionary<string, int> toGoods, Dictionary<string, long> toCost, string goodId, int quantity)
    {
        var unitCost = fromCost.GetValueOrDefault(goodId);
        var held = toGoods.GetValueOrDefault(goodId);
        toCost[goodId] = (toCost.GetValueOrDefault(goodId) * held + unitCost * quantity) / (held + quantity);
        toGoods[goodId] = held + quantity;

        var left = fromGoods[goodId] - quantity;
        if (left == 0)
        {
            fromGoods.Remove(goodId);
            fromCost.Remove(goodId);
        }
        else
        {
            fromGoods[goodId] = left;
        }
    }

    // ----- Daily -----

    /// <summary>Interest, overdue loans, bank interest and stash raids for one day. Returns log messages.</summary>
    public IReadOnlyList<string> OnNewDay(GameState state, GameRules rules)
    {
        var messages = new List<string>();

        foreach (var loan in state.Loans.Where(l => l.Status == LoanStatus.Active))
        {
            var overdue = IsOverdue(loan, state.Day);
            var rate = loan.Rate + (overdue ? rules.OverduePenaltyRate : 0);
            loan.Owed = (long)Math.Ceiling(loan.Owed * (1 + rate));
            if (overdue && !loan.MarkedOverdue)
            {
                loan.MarkedOverdue = true;
                AdjustReputation(state, loan.LenderId, -3);
                var lender = GameCatalog.Lender(loan.LenderId);
                messages.Add($"Your loan from {lender.Name} is overdue. The rate just went up — and they know where to find you in {CityList(lender)}.");
            }
        }

        if (state.Bank > 0)
        {
            state.Bank += (long)(state.Bank * rules.BankInterest);
        }

        foreach (var stash in state.Stashes.Where(s => s.Units > 0))
        {
            if (random.NextDouble() >= RaidChance(state, rules, stash)) continue;
            var units = stash.Units;
            var value = stash.Goods.Sum(kv => kv.Value * stash.AverageCost.GetValueOrDefault(kv.Key));
            stash.Goods.Clear();
            stash.AverageCost.Clear();
            state.Stats.StashRaids++;
            state.Stats.GoodsSeized += units;
            state.Stats.GoodsSeizedValue += value;
            messages.Add($"Police raided your stash in {GameCatalog.City(stash.CityId).Name}. {units} items worth {Money.Format(value)} — gone.");
        }

        return messages;
    }

    /// <summary>If you're overdue with a lender who works in this city, their enforcers might find you.</summary>
    public EnforcerVisit? MaybeEnforcers(GameState state, GameRules rules)
    {
        var loan = state.Loans
            .Where(l => IsOverdue(l, state.Day) && GameCatalog.Lender(l.LenderId).CityIds.Contains(state.CityId))
            .OrderByDescending(l => l.Owed)
            .FirstOrDefault();
        if (loan is null || random.NextDouble() >= rules.EnforcerVisitChance) return null;

        var lender = GameCatalog.Lender(loan.LenderId);
        state.Stats.EnforcerVisits++;
        var parts = new List<string>();

        var cashTaken = Math.Min(state.Cash, loan.Owed);
        if (cashTaken > 0)
        {
            state.Cash -= cashTaken;
            loan.Owed -= cashTaken;
            parts.Add($"empty your pockets ({Money.Format(cashTaken)})");
        }

        if (lender.TakesGoods && loan.Owed > 0 && state.UnitsHeld > 0)
        {
            var units = state.UnitsHeld;
            var value = EncounterService.CargoValue(state);
            state.Inventory.Clear();
            state.AverageCost.Clear();
            loan.Owed = Math.Max(0, loan.Owed - value / 2);
            parts.Add($"take your {units} items");
        }

        var damage = lender.MaxEnforcerDamage > 0
            ? EncounterService.Hurt(state, random.Next(lender.MinEnforcerDamage, lender.MaxEnforcerDamage + 1))
            : 0;
        if (damage > 0) parts.Add($"leave you with {damage} damage as a reminder");

        if (loan.Owed == 0)
        {
            loan.Status = LoanStatus.Repaid;
        }

        var did = parts.Count == 0 ? "find you broke and leave a warning" : string.Join(", ", parts);
        var left = loan.Owed > 0 ? $" You still owe {Money.Format(loan.Owed)}." : " The debt is settled — the hard way.";
        return new EnforcerVisit($"{lender.Name}'s enforcers catch up with you. They {did}.{left}", damage);
    }
}
