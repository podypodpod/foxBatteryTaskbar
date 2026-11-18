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
