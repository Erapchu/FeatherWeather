using Microsoft.Extensions.DependencyInjection;
using FeatherWeather.Services;
using FeatherWeather.Resources;
using FeatherWeather.ViewModels;
using FeatherWeather.Views;
using System.Windows;
using System.Windows.Threading;

namespace FeatherWeather;

public partial class App : Application
{
    private ServiceProvider? _services;
    private SettingsService? _settingsService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Visual preferences must be known before the first window frame is rendered.
        _settingsService = new SettingsService();
        _settingsService.Initialize();
        _settingsService.ThemeChanged += OnThemeChanged;
        ApplyTheme(_settingsService.Theme);
        LocalizationManager.Instance.Initialize(_settingsService);

        var window = new MainWindow();
        MainWindow = window;
        window.ContentRendered += OnInitialContentRendered;
        window.Show();
    }

    private async void OnInitialContentRendered(object? sender, EventArgs e)
    {
        var window = (MainWindow)sender!;
        window.ContentRendered -= OnInitialContentRendered;

        // Let the first, lightweight frame reach the compositor before doing any startup work.
        await Dispatcher.Yield(DispatcherPriority.ContextIdle);

        _services = BuildServices(_settingsService!);

        MainViewModel viewModel = _services.GetRequiredService<MainViewModel>();
        MainView mainView = _services.GetRequiredService<MainView>();
        window.ShowMainContent(mainView);
        await viewModel.InitializeAsync();
    }

    private static ServiceProvider BuildServices(SettingsService settingsService) =>
        new ServiceCollection()
            .AddSingleton<WeatherCache>()
            .AddSingleton<WeatherService>()
            .AddSingleton(settingsService)
            .AddSingleton<MainViewModel>()
            .AddSingleton<SettingsViewModel>()
            .AddSingleton<SettingsView>()
            .AddSingleton(sp => new Lazy<SettingsView>(sp.GetRequiredService<SettingsView>))
            .AddSingleton<MainView>()
            .BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true
                });

    protected override void OnExit(ExitEventArgs e)
    {
        _settingsService?.ThemeChanged -= OnThemeChanged;

        _services?.Dispose();
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
