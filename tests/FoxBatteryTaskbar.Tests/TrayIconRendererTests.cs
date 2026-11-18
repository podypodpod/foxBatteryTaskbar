namespace FoxBatteryTaskbar.Tests;

public class TrayIconRendererTests
{
    public static TheoryData<string> Palettes => ["Dark", "Light"];
    static TrayPalette PaletteNamed(string name) => name == "Light" ? TrayPalette.Light : TrayPalette.Dark;

    [Theory]
    [InlineData(100, "Good")]
    [InlineData(50, "Good")]
    [InlineData(49, "Warn")]
    [InlineData(20, "Warn")]
    [InlineData(19, "Bad")]
    [InlineData(0, "Bad")]
    public void ColorFor_uses_expected_thresholds(int percent, string expected)
    {
        foreach (var p in new[] { TrayPalette.Dark, TrayPalette.Light })
        {
            var colour = expected switch { "Good" => p.Good, "Warn" => p.Warn, _ => p.Bad };
            Assert.Equal(colour, p.ColorFor(percent));
        }
    }

    /// <summary>
    /// Every text colour must reach WCAG's 3:1 contrast for graphics against the taskbar it is meant for
    /// (Windows 11 dark taskbar ≈ #202020, light ≈ #F3F3F3).
    /// </summary>
    [Theory]
    [MemberData(nameof(Palettes))]
    public void Palette_colours_are_readable_on_their_taskbar(string name)
    {
        var p = PaletteNamed(name);
        var background = name == "Light" ? Color.FromArgb(243, 243, 243) : Color.FromArgb(32, 32, 32);
        foreach (var c in new[] { p.Good, p.Warn, p.Bad, p.Stale })
            Assert.True(Contrast(c, background) >= 3.0, $"{name} {c} contrast {Contrast(c, background):0.00}");
    }

    static double Contrast(Color a, Color b)
    {
        static double Lum(Color c)
        {
            static double Ch(int v) { var s = v / 255.0; return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4); }
            return 0.2126 * Ch(c.R) + 0.7152 * Ch(c.G) + 0.0722 * Ch(c.B);
        }
        double l1 = Lum(a), l2 = Lum(b);
        return (Math.Max(l1, l2) + 0.05) / (Math.Min(l1, l2) + 0.05);
    }

    [Fact]
    public void PickInstalled_falls_back_to_last_candidate_when_none_installed()
    {
        Assert.Equal("Segoe UI", TrayIconRenderer.PickInstalled("No Such Font 123", "Segoe UI"));
    }

    [Fact]
    public void Narrow_font_is_a_font_that_is_actually_installed()
    {
        Assert.Equal(TrayIconRenderer.NarrowFontFamily, TrayIconRenderer.PickInstalled(TrayIconRenderer.NarrowFontFamily, "Segoe UI"));
        Assert.NotEqual("Arial Narrow", TrayIconRenderer.NarrowFontFamily);
    }

    [Fact]
    public void TrayIconSize_is_at_least_16px()
    {
        Assert.True(TrayIconRenderer.TrayIconSize() >= 16);
    }

    [Theory]
    [InlineData("7")]
    [InlineData("79")]
    [InlineData("100")]
    [InlineData("!")]
    public void Render_produces_icon_of_tray_size(string text)
    {
        var icon = TrayIconRenderer.Render(text, Color.White, TrayPalette.Dark, 50, charging: true);
        try
        {
            Assert.True(icon.Width >= 16);
            Assert.True(icon.Height >= 16);
        }
        finally
        {
            TrayIconRenderer.Destroy(icon);
        }
    }
}
