using System.IO;

namespace FeatherWeather.Services;

internal static class AppDataPaths
{
    public static string DirectoryPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FeatherWeather");

    public static string WeatherCacheFilePath { get; } = Path.Combine(DirectoryPath, "weather.json");

    public static string SettingsFilePath { get; } = Path.Combine(DirectoryPath, "settings.json");
}
