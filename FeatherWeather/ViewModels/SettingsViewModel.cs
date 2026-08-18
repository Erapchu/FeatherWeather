using CommunityToolkit.Mvvm.ComponentModel;
using FeatherWeather.Resources;
using FeatherWeather.Services;

namespace FeatherWeather.ViewModels;

public sealed partial class SettingsViewModel(SettingsService settingsService) : ObservableObject
{
    public IReadOnlyList<LanguageOption> Languages { get; } =
    [
        new("en", Strings.LanguageEnglish),
        new("ru", Strings.LanguageRussian),
        new("de", Strings.LanguageGerman),
        new("fr", Strings.LanguageFrench),
        new("es", Strings.LanguageSpanish),
        new("it", Strings.LanguageItalian),
        new("pt", Strings.LanguagePortuguese),
        new("nl", Strings.LanguageDutch),
        new("pl", Strings.LanguagePolish),
        new("uk", Strings.LanguageUkrainian),
        new("ar", Strings.LanguageArabic)
    ];

    [ObservableProperty]
    public partial string SelectedTheme { get; set; } = settingsService.Theme;

    [ObservableProperty]
    public partial string SelectedLanguage { get; set; } = settingsService.Language;

    partial void OnSelectedThemeChanged(string value) =>
        settingsService.SetTheme(value);

    partial void OnSelectedLanguageChanged(string value) =>
        settingsService.SetLanguage(value);

    public sealed record LanguageOption(string Code, string Name);
}
