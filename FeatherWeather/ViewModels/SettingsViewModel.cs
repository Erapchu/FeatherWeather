using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;

namespace FeatherWeather.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _selectedTheme = Application.Current.ThemeMode.ToString();

    partial void OnSelectedThemeChanged(string value)
    {
        Application.Current.ThemeMode = value switch
        {
            "Light" => ThemeMode.Light,
            "Dark" => ThemeMode.Dark,
            _ => ThemeMode.System
        };
    }
}
