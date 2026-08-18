using System.IO;
using System.Text.Json;
using FeatherWeather.Models;

namespace FeatherWeather.Services;

internal sealed class WeatherCache
{
    public async Task<CachedWeather?> TryLoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(AppDataPaths.WeatherCacheFilePath))
                return null;

            await using var stream = new FileStream(
                AppDataPaths.WeatherCacheFilePath,
                new FileStreamOptions
                {
                    Mode = FileMode.Open,
                    Access = FileAccess.Read,
                    Share = FileShare.Read,
                    Options = FileOptions.Asynchronous | FileOptions.SequentialScan
                });
            return await JsonSerializer.DeserializeAsync(
                    stream,
                    WeatherJsonContext.Default.CachedWeather,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveAsync(CachedWeather weather, CancellationToken cancellationToken = default)
    {
        string temporaryPath = AtomicFile.CreateTemporaryPath(AppDataPaths.WeatherCacheFilePath);

        try
        {
            Directory.CreateDirectory(AppDataPaths.DirectoryPath);
            await using (var stream = new FileStream(
                             temporaryPath,
                             new FileStreamOptions
                             {
                                 Mode = FileMode.CreateNew,
                                 Access = FileAccess.Write,
                                 Share = FileShare.None,
                                 Options = FileOptions.Asynchronous | FileOptions.WriteThrough
                             }))
            {
                await JsonSerializer.SerializeAsync(
                        stream,
                        weather,
                        WeatherJsonContext.Default.CachedWeather,
                        cancellationToken)
                    .ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            AtomicFile.Commit(temporaryPath, AppDataPaths.WeatherCacheFilePath);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // Cache is best-effort. Weather display must not depend on disk writes.
        }
        finally
        {
            AtomicFile.TryDelete(temporaryPath);
        }
    }
}
