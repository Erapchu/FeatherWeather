using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using FeatherWeather.Services;

namespace FeatherWeather.Resources;

public sealed class LocalizationManager : INotifyPropertyChanged
{
    private SettingsService? _settingsService;

    public static LocalizationManager Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? CultureChanged;

    public string CultureName { get; private set; } = SettingsService.DefaultLanguage;

    public FlowDirection FlowDirection => CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft
        ? FlowDirection.RightToLeft
        : FlowDirection.LeftToRight;

    public string this[string key] =>
        Strings.ResourceManager.GetString(key, Strings.Culture) ?? key;

    private LocalizationManager()
    {
    }

    internal void Initialize(SettingsService settingsService)
    {
        if (_settingsService is not null)
            _settingsService.LanguageChanged -= OnLanguageChanged;

        _settingsService = settingsService;
        _settingsService.LanguageChanged += OnLanguageChanged;
        ApplyCulture(_settingsService.Language);
    }

    private void OnLanguageChanged(object? sender, EventArgs e) =>
        ApplyCulture(_settingsService?.Language ?? SettingsService.DefaultLanguage);

    private void ApplyCulture(string language)
    {
        string cultureName = language switch
        {
            "ru" => "ru-RU",
            "de" => "de-DE",
            "fr" => "fr-FR",
            "es" => "es-ES",
            "it" => "it-IT",
            "pt" => "pt-PT",
            "nl" => "nl-NL",
            "pl" => "pl-PL",
            "uk" => "uk-UA",
            "ar" => "ar-SA",
            _ => "en-GB"
        };
        CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        Strings.Culture = culture;
        CultureName = language;

        OnPropertyChanged("Item[]");
        OnPropertyChanged(nameof(FlowDirection));
        CultureChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
