using System.Windows.Controls;
using FeatherWeather.ViewModels;

namespace FeatherWeather.Views;

public partial class SettingsView : UserControl
{
    public SettingsView(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
