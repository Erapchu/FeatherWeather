using System.Text.Json.Serialization;

namespace FeatherWeather.Models;

internal sealed class GeocodingResponse
{
    [JsonPropertyName("results")]
    public List<GeoResult>? Results { get; init; }
}

public sealed class GeoResult
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; init; } = string.Empty;

    [JsonPropertyName("admin1")]
    public string Admin1 { get; init; } = string.Empty;

    [JsonPropertyName("latitude")]
    public double Latitude { get; init; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; init; }

    [JsonPropertyName("timezone")]
    public string Timezone { get; init; } = "auto";

    [JsonIgnore]
    public string LocationDetails => string.Join(", ", new[] { Admin1, Country }
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Distinct(StringComparer.CurrentCultureIgnoreCase));

    [JsonIgnore]
    public string DisplayName => string.IsNullOrWhiteSpace(LocationDetails)
        ? Name
        : $"{Name}, {LocationDetails}";
}

internal sealed class ForecastResponse
{
    [JsonPropertyName("current")]
    public CurrentWeather Current { get; init; } = new();

    [JsonPropertyName("hourly")]
    public HourlyWeather Hourly { get; init; } = new();

    [JsonPropertyName("daily")]
    public DailyWeather Daily { get; init; } = new();
}

internal sealed class CurrentWeather
{
    [JsonPropertyName("time")]
    public string Time { get; init; } = string.Empty;

    [JsonPropertyName("temperature_2m")]
    public double Temperature { get; init; }

    [JsonPropertyName("apparent_temperature")]
    public double ApparentTemperature { get; init; }

    [JsonPropertyName("relative_humidity_2m")]
    public int Humidity { get; init; }

    [JsonPropertyName("weather_code")]
    public int WeatherCode { get; init; }

    [JsonPropertyName("wind_speed_10m")]
    public double WindSpeed { get; init; }

    [JsonPropertyName("surface_pressure")]
    public double SurfacePressure { get; init; }
}

internal sealed class HourlyWeather
{
    [JsonPropertyName("time")]
    public string[] Time { get; init; } = [];

    [JsonPropertyName("temperature_2m")]
    public double[] Temperature { get; init; } = [];

    [JsonPropertyName("weather_code")]
    public int[] WeatherCode { get; init; } = [];
}

internal sealed class DailyWeather
{
    [JsonPropertyName("time")]
    public string[] Time { get; init; } = [];

    [JsonPropertyName("weather_code")]
    public int[] WeatherCode { get; init; } = [];

    [JsonPropertyName("temperature_2m_max")]
    public double[] MaxTemperature { get; init; } = [];

    [JsonPropertyName("temperature_2m_min")]
    public double[] MinTemperature { get; init; } = [];
}

internal sealed class CachedWeather
{
    public string City { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public DateTimeOffset SavedAt { get; init; }
    public ForecastResponse Forecast { get; init; } = new();
}

[JsonSerializable(typeof(GeocodingResponse))]
[JsonSerializable(typeof(ForecastResponse))]
[JsonSerializable(typeof(CachedWeather))]
internal partial class WeatherJsonContext : JsonSerializerContext
{
}
