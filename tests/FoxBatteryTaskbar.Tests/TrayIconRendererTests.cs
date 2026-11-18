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
