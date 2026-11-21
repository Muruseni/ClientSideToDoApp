namespace LifeManagementApp.Models;

public sealed record WeatherSnapshot(
    string City,
    double Temperature,
    double FeelsLike,
    double WindSpeed,
    string Description,
    DateTime LocalTime)
{
    // The course brief says the alert is for wind over 5 m/s, so exactly 5 m/s
    // remains below the alert threshold.
    public bool KitesurfConditions => WindSpeed > 5;

    public string KitesurfAlert => KitesurfConditions
        ? "🪁 Kitesurf weather!"
        : "Kitesurf alert: wind is 5 m/s or lower.";
}
