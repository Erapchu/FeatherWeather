using System.Globalization;
using System.Windows;
using System.Windows.Input;
using FeatherWeather.Models;
using FeatherWeather.Services;

namespace FeatherWeather;

public partial class MainWindow : Window
{
    private CancellationTokenSource? _refreshCts;
    private bool _firstShown = true;

    public MainWindow()
    {
        InitializeComponent();

        // Show cached data synchronously. It is tiny and makes repeated launches feel instant.
        CachedWeather? cached = WeatherCache.TryLoad();
        if (cached is not null)
        {
            CityBox.Text = cached.City;
            Render(cached.City, cached.Country, cached.Forecast, cached.SavedAt, fromCache: true);
        }

        ContentRendered += OnContentRendered;
        Closed += (_, _) => _refreshCts?.Cancel();
    }

    private void OnContentRendered(object? sender, EventArgs e)
    {
        if (!_firstShown)
            return;

        _firstShown = false;
        _ = RefreshAsync(showLoadingText: false);
    }

    private async void RefreshButton_OnClick(object sender, RoutedEventArgs e) =>
        await RefreshAsync(showLoadingText: true);

    private async void CityBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            await RefreshAsync(showLoadingText: true);
    }

    private async Task RefreshAsync(bool showLoadingText)
    {
        string city = CityBox.Text.Trim();
        if (city.Length < 2)
            return;

        _refreshCts?.Cancel();
        _refreshCts?.Dispose();
        _refreshCts = new CancellationTokenSource();

        if (showLoadingText)
            StatusText.Text = "Обновляем…";

        try
        {
            var (place, forecast) = await WeatherService.GetWeatherAsync(city, _refreshCts.Token);
            DateTimeOffset now = DateTimeOffset.Now;

            Render(place.Name, place.Country, forecast, now, fromCache: false);
            CityBox.Text = place.Name;

            WeatherCache.Save(new CachedWeather
            {
                City = place.Name,
                Country = place.Country,
                SavedAt = now,
                Forecast = forecast
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            StatusText.Text = "Не удалось обновить: " + ex.Message;
        }
    }

    private void Render(
        string city,
        string country,
        ForecastResponse forecast,
        DateTimeOffset updatedAt,
        bool fromCache)
    {
        CurrentWeather current = forecast.Current;
        CityText.Text = string.IsNullOrWhiteSpace(country) ? city : $"{city}, {country}";
        TemperatureText.Text = $"{Math.Round(current.Temperature):0}°";
        WeatherGlyph.Text = WeatherCode.Glyph(current.WeatherCode);
        ConditionText.Text = WeatherCode.Description(current.WeatherCode);
        FeelsLikeText.Text = $"Ощущается как {Math.Round(current.ApparentTemperature):0}°";
        HumidityText.Text = $"{current.Humidity}%";
        WindText.Text = $"{current.WindSpeed / 3.6:0.#} м/с";
        PressureText.Text = $"{current.SurfacePressure * 0.750062:0} мм";
        UpdatedText.Text = fromCache
            ? $"Сохранено {updatedAt:HH:mm} · обновляем в фоне"
            : $"Обновлено {updatedAt:HH:mm}";
        StatusText.Text = "Данные: Open-Meteo";

        HourlyItems.ItemsSource = BuildHourly(forecast.Hourly, forecast.Current.Time);
        DailyItems.ItemsSource = BuildDaily(forecast.Daily);
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
            double temp = index < hourly.Temperature.Length ? hourly.Temperature[index] : 0;

            items[i] = new HourItem(time.ToString("HH:mm"), WeatherCode.Glyph(code), $"{Math.Round(temp):0}°");
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

    private sealed record HourItem(string Time, string Glyph, string Temperature);
    private sealed record DayItem(string Day, string Glyph, string Condition, string Range);
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
