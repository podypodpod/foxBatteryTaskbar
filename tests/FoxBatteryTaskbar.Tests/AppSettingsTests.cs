namespace FoxBatteryTaskbar.Tests;

public sealed class AppSettingsTests : IDisposable
{
    readonly string dir = Directory.CreateTempSubdirectory("fbt-tests-").FullName;
    string SettingsFile => Path.Combine(dir, "settings.json");

    public void Dispose() => Directory.Delete(dir, recursive: true);

    [Fact]
    public void Missing_file_gives_defaults()
    {
        var s = AppSettings.LoadFrom(SettingsFile);
        Assert.Equal("https://www.foxesscloud.com", s.BaseUrl);
        Assert.Null(s.DeviceSerial);
        Assert.Equal(5, s.PollMinutes);
    }

    [Fact]
    public void Corrupt_file_gives_defaults()
    {
        File.WriteAllText(SettingsFile, "{ this is not json");
        Assert.Equal(5, AppSettings.LoadFrom(SettingsFile).PollMinutes);
    }

    [Theory]
    [InlineData(-5, AppSettings.MinPollMinutes)]
    [InlineData(0, AppSettings.MinPollMinutes)]
    [InlineData(1, AppSettings.MinPollMinutes)]
    [InlineData(5, 5)]
    [InlineData(50_000, AppSettings.MaxPollMinutes)]
    [InlineData(int.MaxValue, AppSettings.MaxPollMinutes)]
    public void EffectivePollMinutes_is_clamped(int configured, int expected)
    {
        Assert.Equal(expected, new AppSettings { PollMinutes = configured }.EffectivePollMinutes);
    }

    [Fact]
    public void Max_poll_interval_fits_a_timer()
    {
        // Timer.Interval is an int of milliseconds; the clamp must keep the cast from overflowing.
        Assert.True(TimeSpan.FromMinutes(AppSettings.MaxPollMinutes).TotalMilliseconds < int.MaxValue);
    }

    [Fact]
    public void Values_are_read_from_file()
    {
        File.WriteAllText(SettingsFile, """{"BaseUrl":"https://portal.foxesscloud.us","DeviceSerial":"SN1","PollMinutes":10}""");
        var s = AppSettings.LoadFrom(SettingsFile);
        Assert.Equal("https://portal.foxesscloud.us", s.BaseUrl);
        Assert.Equal("SN1", s.DeviceSerial);
        Assert.Equal(10, s.PollMinutes);
    }
}
