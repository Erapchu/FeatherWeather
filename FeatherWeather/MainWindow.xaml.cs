using System.ComponentModel;
using System.Windows;
using System.Windows.Shell;
using FeatherWeather.Services;
using FeatherWeather.ViewModels;
using FeatherWeather.Views;

namespace FeatherWeather;

internal partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly Lazy<SettingsView> _settingsView;
    private bool _firstShown = true;

    public MainWindow(MainViewModel viewModel, Lazy<SettingsView> settingsView)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _settingsView = settingsView;
        DataContext = viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        SingleInstanceNotifier.RegisterWindow(this);

        WindowChrome.SetWindowChrome(
            this,
            new WindowChrome
            {
                CaptionHeight = 50,
                CornerRadius = new CornerRadius(12),
                GlassFrameThickness = new Thickness(-1),

                ResizeBorderThickness =
                    ResizeMode == ResizeMode.NoResize
                        ? default
                        : new Thickness(4),

                // Именно системные кнопки DWM
                UseAeroCaptionButtons = true,

                NonClientFrameEdges =
                    OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)
                        ? NonClientFrameEdges.Left |
                          NonClientFrameEdges.Right |
                          NonClientFrameEdges.Bottom
                        : NonClientFrameEdges.None
            });

        ContentRendered += OnContentRendered;
        Closed += OnClosed;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsSettingsVisible) &&
            _viewModel.IsSettingsVisible &&
            SecondaryContent.Content is null)
        {
            SecondaryContent.Content = _settingsView.Value;
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel.CancelPendingRefresh();
    }

    private async void OnContentRendered(object? sender, EventArgs e)
    {
        if (!_firstShown)
            return;

        _firstShown = false;
        await _viewModel.InitializeAsync();
    }
}
