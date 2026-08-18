using System.IO;
using System.Text.Json;
using FeatherWeather.Models;

namespace FeatherWeather.Services;

public sealed class SettingsService
{
    public const string DefaultLanguage = "en";
    public const string DefaultTheme = "System";

    private static readonly HashSet<string> SupportedLanguages =
    [
        "en", "ru", "de", "fr", "es", "it", "pt", "nl", "pl", "uk", "ar"
    ];

    private AppSettings _settings = new();

    public event EventHandler? LanguageChanged;
    public event EventHandler? ThemeChanged;

    public string Language => _settings.Language;
    public string City => _settings.City;
    public string Theme => _settings.Theme;

    public void Initialize()
    {
        AppSettings? loadedSettings = LoadSettings();
        _settings = new AppSettings
        {
            Language = NormalizeLanguage(loadedSettings?.Language ?? DefaultLanguage),
            City = loadedSettings?.City?.Trim() ?? string.Empty,
            Theme = NormalizeTheme(loadedSettings?.Theme ?? DefaultTheme)
        };
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

    public void SetCity(string city)
    {
        city = city.Trim();
        if (_settings.City == city)
            return;

        _settings = _settings with { City = city };
        Save();
    }

    public void SetTheme(string theme)
    {
        theme = NormalizeTheme(theme);
        if (_settings.Theme == theme)
            return;

        _settings = _settings with { Theme = theme };
        Save();
        ThemeChanged?.Invoke(this, EventArgs.Empty);
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
        string temporaryPath = AtomicFile.CreateTemporaryPath(AppDataPaths.SettingsFilePath);

        try
        {
            Directory.CreateDirectory(AppDataPaths.DirectoryPath);
            using (var stream = new FileStream(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None))
            {
                JsonSerializer.Serialize(stream, _settings, SettingsJsonContext.Default.AppSettings);
                stream.Flush(flushToDisk: true);
            }

            AtomicFile.Commit(temporaryPath, AppDataPaths.SettingsFilePath);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
        finally
        {
            AtomicFile.TryDelete(temporaryPath);
        }
    }

    private static string NormalizeLanguage(string language) =>
        SupportedLanguages.Contains(language) ? language : DefaultLanguage;

    private static string NormalizeTheme(string theme) =>
        theme is "Light" or "Dark" ? theme : DefaultTheme;
}
