using BlackMarketEmpire.Models;

namespace BlackMarketEmpire.Services;

/// <summary>Works out how you can get to each city, how long it takes, what it costs and how risky it is.</summary>
public static class TravelService
{
    public static string Label(TravelMethod method) => method switch
    {
        TravelMethod.Train => "Train",
        TravelMethod.Car => "Car",
        TravelMethod.Flight => "Flight",
        TravelMethod.Boat => "Boat",
        _ => method.ToString(),
    };

    public static string Icon(TravelMethod method) => method switch
    {
        TravelMethod.Train => "🚆",
        TravelMethod.Car => "🚗",
        TravelMethod.Flight => "✈️",
        TravelMethod.Boat => "🚢",
        _ => "",
    };

    public static string RiskLabel(double stopChance) => stopChance switch
    {
        0 => "No risk",
        < 0.10 => "Low risk",
        < 0.25 => "Medium risk",
        _ => "High risk",
    };

    /// <summary>Days taken by a method, before checking whether the route exists.</summary>
    public static int DaysFor(TravelMethod method, int baseDays) => method switch
    {
        TravelMethod.Flight => Math.Max(1, baseDays - 1),
        TravelMethod.Boat => baseDays + 1,
        _ => baseDays,
    };

    public static long FarePerDay(TravelMethod method, GameRules rules) => method switch
    {
        TravelMethod.Train => rules.TrainFarePerDay,
        TravelMethod.Car => rules.CarFarePerDay,
        TravelMethod.Flight => rules.FlightFarePerDay,
        _ => rules.BoatFarePerDay,
    };

    public static double StopFactor(TravelMethod method, GameRules rules) => method switch
    {
        TravelMethod.Train => rules.TrainStopFactor,
        TravelMethod.Car => rules.CarStopFactor,
        TravelMethod.Flight => rules.FlightStopFactor,
        _ => rules.BoatStopFactor,
    };

    /// <summary>Flights and boats go through customs; trains and cars meet street police or roadblocks.</summary>
    public static bool IsCustoms(TravelMethod method) => method is TravelMethod.Flight or TravelMethod.Boat;

    public static IReadOnlyList<TravelOption> Options(GameState state, GameRules rules, string toCityId) =>
        Enum.GetValues<TravelMethod>().Select(m => Option(state, rules, toCityId, m)).ToList();

    public static TravelOption Option(GameState state, GameRules rules, string toCityId, TravelMethod method)
    {
        var from = state.CityId;
        var baseDays = GameCatalog.DaysBetween(from, toCityId);
        var days = DaysFor(method, baseDays);
        var fare = FarePerDay(method, rules) * baseDays;
        var stop = EncounterService.StopChance(state, rules, method, toCityId);

        string? reason = method switch
        {
            TravelMethod.Train or TravelMethod.Car when !GameCatalog.HasOverlandRoute(from, toCityId) => "No road or rail route",
            TravelMethod.Boat when !GameCatalog.IsPort(from) || !GameCatalog.IsPort(toCityId) => "Needs a port at both ends",
            _ when state.Day + days > rules.TotalDays => "Not enough days left",
            _ when fare > state.Cash => "Can't afford the fare",
            _ => null,
        };

        return new TravelOption(toCityId, method, days, fare, stop, reason is null, reason);
    }
}
