using Microsoft.Extensions.DependencyInjection;
using FeatherWeather.Services;
using FeatherWeather.Resources;
using FeatherWeather.ViewModels;
using FeatherWeather.Views;
using System.Windows;

namespace FeatherWeather;

public partial class App : Application
{
    private static readonly ServiceProvider _services = new ServiceCollection()
        .AddSingleton<WeatherCache>()
        .AddSingleton<WeatherService>()
        .AddSingleton<SettingsService>()
        .AddSingleton<MainViewModel>()
        .AddSingleton<SettingsViewModel>()
        .AddSingleton<SettingsView>()
        .AddSingleton(sp => new Lazy<SettingsView>(sp.GetRequiredService<SettingsView>))
        .AddSingleton<MainWindow>()
        .BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        SettingsService settingsService = _services.GetRequiredService<SettingsService>();
        settingsService.Initialize();
        settingsService.ThemeChanged += OnThemeChanged;
        ApplyTheme(settingsService.Theme);
        LocalizationManager.Instance.Initialize(settingsService);

        MainWindow window = _services.GetRequiredService<MainWindow>();
        MainWindow = window;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services.GetRequiredService<SettingsService>().ThemeChanged -= OnThemeChanged;
        _services.Dispose();
        base.OnExit(e);
    }

    private void OnThemeChanged(object? sender, EventArgs e) =>
        ApplyTheme(((SettingsService)sender!).Theme);

    private void ApplyTheme(string theme)
    {
        ThemeMode = theme switch
        {
            "Light" => ThemeMode.Light,
            "Dark" => ThemeMode.Dark,
            _ => ThemeMode.System
        };
    }
}
