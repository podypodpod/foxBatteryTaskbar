using Microsoft.Win32;

namespace FoxBatteryTaskbar;

/// <summary>Icon colours, chosen to stay readable on a dark or a light taskbar.</summary>
public sealed record TrayPalette(Color Good, Color Warn, Color Bad, Color Stale, Color Track)
{
    public static readonly TrayPalette Dark = new(
        Good: Color.FromArgb(80, 220, 100),
        Warn: Color.FromArgb(255, 190, 40),
        Bad: Color.FromArgb(255, 80, 70),
        Stale: Color.FromArgb(150, 150, 150),
        Track: Color.FromArgb(90, 255, 255, 255));

    public static readonly TrayPalette Light = new(
        Good: Color.FromArgb(0, 120, 40),
        Warn: Color.FromArgb(170, 95, 0),
        Bad: Color.FromArgb(200, 20, 20),
        Stale: Color.FromArgb(110, 110, 110),
        Track: Color.FromArgb(70, 0, 0, 0));

    /// <summary>Green ≥ 50%, amber ≥ 20%, red below.</summary>
    public Color ColorFor(int percent) => percent >= 50 ? Good : percent >= 20 ? Warn : Bad;

    /// <summary>The palette for the current Windows taskbar theme (Settings → Personalization → Colors).</summary>
    public static TrayPalette ForCurrentTheme() => TaskbarIsLight() ? Light : Dark;

    static bool TaskbarIsLight()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        // The taskbar follows "Windows mode" (SystemUsesLightTheme); missing value means dark (the default).
        return key?.GetValue("SystemUsesLightTheme") is int v && v != 0;
    }
}
