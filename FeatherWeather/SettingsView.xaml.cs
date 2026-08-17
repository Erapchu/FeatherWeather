using System.Windows;
using System.Windows.Controls;

namespace FeatherWeather;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();

        string currentTheme = Application.Current.ThemeMode.ToString();
        foreach (ComboBoxItem item in ThemeSelector.Items)
        {
            if (string.Equals(item.Tag?.ToString(), currentTheme, StringComparison.OrdinalIgnoreCase))
            {
                ThemeSelector.SelectedItem = item;
                break;
            }
        }
    }

    private void ThemeSelector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeSelector.SelectedItem is not ComboBoxItem { Tag: string themeName })
        {
            return;
        }

        Application.Current.ThemeMode = themeName switch
        {
            "Light" => ThemeMode.Light,
            "Dark" => ThemeMode.Dark,
            _ => ThemeMode.System
        };
    }
}
