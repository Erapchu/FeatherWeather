using Microsoft.Extensions.DependencyInjection;
using FeatherWeather.Services;
using FeatherWeather.ViewModels;
using FeatherWeather.Views;
using System.Windows;

namespace FeatherWeather;

public partial class App : Application
{
    private static readonly ServiceProvider _services = new ServiceCollection()
        .AddSingleton<WeatherCache>()
        .AddSingleton<WeatherService>()
        .AddSingleton<MainViewModel>()
        .AddSingleton<SettingsViewModel>()
        .AddSingleton<SettingsView>()
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

        MainWindow window = _services.GetRequiredService<MainWindow>();
        MainWindow = window;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services.Dispose();
        base.OnExit(e);
    }
}
