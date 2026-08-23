using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using FeatherWeather.ViewModels;

namespace FeatherWeather.Views;

internal partial class MainView : UserControl, IDisposable
{
    public static readonly DependencyProperty SettingsContentProperty =
        DependencyProperty.Register(
            nameof(SettingsContent),
            typeof(object),
            typeof(MainView));

    private readonly MainViewModel _viewModel;
    private readonly Lazy<SettingsView> _settingsView;

    public object? SettingsContent
    {
        get => GetValue(SettingsContentProperty);
        private set => SetValue(SettingsContentProperty, value);
    }

    public MainView(MainViewModel viewModel, Lazy<SettingsView> settingsView)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _settingsView = settingsView;
        DataContext = viewModel;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsSettingsVisible) &&
            _viewModel.IsSettingsVisible &&
            SettingsContent is null)
        {
            SettingsContent = _settingsView.Value;
        }
    }

    public void Dispose()
    {
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel.CancelPendingRefresh();
    }
}
