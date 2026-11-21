namespace LifeManagementApp.Models;

public sealed record WeatherSnapshot(
    string City,
    double Temperature,
    double FeelsLike,
    double WindSpeed,
    string Description,
    DateTime LocalTime)
{
    // The course requirement treats 5 m/s as the inclusive kitesurf threshold.
    public bool KitesurfConditions => WindSpeed >= 5;
}
