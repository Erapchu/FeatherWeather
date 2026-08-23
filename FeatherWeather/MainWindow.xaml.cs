using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Shell;
using FeatherWeather.Services;
using FeatherWeather.Views;

namespace FeatherWeather;

internal partial class MainWindow : Window
{
    private static readonly Duration ContentTransitionDuration =
        new(TimeSpan.FromMilliseconds(180));

    public MainWindow()
    {
        InitializeComponent();

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

                // Use the native DWM caption buttons.
                UseAeroCaptionButtons = true,

                NonClientFrameEdges =
                    OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)
                        ? NonClientFrameEdges.Left |
                          NonClientFrameEdges.Right |
                          NonClientFrameEdges.Bottom
                        : NonClientFrameEdges.None
            });
    }

    public void ShowMainContent(MainView mainView)
    {
        MainContent.Content = mainView;
        MainContent.Opacity = 1;
        StartupLogo.Opacity = 0;

        var easing = new QuadraticEase { EasingMode = EasingMode.EaseOut };
        var fadeIn = new DoubleAnimation(0, 1, ContentTransitionDuration)
        {
            EasingFunction = easing,
            FillBehavior = FillBehavior.Stop
        };
        var fadeOut = new DoubleAnimation(1, 0, ContentTransitionDuration)
        {
            EasingFunction = easing,
            FillBehavior = FillBehavior.Stop
        };

        fadeOut.Completed += (_, _) =>
        {
            StartupLogo.Visibility = Visibility.Collapsed;
            StartupLogo.BeginAnimation(OpacityProperty, null);
            StartupLogo.Opacity = 1;
        };

        MainContent.BeginAnimation(OpacityProperty, fadeIn);
        StartupLogo.BeginAnimation(OpacityProperty, fadeOut);
    }
}
