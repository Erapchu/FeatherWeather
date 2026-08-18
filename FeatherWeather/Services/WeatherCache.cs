using System.IO;
using System.Text.Json;
using FeatherWeather.Models;

namespace FeatherWeather.Services;

internal sealed class WeatherCache
{
    public CachedWeather? TryLoad()
    {
        try
        {
            if (!File.Exists(AppDataPaths.WeatherCacheFilePath))
                return null;

            using FileStream stream = File.OpenRead(AppDataPaths.WeatherCacheFilePath);
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
            Directory.CreateDirectory(AppDataPaths.DirectoryPath);
            using FileStream stream = File.Create(AppDataPaths.WeatherCacheFilePath);
            JsonSerializer.Serialize(stream, weather, WeatherJsonContext.Default.CachedWeather);
        }
        catch
        {
            // Cache is best-effort. Weather display must not depend on disk writes.
        }
    }
}
