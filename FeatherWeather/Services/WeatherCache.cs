using System.IO;
using System.Text.Json;
using FeatherWeather.Models;

namespace FeatherWeather.Services;

internal sealed class WeatherCache
{
    private static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FeatherWeather");

    private static readonly string FilePath = Path.Combine(DirectoryPath, "weather.json");

    public CachedWeather? TryLoad()
    {
        try
        {
            if (!File.Exists(FilePath))
                return null;

            using FileStream stream = File.OpenRead(FilePath);
            return JsonSerializer.Deserialize(stream, WeatherJsonContext.Default.CachedWeather);
        }
        catch
        {
            return null;
        }
    }

    public void Save(CachedWeather weather)
    {
        try
        {
            Directory.CreateDirectory(DirectoryPath);
            using FileStream stream = File.Create(FilePath);
            JsonSerializer.Serialize(stream, weather, WeatherJsonContext.Default.CachedWeather);
        }
        catch
        {
            // Cache is best-effort. Weather display must not depend on disk writes.
        }
    }
}
