namespace BlackMarketEmpire.Models;

/// <summary>Something you can buy that changes the odds for the rest of the run.</summary>
public sealed record GearItem(
    string Id,
    string Name,
    string Icon,
    long Price,
    string Description,
    bool Consumable = false,
    /// <summary>Taken as evidence if you're arrested.</summary>
    bool SeizedOnArrest = false);

public sealed record Courier(
    string Id,
    string Name,
    /// <summary>Fee as a share of the cargo's value (what you paid).</summary>
    double FeeRate,
    /// <summary>Extra days on top of the overland/flight time.</summary>
    int ExtraDays,
    double SeizureChance,
    double TheftChance,
    string Description);

public enum ShipmentStatus { InTransit, Delivered, Seized, Stolen }

/// <summary>Goods sent ahead with a courier.</summary>
public sealed class Shipment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string GoodId { get; set; } = "";
    public int Quantity { get; set; }
    public long AverageCost { get; set; }
    public string FromCityId { get; set; } = "";
    public string ToCityId { get; set; } = "";
    public string CourierId { get; set; } = "";
    public int SentDay { get; set; }
    public int ArrivalDay { get; set; }
    public ShipmentStatus Status { get; set; } = ShipmentStatus.InTransit;
}
