using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace FoxBatteryTaskbar;

/// <summary>Draws the tray icon: the percentage as text, a fill bar, and a charging marker.</summary>
public static partial class TrayIconRenderer
{
    /// <summary>
    /// Renders an icon. The caller owns the returned icon and must dispose it with <see cref="Destroy"/>.
    /// </summary>
    public static Icon Render(string text, Color color, TrayPalette palette, int? fillPercent = null, bool charging = false)
    {
        int w = TrayIconSize(), h = w;

        using var bmp = new Bitmap(w, h);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int barHeight = Math.Max(2, h / 8);
            if (fillPercent is int pct)
            {
                using var track = new SolidBrush(palette.Track);
                using var fill = new SolidBrush(color);
                g.FillRectangle(track, 0, h - barHeight, w, barHeight);
                g.FillRectangle(fill, 0, h - barHeight, Math.Max(1, w * Math.Clamp(pct, 0, 100) / 100), barHeight);
            }

            DrawFittedText(g, text, color, new RectangleF(0, 0, w, h - barHeight));

            if (charging)
            {
                int a = w / 3;
                using var brush = new SolidBrush(palette.Good);
                g.FillPolygon(brush, [new Point(w - a / 2 - 1, 0), new Point(w - 1, a / 2 + 1), new Point(w - a, a / 2 + 1)]);
            }
        }

        return Icon.FromHandle(bmp.GetHicon());
    }

    static void DrawFittedText(Graphics g, string text, Color color, RectangleF area)
    {
        var family = text.Length >= 3 ? NarrowFontFamily : "Segoe UI";
        var format = StringFormat.GenericTypographic;
        for (float px = area.Height; px > 5; px -= 0.5f)
        {
            using var font = new Font(family, px, FontStyle.Bold, GraphicsUnit.Pixel);
            var m = g.MeasureString(text, font, PointF.Empty, format);
            if (m.Width > area.Width || m.Height > area.Height) continue;

            using var brush = new SolidBrush(color);
            g.DrawString(text, font, brush, area.X + (area.Width - m.Width) / 2, area.Y + (area.Height - m.Height) / 2, format);
            return;
        }
    }

    /// <summary>Frees the native handle behind an icon created by <see cref="Render"/>.</summary>
    public static void Destroy(Icon? icon)
    {
        if (icon is null) return;
        var handle = icon.Handle;
        icon.Dispose();
        DestroyIcon(handle);
    }

    /// <summary>
    /// A condensed font so "100" stays legible. Bahnschrift ships with Windows 10 1709+;
    /// fall back to Segoe UI (always present) rather than letting GDI+ substitute a wide font.
    /// </summary>
    internal static readonly string NarrowFontFamily = PickInstalled("Bahnschrift SemiBold Condensed", "Bahnschrift Condensed", "Segoe UI");

    internal static string PickInstalled(params string[] candidates)
    {
        using var installed = new InstalledFontCollection();
        var names = installed.Families.Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return candidates.FirstOrDefault(names.Contains) ?? candidates[^1];
    }

    /// <summary>Small-icon size at the DPI of the monitor showing the taskbar (not just the system DPI at startup).</summary>
    internal static int TrayIconSize()
    {
        const int SM_CXSMICON = 49;
        uint dpi = 0;
        var taskbar = FindWindowW("Shell_TrayWnd", null);
        if (taskbar != IntPtr.Zero) dpi = GetDpiForWindow(taskbar);
        if (dpi == 0) dpi = GetDpiForSystem();
        int size = GetSystemMetricsForDpi(SM_CXSMICON, dpi);
        return Math.Max(16, size > 0 ? size : SystemInformation.SmallIconSize.Width);
    }

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr FindWindowW(string className, string? windowName);

    [LibraryImport("user32.dll")]
    private static partial uint GetDpiForWindow(IntPtr hwnd);

    [LibraryImport("user32.dll")]
    private static partial uint GetDpiForSystem();

    [LibraryImport("user32.dll")]
    private static partial int GetSystemMetricsForDpi(int index, uint dpi);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DestroyIcon(IntPtr handle);
}
