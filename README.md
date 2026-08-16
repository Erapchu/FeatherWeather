# Feather Weather

Feather Weather is a small, lightweight weather app for Windows built with WPF. It is designed for fast startup and a low memory footprint without sacrificing the essentials of a desktop forecast.

The application UI is currently in Russian.

## Screenshots
![Alt text](images/app.png "App")

## Features

- Search for weather by city name
- Current temperature, apparent temperature, humidity, wind speed, and pressure
- Forecast for the next 8 hours
- 7-day forecast with daily high and low temperatures
- Automatic light and dark themes based on the Windows setting
- Immediate display of the last successful forecast from a local cache
- Manual refresh with request cancellation
- Single-instance application behavior

Weather and geocoding data are provided by [Open-Meteo](https://open-meteo.com/). No API key is required.

## Technology

- .NET 10 and WPF
- Built-in WPF Fluent theme (`ThemeMode="System"`)
- Open-Meteo Forecast and Geocoding APIs
- `System.Text.Json` source generation
- No third-party NuGet packages
- No dependency-injection container, Generic Host, WebView, MVVM framework, or charting library

## Requirements

- Windows 10 or later
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), or Visual Studio with the **.NET desktop development** workload
- An internet connection for retrieving fresh weather data

## Run locally

From the repository root:

```powershell
dotnet run --project .\FeatherWeather\FeatherWeather.csproj -c Release
```

The default city is Saint Petersburg. Enter another city and press **Enter** or use the refresh button to load its forecast.

## Build

```powershell
dotnet build .\FeatherWeather.sln -c Release
```

## Publish

For a framework-dependent, 64-bit Windows build:

```powershell
dotnet publish .\FeatherWeather\FeatherWeather.csproj -c Release -r win-x64 --self-contained false
```

The published files are written under `FeatherWeather\bin\Release\net10.0-windows\win-x64\publish\`.

The project intentionally keeps `PublishSingleFile` disabled. A regular framework-dependent build avoids bundling the runtime and lets Windows and .NET reuse shared components, which is a better fit for the application's startup and memory goals.

## Cache and startup behavior

The most recent successful forecast is stored at:

```text
%LOCALAPPDATA%\FeatherWeather\weather.json
```

On startup, the main window is displayed first. The cached forecast is then loaded and shown when available, while a fresh request runs in the background. Cache reads and writes are best-effort, so a missing or invalid cache does not prevent the application from running.

There are no background polling timers. Weather data is requested only at startup or when the user refreshes it.

## Performance choices

- A single static `HttpClient` is reused for all requests.
- Network access starts only after the window has rendered.
- JSON serialization metadata is generated at compile time.
- Release builds enable optimization, tiered compilation, tiered PGO, and ReadyToRun publishing.
- Straightforward WPF code-behind is used where it keeps the application smaller and simpler.

## Measuring memory usage

For useful comparisons, run a Release build, wait for the forecast to load, minimize and restore the window, and then leave it idle for 20–30 seconds.

```powershell
$process = Get-Process FeatherWeather
$process | Select-Object ProcessName,
    @{N='WorkingSetMB';E={[math]::Round($_.WorkingSet64 / 1MB, 1)}},
    @{N='PrivateMB';E={[math]::Round($_.PrivateMemorySize64 / 1MB, 1)}}
```

`PrivateMemorySize64` is generally the more useful primary footprint measurement; treat the working set as an additional data point.

## Possible future improvements

- Optional city detection through the Windows Location API
- System tray support
- Compact window mode
- Short-term precipitation forecast
- On-demand UV index and air quality data
- Manual theme selection
