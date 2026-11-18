namespace FoxBatteryTaskbar.Tests;

public class BatteryReaderTests
{
    static FakeHttpHandler RoutingHandler() => new(req =>
    {
        var sample = req.RequestUri!.AbsolutePath.EndsWith("/device/list") ? "device-list.json" : "real-query.json";
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Samples", sample));
        return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(json) };
    });

    [Fact]
    public async Task Discovers_and_remembers_serial_when_not_configured()
    {
        var handler = RoutingHandler();
        var settings = new AppSettings { BaseUrl = "https://example.test" };
        int saved = 0;

        var r = await BatteryReader.ReadAsync(new HttpClient(handler), settings, "key", () => saved++);

        Assert.Equal(79, r.StateOfCharge);
        Assert.Equal("TESTSN000000001", settings.DeviceSerial);
        Assert.Equal(1, saved);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Uses_configured_serial_without_listing_devices()
    {
        var handler = RoutingHandler();
        var settings = new AppSettings { BaseUrl = "https://example.test", DeviceSerial = "MYSN" };
        int saved = 0;

        await BatteryReader.ReadAsync(new HttpClient(handler), settings, "key", () => saved++);

        var (req, body) = Assert.Single(handler.Requests);
        Assert.EndsWith("/device/real/query", req.RequestUri!.AbsolutePath);
        Assert.Contains("\"MYSN\"", body);
        Assert.Equal(0, saved);
    }
}
