namespace FeatherWeather;

internal static class Program
{
    [STAThread]
    public static void Main()
    {
        using var mutex = new Mutex(
            initiallyOwned: true,
            name: @"Local\FeatherWeather",
            createdNew: out bool isFirstInstance);

        if (!isFirstInstance)
        {
            SingleInstanceNotifier.NotifyExistingInstance();
            return;
        }

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
