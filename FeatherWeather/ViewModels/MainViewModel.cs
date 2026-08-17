using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FeatherWeather.Models;
using FeatherWeather.Services;
using System.Globalization;

namespace FeatherWeather.ViewModels;

internal sealed partial class MainViewModel(
    WeatherCache weatherCache,
    WeatherService weatherService) : ObservableObject, IDisposable
{
    private CancellationTokenSource? _refreshCts;
    private bool _initialized;

    [ObservableProperty]
    private string _city = "Санкт-Петербург";

    [ObservableProperty]
    private string _displayCity = "Погода";

    [ObservableProperty]
    private string _updatedText = "Загрузка…";

    [ObservableProperty]
    private string _weatherGlyph = "☁";

    [ObservableProperty]
    private string _temperatureText = "--°";

    [ObservableProperty]
    private string _conditionText = "Получаем прогноз";

    [ObservableProperty]
    private string _feelsLikeText = "Ощущается: --°";

    [ObservableProperty]
    private string _humidityText = "--%";

    [ObservableProperty]
    private string _windText = "-- м/с";

    [ObservableProperty]
    private string _pressureText = "-- мм";

    [ObservableProperty]
    private string _statusText = "Open-Meteo";

    [ObservableProperty]
    private IReadOnlyList<HourItem> _hourlyItems = [];

    [ObservableProperty]
    private IReadOnlyList<DayItem> _dailyItems = [];

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
            StatusText = "Обновляем…";

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
            StatusText = "Не удалось обновить: " + ex.Message;
        }
    }

    private void ApplyForecast(
        string city,
        string country,
        ForecastResponse forecast,
        DateTimeOffset updatedAt,
        bool fromCache)
    {
        CurrentWeather current = forecast.Current;
        DisplayCity = string.IsNullOrWhiteSpace(country) ? city : $"{city}, {country}";
        TemperatureText = $"{Math.Round(current.Temperature):0}°";
        WeatherGlyph = WeatherCode.Glyph(current.WeatherCode);
        ConditionText = WeatherCode.Description(current.WeatherCode);
        FeelsLikeText = $"Ощущается как {Math.Round(current.ApparentTemperature):0}°";
        HumidityText = $"{current.Humidity}%";
        WindText = $"{current.WindSpeed / 3.6:0.#} м/с";
        PressureText = $"{current.SurfacePressure * 0.750062:0} мм";
        UpdatedText = fromCache
            ? $"Сохранено {updatedAt:HH:mm} · обновляем в фоне"
            : $"Обновлено {updatedAt:HH:mm}";
        StatusText = "Данные: Open-Meteo";
        HourlyItems = BuildHourly(forecast.Hourly, forecast.Current.Time);
        DailyItems = BuildDaily(forecast.Daily);
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

            items[i] = new HourItem(time.ToString("HH:mm"), WeatherCode.Glyph(code), $"{Math.Round(temperature):0}°");
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
        CultureInfo ru = CultureInfo.GetCultureInfo("ru-RU");

        for (int i = 0; i < count; i++)
        {
            DateTime.TryParse(daily.Time[i], CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date);
            string day = i == 0 ? "Сегодня" : ru.TextInfo.ToTitleCase(date.ToString("ddd, d MMM", ru));
            int code = daily.WeatherCode[i];

            items[i] = new DayItem(
                day,
                WeatherCode.Glyph(code),
                WeatherCode.Description(code),
                $"{Math.Round(daily.MaxTemperature[i]):0}°  {Math.Round(daily.MinTemperature[i]):0}°");
        }

        return items;
    }

    public void Dispose()
    {
        _refreshCts?.Cancel();
        _refreshCts?.Dispose();
    }

    public sealed record HourItem(string Time, string Glyph, string Temperature);
    public sealed record DayItem(string Day, string Glyph, string Condition, string Range);
}

internal static class WeatherCode
{
    public static string Glyph(int code) => code switch
    {
        0 => "☀",
        1 or 2 => "⛅",
        3 => "☁",
        45 or 48 => "≋",
        51 or 53 or 55 or 56 or 57 => "☂",
        61 or 63 or 65 or 66 or 67 or 80 or 81 or 82 => "☔",
        71 or 73 or 75 or 77 or 85 or 86 => "❄",
        95 or 96 or 99 => "ϟ",
        _ => "☁"
    };

    public static string Description(int code) => code switch
    {
        0 => "Ясно",
        1 => "Преимущественно ясно",
        2 => "Переменная облачность",
        3 => "Пасмурно",
        45 or 48 => "Туман",
        51 or 53 or 55 => "Морось",
        56 or 57 => "Ледяная морось",
        61 or 63 or 65 => "Дождь",
        66 or 67 => "Ледяной дождь",
        71 or 73 or 75 or 77 => "Снег",
        80 or 81 or 82 => "Ливень",
        85 or 86 => "Снегопад",
        95 => "Гроза",
        96 or 99 => "Гроза с градом",
        _ => "Нет данных"
    };
}
