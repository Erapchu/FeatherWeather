using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace FeatherWeather.Services;

internal static class SingleInstanceNotifier
{
    private const string ShowMainWindowMessageName = "FeatherWeather.ShowMainWindow";
    private static readonly nint HwndBroadcast = new(0xffff);

    private static readonly uint ShowMainWindowMessage =
        RegisterWindowMessage(ShowMainWindowMessageName);

    public static void RegisterWindow(Window window)
    {
        window.SourceInitialized += (_, _) =>
        {
            var source = (HwndSource)PresentationSource.FromVisual(window);
            source.AddHook((nint hwnd, int message, nint wParam, nint lParam, ref bool handled) =>
            {
                if ((uint)message != ShowMainWindowMessage)
                    return nint.Zero;

                ShowWindow(window);
                handled = true;
                return nint.Zero;
            });
        };
    }

    public static void NotifyExistingInstance() =>
        SendNotifyMessage(HwndBroadcast, ShowMainWindowMessage, nint.Zero, nint.Zero);

    private static void ShowWindow(Window window)
    {
        if (!window.IsVisible)
            window.Show();

        if (window.WindowState == WindowState.Minimized)
            window.WindowState = WindowState.Normal;

        window.Activate();
    }

    [DllImport("user32.dll", EntryPoint = "RegisterWindowMessageW", CharSet = CharSet.Unicode)]
    private static extern uint RegisterWindowMessage(string messageName);

    [DllImport("user32.dll", EntryPoint = "SendNotifyMessageW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SendNotifyMessage(
        nint hwnd,
        uint message,
        nint wParam,
        nint lParam);
}
