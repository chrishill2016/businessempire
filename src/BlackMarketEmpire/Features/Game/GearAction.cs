namespace BlackMarketEmpire.Features.Game;

public sealed record GearAction(string GearId, bool Use);

public sealed record ShipOrder(string GoodId, int Quantity, string ToCityId, string CourierId);
