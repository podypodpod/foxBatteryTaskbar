using System.Runtime.InteropServices;

namespace FoxBatteryTaskbar;

static partial class Program
{
    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AttachConsole(int processId);

    [STAThread]
    static int Main(string[] args)
    {
        if (args.Contains("--test"))
            return RunSelfTest().GetAwaiter().GetResult();

        using var mutex = new Mutex(initiallyOwned: true, @"Local\FoxBatteryTaskbar", out bool isFirstInstance);
        if (!isFirstInstance) return 0;

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext());
        return 0;
    }

    /// <summary>Fetches one reading and prints it to the calling terminal.</summary>
    static async Task<int> RunSelfTest()
    {
        AttachConsole(-1); // WinExe has no console of its own; write to the parent terminal
        try
        {
            var settings = AppSettings.Load();
            var key = AppSettings.LoadApiKey()
                ?? throw new FoxCloudException($"No API key. Set {AppSettings.ApiKeyEnvironmentVariable} or use 'Set API key…' in the tray menu.");
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            var client = new FoxCloudClient(http, settings.BaseUrl, key);
            var sn = string.IsNullOrWhiteSpace(settings.DeviceSerial) ? await client.GetFirstDeviceSerialAsync() : settings.DeviceSerial;
            Console.WriteLine(await client.GetReadingAsync(sn));
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("ERROR: " + ex.Message);
            return 1;
        }
    }
}
