using System.Diagnostics;

namespace FoxBatteryTaskbar;

static class Shell
{
    /// <summary>Opens a URL or folder with its default handler.</summary>
    public static void Open(string target) =>
        Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
}
