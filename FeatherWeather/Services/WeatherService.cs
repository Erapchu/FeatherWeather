using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using FeatherWeather.Models;

namespace FeatherWeather.Services;

internal static class WeatherService
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(8)
    };

    public static async Task<(GeoResult Place, ForecastResponse Forecast)> GetWeatherAsync(
        string city,
        CancellationToken cancellationToken)
    {
        GeoResult place = await FindCityAsync(city, cancellationToken).ConfigureAwait(false);
        ForecastResponse forecast = await GetForecastAsync(place, cancellationToken).ConfigureAwait(false);
        return (place, forecast);
    }

    private static async Task<GeoResult> FindCityAsync(string city, CancellationToken cancellationToken)
    {
        string url = "https://geocoding-api.open-meteo.com/v1/search?name=" +
                     Uri.EscapeDataString(city) +
                     "&count=1&language=ru&format=json";

        await using Stream stream = await Http.GetStreamAsync(url, cancellationToken).ConfigureAwait(false);
        GeocodingResponse? response = await JsonSerializer.DeserializeAsync(
            stream,
            WeatherJsonContext.Default.GeocodingResponse,
            cancellationToken).ConfigureAwait(false);

        return response?.Results?.FirstOrDefault()
               ?? throw new InvalidOperationException("Город не найден.");
    }

    private static async Task<ForecastResponse> GetForecastAsync(GeoResult place, CancellationToken cancellationToken)
    {
        string lat = place.Latitude.ToString(CultureInfo.InvariantCulture);
        string lon = place.Longitude.ToString(CultureInfo.InvariantCulture);

        string url = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}" +
                     "&current=temperature_2m,apparent_temperature,relative_humidity_2m,weather_code,wind_speed_10m,surface_pressure" +
                     "&hourly=temperature_2m,weather_code" +
                     "&daily=weather_code,temperature_2m_max,temperature_2m_min" +
                     "&forecast_days=7&timezone=auto";

        await using Stream stream = await Http.GetStreamAsync(url, cancellationToken).ConfigureAwait(false);
        return await JsonSerializer.DeserializeAsync(
                   stream,
                   WeatherJsonContext.Default.ForecastResponse,
                   cancellationToken).ConfigureAwait(false)
               ?? throw new InvalidOperationException("Сервис погоды вернул пустой ответ.");
    }
}
