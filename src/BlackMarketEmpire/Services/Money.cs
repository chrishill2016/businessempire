namespace BlackMarketEmpire.Services;

public static class Money
{
    /// <summary>Formats pounds with the sign in front: £1,234 or -£1,234.</summary>
    public static string Format(long amount) => amount < 0 ? $"-£{-amount:N0}" : $"£{amount:N0}";
}
