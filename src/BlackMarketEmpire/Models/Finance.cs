namespace BlackMarketEmpire.Models;

public enum LenderKind { LoanShark, Bank, Underground }

/// <summary>Someone who'll lend you money, on their terms.</summary>
public sealed record Lender(
    string Id,
    string Name,
    LenderKind Kind,
    IReadOnlyList<string> CityIds,
    /// <summary>Daily interest at an average (3★) reputation, e.g. 0.06 = 6% a day.</summary>
    double BaseRate,
    /// <summary>Most they'll lend at an average reputation.</summary>
    long BaseLimit,
    /// <summary>Days until the loan is due.</summary>
    int TermDays,
    /// <summary>Days they'll wait after the due date before counting it as overdue.</summary>
    int GraceDays,
    /// <summary>Damage range when their enforcers find you overdue.</summary>
    int MinEnforcerDamage,
    int MaxEnforcerDamage,
    /// <summary>Their enforcers also take goods.</summary>
    bool TakesGoods,
    string Blurb);

public enum LoanStatus { Active, Repaid }

public sealed class Loan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string LenderId { get; set; } = "";
    public long Principal { get; set; }
    public long Owed { get; set; }
    /// <summary>Daily rate locked in when you borrowed.</summary>
    public double Rate { get; set; }
    public int TakenDay { get; set; }
    public int DueDay { get; set; }
    public LoanStatus Status { get; set; } = LoanStatus.Active;
    /// <summary>Set once the loan has gone overdue (so the reputation hit only happens once).</summary>
    public bool MarkedOverdue { get; set; }
}

public enum StashTier { None, Lockup, Warehouse }

/// <summary>A rented hiding place for goods in one city.</summary>
public sealed class Stash
{
    public string CityId { get; set; } = "";
    public StashTier Tier { get; set; }
    public Dictionary<string, int> Goods { get; set; } = [];
    public Dictionary<string, long> AverageCost { get; set; } = [];

    public int Units => Goods.Values.Sum();
}

public enum DangerLevel { Low, Medium, High }
