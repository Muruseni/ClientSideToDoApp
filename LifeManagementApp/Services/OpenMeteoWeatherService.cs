using System.Net.Http.Json;
using System.Text.Json.Serialization;
using LifeManagementApp.Models;

namespace LifeManagementApp.Services;

public sealed class OpenMeteoWeatherService(HttpClient httpClient) : IWeatherService
{
    public async Task<WeatherSnapshot> GetCurrentAsync(string city, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(city)) throw new ArgumentException("Enter a city.", nameof(city));

        var location = await httpClient.GetFromJsonAsync<GeocodingResponse>(
            $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city.Trim())}&count=1&language=en&format=json",
            cancellationToken) ?? throw new InvalidOperationException("Location was not found.");
        var result = location.Results?.FirstOrDefault() ?? throw new InvalidOperationException("Location was not found.");

        var forecast = await httpClient.GetFromJsonAsync<ForecastResponse>(
            $"https://api.open-meteo.com/v1/forecast?latitude={result.Latitude}&longitude={result.Longitude}&current=temperature_2m,apparent_temperature,weather_code,wind_speed_10m&wind_speed_unit=ms&timezone=auto",
            cancellationToken) ?? throw new InvalidOperationException("Weather data was unavailable.");
        var current = forecast.Current ?? throw new InvalidOperationException("Weather data was unavailable.");

        return new WeatherSnapshot(
            result.Name ?? city.Trim(),
            current.Temperature,
            current.FeelsLike,
            current.WindSpeed,
            Describe(current.WeatherCode),
            current.Time);
    }

    private static string Describe(int code) => code switch
    {
        0 => "Clear sky",
        1 or 2 => "Mostly clear",
        3 => "Overcast",
        45 or 48 => "Foggy",
        51 or 53 or 55 or 56 or 57 => "Drizzle",
        61 or 63 or 65 or 66 or 67 => "Rain",
        71 or 73 or 75 or 77 => "Snow",
        80 or 81 or 82 => "Rain showers",
        85 or 86 => "Snow showers",
        95 or 96 or 99 => "Thunderstorm",
        _ => "Unknown conditions"
    };

    private sealed class GeocodingResponse
    {
        [JsonPropertyName("results")] public List<Location>? Results { get; set; }
    }

    private sealed class Location
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("latitude")] public double Latitude { get; set; }
        [JsonPropertyName("longitude")] public double Longitude { get; set; }
    }

    private sealed class ForecastResponse
    {
        [JsonPropertyName("current")] public CurrentWeather? Current { get; set; }
    }

    private sealed class CurrentWeather
    {
        [JsonPropertyName("time")] public DateTime Time { get; set; }
        [JsonPropertyName("temperature_2m")] public double Temperature { get; set; }
        [JsonPropertyName("apparent_temperature")] public double FeelsLike { get; set; }
        [JsonPropertyName("weather_code")] public int WeatherCode { get; set; }
        [JsonPropertyName("wind_speed_10m")] public double WindSpeed { get; set; }
    }
}
