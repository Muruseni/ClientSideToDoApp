using System.Net;
using System.Net.Http.Json;
using LifeManagementApp.Services;

namespace LifeManagementApp.Tests;

public class WeatherServiceTests
{
    // A fake HttpMessageHandler makes the REST test deterministic and offline.
    [Fact]
    public async Task WeatherServiceMapsForecastAndKitesurfThreshold()
    {
        // Stub both upstream calls so this test proves parsing and units without internet access.
        var handler = new WeatherHandler();
        using var client = new HttpClient(handler);
        var weather = await new OpenMeteoWeatherService(client).GetCurrentAsync("Kyiv");

        Assert.Equal("Kyiv", weather.City);
        Assert.Equal(5, weather.WindSpeed);
        Assert.False(weather.KitesurfConditions);
        Assert.Contains("wind_speed_unit=ms", handler.ForecastRequest!.RequestUri!.Query);
        Assert.Equal("Clear sky", weather.Description);
    }

    private sealed class WeatherHandler : HttpMessageHandler
    {
        public HttpRequestMessage? ForecastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri!.Host.StartsWith("api.open-meteo", StringComparison.Ordinal)) ForecastRequest = request;
            var json = request.RequestUri!.Host.StartsWith("geocoding", StringComparison.Ordinal)
                ? "{\"results\":[{\"name\":\"Kyiv\",\"latitude\":50.45,\"longitude\":30.52}]}"
                : "{\"current\":{\"time\":\"2026-11-21T12:00\",\"temperature_2m\":12,\"apparent_temperature\":10,\"weather_code\":0,\"wind_speed_10m\":5}}";

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(System.Text.Json.JsonSerializer.Deserialize<object>(json))
            });
        }
    }
}
