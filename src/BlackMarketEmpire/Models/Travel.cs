namespace BlackMarketEmpire.Models;

public enum TravelMethod { Train, Car, Flight, Boat }

/// <summary>One way of getting to a city, priced and timed for the player's current situation.</summary>
public sealed record TravelOption(
    string CityId,
    TravelMethod Method,
    int Days,
    long Fare,
    double StopChance,
    bool Available,
    string? Reason);
