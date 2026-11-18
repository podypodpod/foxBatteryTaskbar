using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace FoxBatteryTaskbar;

/// <summary>Draws the tray icon: the percentage as text, a fill bar, and a charging marker.</summary>
public static partial class TrayIconRenderer
{
    public static readonly Color Good = Color.FromArgb(80, 220, 100);
    public static readonly Color Warn = Color.FromArgb(255, 190, 40);
    public static readonly Color Bad = Color.FromArgb(255, 80, 70);
    public static readonly Color Neutral = Color.Gray;

    public static Color ColorFor(int percent) => percent >= 50 ? Good : percent >= 20 ? Warn : Bad;

    /// <summary>
    /// Renders an icon. The caller owns the returned icon and must dispose it with <see cref="Destroy"/>.
    /// </summary>
    public static Icon Render(string text, Color color, int? fillPercent = null, bool charging = false)
    {
        var size = SystemInformation.SmallIconSize;
        int w = Math.Max(16, size.Width), h = Math.Max(16, size.Height);

        using var bmp = new Bitmap(w, h);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int barHeight = Math.Max(2, h / 8);
            if (fillPercent is int pct)
            {
                using var track = new SolidBrush(Color.FromArgb(90, 255, 255, 255));
                using var fill = new SolidBrush(color);
                g.FillRectangle(track, 0, h - barHeight, w, barHeight);
                g.FillRectangle(fill, 0, h - barHeight, Math.Max(1, w * Math.Clamp(pct, 0, 100) / 100), barHeight);
            }

            DrawFittedText(g, text, color, new RectangleF(0, 0, w, h - barHeight));

            if (charging)
            {
                int a = w / 3;
                using var brush = new SolidBrush(Good);
                g.FillPolygon(brush, [new Point(w - a / 2 - 1, 0), new Point(w - 1, a / 2 + 1), new Point(w - a, a / 2 + 1)]);
            }
        }

        return Icon.FromHandle(bmp.GetHicon());
    }

    static void DrawFittedText(Graphics g, string text, Color color, RectangleF area)
    {
        var family = text.Length >= 3 ? "Arial Narrow" : "Segoe UI";
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

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DestroyIcon(IntPtr handle);
}
