using BlackMarketEmpire.Models;

namespace BlackMarketEmpire.Services;

public sealed record ShippingQuote(long Fee, int Days, int ArrivalDay);

/// <summary>Sends goods ahead with a courier so you don't have to carry them past the police.</summary>
public sealed class ShippingService(IRandomSource random)
{
    public const long MinimumFee = 50;

    public static ShippingQuote Quote(GameState state, string goodId, int quantity, string toCityId, Courier courier)
    {
        var baseDays = GameCatalog.DaysBetween(state.CityId, toCityId);
        var days = TravelService.DaysFor(TravelMethod.Flight, baseDays) + courier.ExtraDays;
        var value = quantity * state.AverageCost.GetValueOrDefault(goodId);
        var fee = Math.Max(MinimumFee, (long)Math.Ceiling(value * courier.FeeRate));
        return new ShippingQuote(fee, days, state.Day + days);
    }

    /// <summary>Settles shipments due today. Returns messages for the activity log.</summary>
    public IReadOnlyList<string> OnNewDay(GameState state)
    {
        var messages = new List<string>();
        foreach (var s in state.Shipments.Where(s => s.Status == ShipmentStatus.InTransit && s.ArrivalDay <= state.Day))
        {
            var courier = GameCatalog.Courier(s.CourierId);
            var what = $"{s.Quantity} × {GameCatalog.Good(s.GoodId).Name}";
            var city = GameCatalog.City(s.ToCityId).Name;
            var roll = random.NextDouble();
            if (roll < courier.SeizureChance)
            {
                s.Status = ShipmentStatus.Seized;
                state.Stats.ShipmentsLost++;
                messages.Add($"Customs in {city} seized your shipment of {what}.");
            }
            else if (roll < courier.SeizureChance + courier.TheftChance)
            {
                s.Status = ShipmentStatus.Stolen;
                state.Stats.ShipmentsLost++;
                messages.Add($"Your courier vanished with {what}. It's never arriving in {city}.");
            }
            else
            {
                s.Status = ShipmentStatus.Delivered;
                messages.Add($"Your shipment of {what} is waiting for you in {city}.");
            }
        }

        // Forget lost shipments after a few days so the list stays short.
        state.Shipments.RemoveAll(s => s.Status is ShipmentStatus.Seized or ShipmentStatus.Stolen && state.Day - s.ArrivalDay > 3);
        return messages;
    }
}
