using System.Globalization;

namespace FoxBatteryTaskbar.Tests;

public sealed class TrayTextTests : IDisposable
{
    readonly CultureInfo originalCulture = CultureInfo.CurrentCulture;

    public TrayTextTests() => CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    public void Dispose() => CultureInfo.CurrentCulture = originalCulture;

    static BatteryReading Reading(double soc = 50, double charge = 0, double discharge = 0, double pv = 0,
        double house = 0, double import = 0, double export = 0) =>
        new(soc, charge, discharge, pv, house, import, export, 30, new DateTimeOffset(2026, 1, 1, 8, 5, 0, TimeSpan.Zero));

    [Fact]
    public void Idle_reading_is_formatted()
    {
        Assert.Equal("50% idle\nSolar 0.0 House 1.2 Grid +1.2kW\n@08:05",
            TrayText.ForReading(Reading(house: 1.2, import: 1.2)));
    }

    [Fact]
    public void Charging_and_exporting_are_shown()
    {
        var text = TrayText.ForReading(Reading(soc: 64.6, charge: 2.5, pv: 5, house: 0.5, export: 2));
        Assert.StartsWith("65% charging 2.5kW", text);
        Assert.Contains("Grid -2.0kW", text);
    }

    [Fact]
    public void Discharging_is_shown()
    {
        Assert.StartsWith("30% using 1.0kW", TrayText.ForReading(Reading(soc: 30, discharge: 1.004)));
    }

    [Fact]
    public void Stale_reading_says_so()
    {
        Assert.EndsWith("Stale since 08:05", TrayText.ForReading(Reading(), stale: true));
    }

    public static TheoryData<BatteryReading> ExtremeReadings =>
    [
        Reading(soc: 100, charge: 12.34, pv: 99.9, house: 99.9, import: 99.9),
        Reading(soc: 100, discharge: 123.45, pv: 1234.5, house: 1234.5, export: 1234.5),
        Reading(soc: -5, charge: double.MaxValue, pv: double.MaxValue, house: double.MaxValue, export: double.MaxValue),
    ];

    /// <summary>A tooltip longer than NotifyIcon allows crashed the app in an early version.</summary>
    [Theory]
    [MemberData(nameof(ExtremeReadings))]
    public void Tooltip_never_exceeds_notify_icon_limit(BatteryReading r)
    {
        Assert.True(TrayText.ForReading(r).Length <= TrayText.MaxLength);
        Assert.True(TrayText.ForReading(r, stale: true).Length <= TrayText.MaxLength);
    }

    [Fact]
    public void Long_error_is_truncated()
    {
        Assert.Equal(TrayText.MaxLength, TrayText.ForError(new string('x', 500)).Length);
    }

    [Fact]
    public void Uses_users_decimal_separator()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
        Assert.Contains("House 1,5", TrayText.ForReading(Reading(house: 1.5)));
    }
}
