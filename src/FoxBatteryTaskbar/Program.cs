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
            var key = AppSettings.LoadApiKey() ?? throw new FoxCloudException(BatteryReader.MissingKeyMessage);
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            // Same path as the tray app; the discovered serial isn't saved, so --test has no side effects.
            Console.WriteLine(await BatteryReader.ReadAsync(http, settings, key));
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("ERROR: " + ex.Message);
            return 1;
        }
    }
}
