using LifeManagementApp.Models;

namespace LifeManagementApp.Services;

public interface IWeatherService
{
    // Implementations return normalized units and descriptions for either UI client.
    Task<WeatherSnapshot> GetCurrentAsync(string city, CancellationToken cancellationToken = default);
}
