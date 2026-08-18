using System.Text.Json.Serialization;
using FeatherWeather.Services;

namespace FeatherWeather.Models;

internal sealed record AppSettings
{
    public string Language { get; init; } = SettingsService.DefaultLanguage;
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppSettings))]
internal partial class SettingsJsonContext : JsonSerializerContext
{
}
