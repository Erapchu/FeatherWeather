using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FeatherWeather.Services;
using System.Windows;

namespace FeatherWeather;

public partial class App : System.Windows.Application
{
    private static readonly IHost _host = Host
        .CreateDefaultBuilder()
        .ConfigureServices((context, services) =>
        {
            services.AddSingleton<WeatherCache>();
            services.AddSingleton<WeatherService>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<SettingsView>();
            services.AddSingleton<MainWindow>();
        })
        .Build();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host.Start();

        MainWindow window = _host.Services.GetRequiredService<MainWindow>();
        MainWindow = window;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _host.StopAsync().GetAwaiter().GetResult();
        }
        finally
        {
            _host.Dispose();
            base.OnExit(e);
        }
    }
}
