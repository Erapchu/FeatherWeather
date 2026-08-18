using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FeatherWeather.Models;
using FeatherWeather.Resources;
using FeatherWeather.Services;
using System.Globalization;

namespace FeatherWeather.ViewModels;

internal sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly WeatherCache weatherCache;
    private readonly WeatherService weatherService;
    private CancellationTokenSource? _refreshCts;
    private ForecastSnapshot? _lastForecast;
    private bool _initialized;

    public MainViewModel(WeatherCache weatherCache, WeatherService weatherService)
    {
        this.weatherCache = weatherCache;
        this.weatherService = weatherService;
        LocalizationManager.Instance.CultureChanged += OnCultureChanged;
    }

    [ObservableProperty]
    private string _city = Strings.DefaultCity;

    [ObservableProperty]
    public partial string DisplayCity { get; set; } = Strings.DefaultDisplayCity;

    [ObservableProperty]
    public partial string UpdatedText { get; set; } = Strings.Loading;

    [ObservableProperty]
    public partial string WeatherGlyph { get; set; } = Strings.WeatherGlyphCloudy;

    [ObservableProperty]
    public partial string TemperatureText { get; set; } = Strings.TemperaturePlaceholder;

    [ObservableProperty]
    public partial string ConditionText { get; set; } = Strings.FetchingForecast;

    [ObservableProperty]
    public partial string FeelsLikeText { get; set; } = Strings.FeelsLikePlaceholder;

    [ObservableProperty]
    public partial string HumidityText { get; set; } = Strings.HumidityPlaceholder;

    [ObservableProperty]
    public partial string WindText { get; set; } = Strings.WindPlaceholder;

    [ObservableProperty]
    public partial string PressureText { get; set; } = Strings.PressurePlaceholder;

    [ObservableProperty]
    public partial string StatusText { get; set; } = Strings.DataSource;

    [ObservableProperty]
    public partial IReadOnlyList<HourItem> HourlyItems { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<DayItem> DailyItems { get; set; } = [];

    [ObservableProperty]
    private bool _isSettingsVisible;

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        _initialized = true;

        CachedWeather? cached = await Task.Run(weatherCache.TryLoad);
        if (cached is not null)
        {
            City = cached.City;
            ApplyForecast(cached.City, cached.Country, cached.Forecast, cached.SavedAt, fromCache: true);
        }

        await RefreshCoreAsync(showLoadingText: false);
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task RefreshAsync() => RefreshCoreAsync(showLoadingText: true);

    [RelayCommand]
    private void ShowSettings() => IsSettingsVisible = true;

    [RelayCommand]
    private void ShowWeather() => IsSettingsVisible = false;

    internal void CancelPendingRefresh() => _refreshCts?.Cancel();

    private async Task RefreshCoreAsync(bool showLoadingText)
    {
        string city = City.Trim();
        if (city.Length < 2)
            return;

        _refreshCts?.Cancel();
        _refreshCts?.Dispose();
        _refreshCts = new CancellationTokenSource();
        CancellationToken cancellationToken = _refreshCts.Token;

        if (showLoadingText)
            StatusText = Strings.Updating;

        try
        {
            var (place, forecast) = await weatherService.GetWeatherAsync(city, cancellationToken);
            DateTimeOffset now = DateTimeOffset.Now;

            ApplyForecast(place.Name, place.Country, forecast, now, fromCache: false);
            City = place.Name;

            var cachedWeather = new CachedWeather
            {
                City = place.Name,
                Country = place.Country,
                SavedAt = now,
                Forecast = forecast
            };

            await Task.Run(() => weatherCache.Save(cachedWeather), cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            StatusText = string.Format(CultureInfo.CurrentCulture, Strings.UpdateFailedFormat, ex.Message);
        }
    }

    private void ApplyForecast(
        string city,
        string country,
        ForecastResponse forecast,
        DateTimeOffset updatedAt,
        bool fromCache)
    {
        _lastForecast = new ForecastSnapshot(city, country, forecast, updatedAt, fromCache);
        CurrentWeather current = forecast.Current;
        DisplayCity = string.IsNullOrWhiteSpace(country)
            ? city
            : string.Format(CultureInfo.CurrentCulture, Strings.LocationFormat, city, country);
        TemperatureText = string.Format(CultureInfo.CurrentCulture, Strings.TemperatureFormat, Math.Round(current.Temperature));
        WeatherGlyph = WeatherCode.Glyph(current.WeatherCode);
        ConditionText = WeatherCode.Description(current.WeatherCode);
        FeelsLikeText = string.Format(CultureInfo.CurrentCulture, Strings.FeelsLikeFormat, Math.Round(current.ApparentTemperature));
        HumidityText = string.Format(CultureInfo.CurrentCulture, Strings.HumidityFormat, current.Humidity);
        WindText = string.Format(CultureInfo.CurrentCulture, Strings.WindSpeedFormat, current.WindSpeed / 3.6);
        PressureText = string.Format(CultureInfo.CurrentCulture, Strings.PressureFormat, current.SurfacePressure * 0.750062);
        UpdatedText = fromCache
            ? string.Format(CultureInfo.CurrentCulture, Strings.CachedUpdateFormat, updatedAt)
            : string.Format(CultureInfo.CurrentCulture, Strings.UpdatedFormat, updatedAt);
        StatusText = Strings.DataSource;
        HourlyItems = BuildHourly(forecast.Hourly, forecast.Current.Time);
        DailyItems = BuildDaily(forecast.Daily);
    }

    private void OnCultureChanged(object? sender, EventArgs e)
    {
        if (_lastForecast is { } snapshot)
        {
            ApplyForecast(
                snapshot.City,
                snapshot.Country,
                snapshot.Forecast,
                snapshot.UpdatedAt,
                snapshot.FromCache);
            return;
        }

        DisplayCity = Strings.DefaultDisplayCity;
        UpdatedText = Strings.Loading;
        TemperatureText = Strings.TemperaturePlaceholder;
        ConditionText = Strings.FetchingForecast;
        FeelsLikeText = Strings.FeelsLikePlaceholder;
        HumidityText = Strings.HumidityPlaceholder;
        WindText = Strings.WindPlaceholder;
        PressureText = Strings.PressurePlaceholder;
        StatusText = Strings.DataSource;
    }

    private static HourItem[] BuildHourly(HourlyWeather hourly, string currentTime)
    {
        if (hourly.Time.Length == 0)
            return [];

        DateTime.TryParse(currentTime, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime now);
        if (now == default)
            now = DateTime.Now;

        int start = 0;
        for (int i = 0; i < hourly.Time.Length; i++)
        {
            if (DateTime.TryParse(hourly.Time[i], CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime time) &&
                time >= now.AddMinutes(-30))
            {
                start = i;
                break;
            }
        }

        int count = Math.Min(8, hourly.Time.Length - start);
        var items = new HourItem[count];
        for (int i = 0; i < count; i++)
        {
            int index = start + i;
            DateTime.TryParse(hourly.Time[index], CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime time);
            int code = index < hourly.WeatherCode.Length ? hourly.WeatherCode[index] : 0;
            double temperature = index < hourly.Temperature.Length ? hourly.Temperature[index] : 0;

            items[i] = new HourItem(
                time.ToString(Strings.ShortTimeFormat, CultureInfo.CurrentCulture),
                WeatherCode.Glyph(code),
                string.Format(CultureInfo.CurrentCulture, Strings.TemperatureFormat, Math.Round(temperature)));
        }

        return items;
    }

    private static DayItem[] BuildDaily(DailyWeather daily)
    {
        int count = new[]
        {
            daily.Time.Length,
            daily.WeatherCode.Length,
            daily.MaxTemperature.Length,
            daily.MinTemperature.Length
        }.Min();

        var items = new DayItem[count];
        CultureInfo culture = CultureInfo.CurrentCulture;

        for (int i = 0; i < count; i++)
        {
            DateTime.TryParse(daily.Time[i], CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date);
            string day = i == 0 ? Strings.Today : culture.TextInfo.ToTitleCase(date.ToString(Strings.ShortDayFormat, culture));
            int code = daily.WeatherCode[i];

            items[i] = new DayItem(
                day,
                WeatherCode.Glyph(code),
                WeatherCode.Description(code),
                string.Format(
                    CultureInfo.CurrentCulture,
                    Strings.TemperatureRangeFormat,
                    Math.Round(daily.MaxTemperature[i]),
                    Math.Round(daily.MinTemperature[i])));
        }

        return items;
    }

    public void Dispose()
    {
        LocalizationManager.Instance.CultureChanged -= OnCultureChanged;
        _refreshCts?.Cancel();
        _refreshCts?.Dispose();
    }

    private sealed record ForecastSnapshot(
        string City,
        string Country,
        ForecastResponse Forecast,
        DateTimeOffset UpdatedAt,
        bool FromCache);

    public sealed record HourItem(string Time, string Glyph, string Temperature);
    public sealed record DayItem(string Day, string Glyph, string Condition, string Range);
}

internal static class WeatherCode
{
    public static string Glyph(int code) => code switch
    {
        0 => Strings.WeatherGlyphClear,
        1 or 2 => Strings.WeatherGlyphPartlyCloudy,
        3 => Strings.WeatherGlyphCloudy,
        45 or 48 => Strings.WeatherGlyphFog,
        51 or 53 or 55 or 56 or 57 => Strings.WeatherGlyphDrizzle,
        61 or 63 or 65 or 66 or 67 or 80 or 81 or 82 => Strings.WeatherGlyphRain,
        71 or 73 or 75 or 77 or 85 or 86 => Strings.WeatherGlyphSnow,
        95 or 96 or 99 => Strings.WeatherGlyphThunderstorm,
        _ => Strings.WeatherGlyphCloudy
    };

    public static string Description(int code) => code switch
    {
        0 => Strings.WeatherClearSky,
        1 => Strings.WeatherMainlyClear,
        2 => Strings.WeatherPartlyCloudy,
        3 => Strings.WeatherOvercast,
        45 or 48 => Strings.WeatherFog,
        51 or 53 or 55 => Strings.WeatherDrizzle,
        56 or 57 => Strings.WeatherFreezingDrizzle,
        61 or 63 or 65 => Strings.WeatherRain,
        66 or 67 => Strings.WeatherFreezingRain,
        71 or 73 or 75 or 77 => Strings.WeatherSnow,
        80 or 81 or 82 => Strings.WeatherRainShowers,
        85 or 86 => Strings.WeatherSnowShowers,
        95 => Strings.WeatherThunderstorm,
        96 or 99 => Strings.WeatherThunderstormWithHail,
        _ => Strings.WeatherUnknown
    };
}
