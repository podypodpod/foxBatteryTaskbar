using System.Drawing;

namespace FoxBatteryTaskbar.Tests;

public class TrayIconRendererTests
{
    [Theory]
    [InlineData(100, "Good")]
    [InlineData(50, "Good")]
    [InlineData(49, "Warn")]
    [InlineData(20, "Warn")]
    [InlineData(19, "Bad")]
    [InlineData(0, "Bad")]
    public void ColorFor_uses_expected_thresholds(int percent, string expected)
    {
        var colour = expected switch
        {
            "Good" => TrayIconRenderer.Good,
            "Warn" => TrayIconRenderer.Warn,
            _ => TrayIconRenderer.Bad,
        };
        Assert.Equal(colour, TrayIconRenderer.ColorFor(percent));
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
        var icon = TrayIconRenderer.Render(text, Color.White, 50, charging: true);
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
