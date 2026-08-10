# Feather Weather

Небольшое WPF-приложение погоды, сделанное с приоритетом на быстрый запуск и низкое потребление памяти.

## Стек

- .NET 9 / WPF
- встроенный WPF Fluent Theme (`ThemeMode="System"`)
- Open-Meteo Forecast + Geocoding API
- `System.Text.Json` source generation
- без сторонних NuGet-пакетов
- без DI-контейнера, Generic Host, WebView, MVVM-фреймворка и тяжёлых chart-библиотек

## Почему должно стартовать быстро

1. Главное окно создаётся без сетевых вызовов.
2. Последний прогноз читается из маленького локального JSON-кэша и сразу показывается.
3. Сетевое обновление запускается только после первого `ContentRendered`.
4. Один статический `HttpClient` на всё приложение.
5. JSON-модели используют source generation вместо runtime reflection metadata.
6. Нет фоновых таймеров. Пока приложение открыто, оно ничего не опрашивает само по себе.
7. Нет single-file упаковки: обычный framework-dependent build даёт ОС и .NET лучший шанс переиспользовать уже загруженные shared-компоненты.

## Запуск

Нужен .NET 9 SDK / Visual Studio с workload Desktop development with .NET.

```powershell
dotnet run --project .\FeatherWeather\FeatherWeather.csproj -c Release
```

## Публикация

Для собственного компьютера рекомендую framework-dependent Release:

```powershell
dotnet publish .\FeatherWeather\FeatherWeather.csproj -c Release -r win-x64 --self-contained false
```

Не включайте `PublishSingleFile` только ради одного EXE: это не является бесплатной оптимизацией cold start.

## Измерение памяти

Сравнивайте приложения после одинакового сценария: запустить Release, дождаться загрузки прогноза, свернуть/развернуть и оставить на 20–30 секунд.

PowerShell:

```powershell
$p = Get-Process FeatherWeather
$p | Select-Object ProcessName,
    @{N='WorkingSetMB';E={[math]::Round($_.WorkingSet64/1MB,1)}},
    @{N='PrivateMB';E={[math]::Round($_.PrivateMemorySize64/1MB,1)}}
```

Для footprint полезнее в первую очередь смотреть `PrivateMemorySize64`, а Working Set использовать как дополнительный показатель.

## Что можно добавить дальше без раздувания приложения

- автоопределение города через Windows Location API (лучше отдельной optional-функцией)
- системный tray icon
- мини-режим окна
- осадки на ближайшие часы
- UV / качество воздуха отдельным запросом по требованию
- ручной Light/Dark переключатель

