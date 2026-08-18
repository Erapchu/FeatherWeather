using System.IO;
using System.Text.Json;
using FeatherWeather.Models;

namespace FeatherWeather.Services;

public sealed class SettingsService
{
    public const string DefaultLanguage = "en";

    private static readonly HashSet<string> SupportedLanguages =
    [
        "en", "ru", "de", "fr", "es", "it", "pt", "nl", "pl", "uk", "ar"
    ];

    private AppSettings _settings = new();

    public event EventHandler? LanguageChanged;

    public string Language => _settings.Language;

    public void Initialize()
    {
        AppSettings? loadedSettings = LoadSettings();
        string language = loadedSettings?.Language
            ?? DefaultLanguage;

        _settings = new AppSettings { Language = NormalizeLanguage(language) };

        if (loadedSettings is null || loadedSettings.Language != _settings.Language)
            Save();
    }

    public void SetLanguage(string language)
    {
        language = NormalizeLanguage(language);
        if (_settings.Language == language)
            return;

        _settings = _settings with { Language = language };
        Save();
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    private static AppSettings? LoadSettings()
    {
        try
        {
            if (!File.Exists(AppDataPaths.SettingsFilePath))
                return null;

            using FileStream stream = File.OpenRead(AppDataPaths.SettingsFilePath);
            return JsonSerializer.Deserialize(stream, SettingsJsonContext.Default.AppSettings);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(AppDataPaths.DirectoryPath);
            using FileStream stream = File.Create(AppDataPaths.SettingsFilePath);
            JsonSerializer.Serialize(stream, _settings, SettingsJsonContext.Default.AppSettings);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string NormalizeLanguage(string language) =>
        SupportedLanguages.Contains(language) ? language : DefaultLanguage;
}
