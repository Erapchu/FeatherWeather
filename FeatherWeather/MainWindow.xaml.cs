using System.Windows;
using System.Windows.Shell;

namespace FeatherWeather;

internal partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private bool _firstShown = true;

    public MainWindow(MainViewModel viewModel, SettingsView settingsView)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = viewModel;
        SecondaryContent.Content = settingsView;

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
        Closed += (_, _) => _viewModel.CancelPendingRefresh();
    }

    private async void OnContentRendered(object? sender, EventArgs e)
    {
        if (!_firstShown)
            return;

        _firstShown = false;
        await _viewModel.InitializeAsync();
    }
}
