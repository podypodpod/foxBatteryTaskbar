using System.Globalization;

namespace FoxBatteryTaskbar;

/// <summary>Builds the tray tooltip text. Kept free of UI types so it can be unit tested.</summary>
public static class TrayText
{
    /// <summary>NotifyIcon.Text throws if longer than this on some .NET versions.</summary>
    public const int MaxLength = 63;

    /// <summary>Power below this (kW) counts as zero, to ignore sensor noise.</summary>
    const double Threshold = 0.05;

    public static string ForReading(BatteryReading r, bool stale = false)
    {
        var c = CultureInfo.CurrentCulture; // user's own decimal separator
        int soc = (int)Math.Round(r.StateOfCharge);
        string state = r.ChargePower > Threshold ? string.Format(c, "charging {0:0.0}kW", r.ChargePower)
            : r.DischargePower > Threshold ? string.Format(c, "using {0:0.0}kW", r.DischargePower)
            : "idle";
        // Grid: + importing, - exporting
        double grid = r.GridExportPower > Threshold ? -r.GridExportPower : r.GridImportPower;

        string text =
            $"{soc}% {state}\n" +
            string.Format(c, "Solar {0:0.0} House {1:0.0} Grid {2:+0.0;-0.0;0.0}kW\n", r.SolarPower, r.HousePower, grid) +
            (stale ? $"Stale since {r.FetchedAt:HH:mm}" : $"@{r.FetchedAt:HH:mm}");
        return Fit(text);
    }

    public static string ForError(string message) => Fit("Fox battery: " + message);

    public static string Fit(string text) => text.Length > MaxLength ? text[..MaxLength] : text;

    public static bool IsCharging(BatteryReading r) => r.ChargePower > Threshold;
}
