using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FeatherWeather.Models;
using FeatherWeather.Resources;
using FeatherWeather.Services;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Collections.ObjectModel;

namespace FeatherWeather.ViewModels;

internal sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly WeatherCache weatherCache;
    private readonly WeatherService weatherService;
    private readonly SettingsService settingsService;
    private readonly Dictionary<(double Latitude, double Longitude), ForecastSnapshot> _forecasts = [];
    private CancellationTokenSource? _refreshCts;
    private CancellationTokenSource? _citySearchCts;
    private ForecastSnapshot? _lastForecast;
    private GeoResult? _selectedPlace;
    private GeoResult? _currentPlace;
    private string _currentCity;
    private bool _isApplyingSuggestion;
    private bool _initialized;

    [ObservableProperty]
    public partial string City { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<GeoResult> CitySuggestions { get; set; } = [];

    [ObservableProperty]
    public partial GeoResult? SelectedCitySuggestion { get; set; }

    [ObservableProperty]
    public partial bool IsCitySuggestionsOpen { get; set; }

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
    public partial bool IsSettingsVisible { get; set; }

    public ObservableCollection<SavedCityItem> SavedCities { get; } = [];

    public MainViewModel(
        WeatherCache weatherCache,
        WeatherService weatherService,
        SettingsService settingsService)
    {
        this.weatherCache = weatherCache;
        this.weatherService = weatherService;
        this.settingsService = settingsService;
        _currentPlace = settingsService.CurrentPlace;
        _currentCity = string.IsNullOrWhiteSpace(settingsService.City)
            ? Strings.DefaultCity
            : settingsService.City;
        foreach (GeoResult place in settingsService.SavedCities)
            SavedCities.Add(new SavedCityItem(place));
        UpdateSavedCitySelection();
        LocalizationManager.Instance.CultureChanged += OnCultureChanged;
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        _initialized = true;

        CachedWeather[] cached = await weatherCache.TryLoadAsync();
        foreach (CachedWeather entry in cached.OfType<CachedWeather>())
        {
            if (entry.Forecast is null)
                continue;
            var key = (entry.Latitude, entry.Longitude);
            if (SavedCities.Any(city => city.Place.Latitude == entry.Latitude &&
                                        city.Place.Longitude == entry.Longitude))
                _forecasts.TryAdd(key,
                    new ForecastSnapshot(entry.City, entry.Country, entry.Forecast, entry.SavedAt, true));
        }

        UpdateSavedCityWeather();
        if (_currentPlace is not null &&
            _forecasts.TryGetValue((_currentPlace.Latitude, _currentPlace.Longitude), out var snapshot))
            ShowForecast(snapshot);

        // Discard entries for removed cities and the old format without coordinates.
        await SaveForecastsAsync();
        if (_lastForecast is null && _refreshCts is null)
            await RefreshCoreAsync(showLoadingText: true, placeOverride: _currentPlace);
    }

    private Task SaveForecastsAsync()
    {
        CachedWeather[] entries = SavedCities
            .Where(city => _forecasts.ContainsKey((city.Place.Latitude, city.Place.Longitude)))
            .Select(city =>
            {
                GeoResult place = city.Place;
                ForecastSnapshot snapshot = _forecasts[(place.Latitude, place.Longitude)];
                return new CachedWeather
                {
                    Latitude = place.Latitude,
                    Longitude = place.Longitude,
                    City = snapshot.City,
                    Country = snapshot.Country,
                    SavedAt = snapshot.UpdatedAt,
                    Forecast = snapshot.Forecast
                };
            }).ToArray();
        return weatherCache.SaveAsync(entries);
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task RefreshAsync() => RefreshCoreAsync(showLoadingText: true, placeOverride: _currentPlace);

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task SearchCityAsync() => RefreshCoreAsync(showLoadingText: true, saveCity: true);

    [RelayCommand]
    private void ShowSettings() => IsSettingsVisible = true;

    [RelayCommand]
    private void ShowWeather() => IsSettingsVisible = false;

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task SelectSavedCityAsync(SavedCityItem city)
    {
        City = string.Empty;
        SelectPlace(city.Place, saveCity: false);
        return _forecasts.ContainsKey((city.Place.Latitude, city.Place.Longitude))
            ? Task.CompletedTask
            : RefreshCoreAsync(showLoadingText: true, placeOverride: city.Place);
    }

    private void SelectPlace(GeoResult place, bool saveCity)
    {
        _refreshCts?.Cancel();
        IsSettingsVisible = false;
        _currentPlace = place;
        _currentCity = place.Name;
        if (saveCity && !SavedCities.Any(city => SettingsService.IsSamePlace(city.Place, place)))
            SavedCities.Add(new SavedCityItem(place));
        settingsService.SetCurrentPlace(place, saveCity);
        UpdateSavedCitySelection();
        var key = (place.Latitude, place.Longitude);
        if (_forecasts.TryGetValue(key, out var snapshot))
            ShowForecast(snapshot);
        else
        {
            _lastForecast = null;
            HourlyItems = [];
            DailyItems = [];
            OnCultureChanged(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    private async Task RemoveSavedCityAsync(SavedCityItem city)
    {
        settingsService.RemoveSavedCity(city.Place);
        SavedCities.Remove(city);
        var key = (city.Place.Latitude, city.Place.Longitude);
        _forecasts.Remove(key);
        if (_currentPlace is not null && SettingsService.IsSamePlace(_currentPlace, city.Place))
        {
            if (SavedCities.Count > 0)
            {
                City = string.Empty;
                SelectPlace(SavedCities[0].Place, saveCity: false);
            }
            else
            {
                _refreshCts?.Cancel();
                _lastForecast = null;
                HourlyItems = [];
                DailyItems = [];
                OnCultureChanged(this, EventArgs.Empty);
            }
        }
        await SaveForecastsAsync();
        if (_currentPlace is not null && SavedCities.Any(city => SettingsService.IsSamePlace(city.Place, _currentPlace)) &&
            !_forecasts.ContainsKey((_currentPlace.Latitude, _currentPlace.Longitude)))
            await RefreshCoreAsync(showLoadingText: true, placeOverride: _currentPlace);
    }

    private void UpdateSavedCitySelection()
    {
        foreach (SavedCityItem city in SavedCities)
            city.IsCurrent = _currentPlace is not null && SettingsService.IsSamePlace(city.Place, _currentPlace);
        UpdateSavedCityWeather();
    }

    private void UpdateSavedCityWeather()
    {
        foreach (SavedCityItem city in SavedCities)
        {
            if (_forecasts.TryGetValue((city.Place.Latitude, city.Place.Longitude), out var snapshot))
            {
                CurrentWeather current = snapshot.Forecast.Current;
                city.WeatherGlyph = WeatherCode.Glyph(current.WeatherCode);
                city.TemperatureText = string.Format(CultureInfo.CurrentCulture,
                    Strings.TemperatureFormat, Math.Round(current.Temperature));
                city.ConditionText = WeatherCode.Description(current.WeatherCode);
            }
        }
    }

    internal void CancelPendingRefresh() => _refreshCts?.Cancel();

    partial void OnCityChanged(string value)
    {
        if (_isApplyingSuggestion)
            return;

        _selectedPlace = null;
        SelectedCitySuggestion = null;
        QueueCitySearch(value);
    }

    partial void OnSelectedCitySuggestionChanged(GeoResult? value)
    {
        if (value is null)
            return;

        _citySearchCts?.Cancel();
        _selectedPlace = value;
        _isApplyingSuggestion = true;
        City = value.Name;
        _isApplyingSuggestion = false;
        CitySuggestions = [];
        IsCitySuggestionsOpen = false;
        SelectPlace(value, saveCity: true);
        _ = RefreshCoreAsync(showLoadingText: true, placeOverride: value);
    }

    private void QueueCitySearch(string query)
    {
        _citySearchCts?.Cancel();
        _citySearchCts?.Dispose();
        _citySearchCts = null;

        query = query.Trim();
        if (query.Length < 2)
        {
            CitySuggestions = [];
            IsCitySuggestionsOpen = false;
            return;
        }

        _citySearchCts = new CancellationTokenSource();
        _ = SearchCitySuggestionsAsync(query, _citySearchCts.Token);
    }

    private async Task SearchCitySuggestionsAsync(string query, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(100, cancellationToken);
            IReadOnlyList<GeoResult> suggestions =
                await weatherService.SearchCitiesAsync(query, 8, cancellationToken);

            if (cancellationToken.IsCancellationRequested ||
                !string.Equals(query, City.Trim(), StringComparison.CurrentCultureIgnoreCase))
                return;

            CitySuggestions = suggestions;
            IsCitySuggestionsOpen = suggestions.Count > 0;
        }
        catch (OperationCanceledException)
        {
        }
        catch (HttpRequestException)
        {
            CitySuggestions = [];
            IsCitySuggestionsOpen = false;
        }
        catch (JsonException)
        {
            CitySuggestions = [];
            IsCitySuggestionsOpen = false;
        }
    }

    private async Task RefreshCoreAsync(bool showLoadingText, GeoResult? placeOverride = null, bool saveCity = false)
    {
        string query = placeOverride?.Name ?? City.Trim();
        string city = query.Length == 0 ? _currentCity : query;
        if (city.Length < 2)
            return;

        _citySearchCts?.Cancel();
        IsCitySuggestionsOpen = false;

        _refreshCts?.Cancel();
        _refreshCts?.Dispose();
        _refreshCts = new CancellationTokenSource();
        CancellationToken cancellationToken = _refreshCts.Token;

        if (showLoadingText)
        {
            StatusText = Strings.Updating;
            if (_lastForecast is null)
                UpdatedText = Strings.Updating;
        }

        try
        {
            GeoResult? selectedPlace = placeOverride ?? (query.Length == 0 ? _currentPlace : _selectedPlace);
            var (place, forecast) = selectedPlace is not null &&
                                    string.Equals(selectedPlace.Name, city, StringComparison.CurrentCultureIgnoreCase)
                ? await weatherService.GetWeatherAsync(selectedPlace, cancellationToken)
                : await weatherService.GetWeatherAsync(city, cancellationToken);
            DateTimeOffset now = DateTimeOffset.Now;
            cancellationToken.ThrowIfCancellationRequested();

            _currentCity = place.Name;
            _currentPlace = place;
            _forecasts[(place.Latitude, place.Longitude)] =
                new ForecastSnapshot(place.Name, place.Country, forecast, now, false);
            ApplyForecast(place.Name, place.Country, forecast, now, fromCache: false);
            saveCity = saveCity && query.Length > 0;
            settingsService.SetCurrentPlace(place, saveCity);
            if (saveCity && !SavedCities.Any(city => SettingsService.IsSamePlace(city.Place, place)))
                SavedCities.Add(new SavedCityItem(place));
            UpdateSavedCitySelection();

            await SaveForecastsAsync();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
                ShowForecastError(ex);
        }
    }

    private void ShowForecast(ForecastSnapshot snapshot) =>
        ApplyForecast(snapshot.City, snapshot.Country, snapshot.Forecast, snapshot.UpdatedAt, snapshot.FromCache);

    private void ShowForecastError(Exception error)
    {
        StatusText = string.Format(CultureInfo.CurrentCulture, Strings.UpdateFailedFormat, error.Message);
        if (_lastForecast is null)
            UpdatedText = StatusText;
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
        UpdateSavedCityWeather();
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

        DisplayCity = _currentPlace is { } place
            ? string.IsNullOrWhiteSpace(place.Country)
                ? place.Name
                : string.Format(CultureInfo.CurrentCulture, Strings.LocationFormat, place.Name, place.Country)
            : Strings.DefaultDisplayCity;
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
        _citySearchCts?.Cancel();
        _citySearchCts?.Dispose();
    }

    private sealed record ForecastSnapshot(
        string City,
        string Country,
        ForecastResponse Forecast,
        DateTimeOffset UpdatedAt,
        bool FromCache);

    public sealed record HourItem(string Time, string Glyph, string Temperature);
    public sealed record DayItem(string Day, string Glyph, string Condition, string Range);

    public sealed partial class SavedCityItem(GeoResult place) : ObservableObject
    {
        public GeoResult Place { get; } = place;

        [ObservableProperty]
        public partial bool IsCurrent { get; set; }

        [ObservableProperty]
        public partial string WeatherGlyph { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string TemperatureText { get; set; } = "—";

        [ObservableProperty]
        public partial string ConditionText { get; set; } = string.Empty;
    }
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
