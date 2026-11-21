namespace LifeManagementApp.Models;

public sealed record WeatherSnapshot(
    string City,
    double Temperature,
    double FeelsLike,
    double WindSpeed,
    string Description,
    DateTime LocalTime);
