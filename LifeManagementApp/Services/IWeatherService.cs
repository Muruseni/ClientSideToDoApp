using LifeManagementApp.Models;

namespace LifeManagementApp.Services;

public interface IWeatherService
{
    Task<WeatherSnapshot> GetCurrentAsync(string city, CancellationToken cancellationToken = default);
}
