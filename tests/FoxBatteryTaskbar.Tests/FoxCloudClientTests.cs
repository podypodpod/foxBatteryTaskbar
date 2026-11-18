using System.Net;
using System.Text.Json.Nodes;

namespace FoxBatteryTaskbar.Tests;

public class FoxCloudClientTests
{
    const string BaseUrl = "https://example.test";
    const string ApiKey = "test-key";

    static FoxCloudClient Client(FakeHttpHandler handler) => new(new HttpClient(handler), BaseUrl, ApiKey);

    [Fact]
    public void Sign_matches_fox_documentation_example()
    {
        // Example from the Fox docs; separator is the literal 4 characters \r\n.
        // Expected value computed independently with PowerShell's MD5.
        var sig = FoxCloudClient.Sign("/op/v0/device/list", "abc123xyz", "1719834567890");
        Assert.Equal("ec1ceaf12d2dcefe83b18f26f424950d", sig);
    }

    [Fact]
    public void Sign_uses_literal_backslash_separator_not_crlf()
    {
        var literal = FoxCloudClient.Sign("/p", "t", "1");
        var md5OfCrlf = Convert.ToHexStringLower(
            System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes("/p\r\nt\r\n1")));
        Assert.NotEqual(md5OfCrlf, literal);
    }

    [Fact]
    public async Task GetReadingAsync_parses_sample_response()
    {
        var r = await Client(FakeHttpHandler.Sample("real-query.json")).GetReadingAsync("TESTSN000000001");

        Assert.Equal(79, r.StateOfCharge);
        Assert.Equal(0, r.ChargePower);
        Assert.Equal(1.004, r.DischargePower);
        Assert.Equal(2.5, r.SolarPower);
        Assert.Equal(1.028, r.HousePower);
        Assert.Equal(0.024, r.GridImportPower);
        Assert.Equal(0, r.GridExportPower);
        Assert.Equal(35.5, r.BatteryTemperature);
    }

    [Fact]
    public async Task GetReadingAsync_sends_signed_request_with_serial_and_variables()
    {
        var handler = FakeHttpHandler.Sample("real-query.json");
        await Client(handler).GetReadingAsync("TESTSN000000001");

        var (req, body) = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.Equal($"{BaseUrl}/op/v0/device/real/query", req.RequestUri!.ToString());

        string Header(string name) => Assert.Single(req.Headers.GetValues(name));
        Assert.Equal(ApiKey, Header("token"));
        Assert.Equal("en", Header("lang"));
        var ts = Header("timestamp");
        Assert.Equal(FoxCloudClient.Sign("/op/v0/device/real/query", ApiKey, ts), Header("signature"));

        var json = JsonNode.Parse(body)!;
        Assert.Equal("TESTSN000000001", json["sn"]!.GetValue<string>());
        Assert.Contains("SoC", json["variables"]!.AsArray().Select(v => v!.GetValue<string>()));
    }

    [Fact]
    public async Task GetReadingAsync_accepts_values_sent_as_strings()
    {
        const string json = """{"errno":0,"result":[{"datas":[{"variable":"SoC","value":"42.6"}]}]}""";
        var r = await Client(FakeHttpHandler.Json(json)).GetReadingAsync("SN");

        Assert.Equal(42.6, r.StateOfCharge);
        Assert.Equal(0, r.SolarPower); // missing variables default to 0
        Assert.Null(r.BatteryTemperature);
    }

    [Fact]
    public async Task GetReadingAsync_throws_when_soc_missing()
    {
        const string json = """{"errno":0,"result":[{"datas":[{"variable":"pvPower","value":1.0}]}]}""";
        var ex = await Assert.ThrowsAsync<FoxCloudException>(() => Client(FakeHttpHandler.Json(json)).GetReadingAsync("SN"));
        Assert.Contains("state of charge", ex.Message);
    }

    [Fact]
    public async Task Api_error_number_becomes_FoxCloudException()
    {
        const string json = """{"errno":40256,"msg":"illegal signature"}""";
        var ex = await Assert.ThrowsAsync<FoxCloudException>(() => Client(FakeHttpHandler.Json(json)).GetReadingAsync("SN"));
        Assert.Contains("40256", ex.Message);
        Assert.Contains("illegal signature", ex.Message);
    }

    [Fact]
    public async Task Non_json_response_becomes_FoxCloudException()
    {
        var handler = FakeHttpHandler.Json("<html>Down for maintenance</html>");
        await Assert.ThrowsAsync<FoxCloudException>(() => Client(handler).GetReadingAsync("SN"));
    }

    [Theory]
    [InlineData("""{"errno":0,"result":{"datas":[]}}""")]          // object instead of array
    [InlineData("""{"errno":0,"result":[{"datas":{"SoC":1}}]}""")] // datas not an array
    [InlineData("""{"errno":0,"result":"oops"}""")]
    public async Task Unexpected_json_shape_becomes_FoxCloudException(string json)
    {
        await Assert.ThrowsAsync<FoxCloudException>(() => Client(FakeHttpHandler.Json(json)).GetReadingAsync("SN"));
    }

    [Fact]
    public async Task Errno_sent_as_string_is_understood()
    {
        const string json = """{"errno":"0","result":[{"datas":[{"variable":"SoC","value":55}]}]}""";
        var r = await Client(FakeHttpHandler.Json(json)).GetReadingAsync("SN");
        Assert.Equal(55, r.StateOfCharge);
    }

    [Fact]
    public async Task Unexpected_device_list_shape_becomes_FoxCloudException()
    {
        const string json = """{"errno":0,"result":{"data":{"deviceSN":"x"}}}""";
        await Assert.ThrowsAsync<FoxCloudException>(() => Client(FakeHttpHandler.Json(json)).GetFirstDeviceSerialAsync());
    }

    [Fact]
    public async Task User_agent_carries_assembly_version()
    {
        var handler = FakeHttpHandler.Sample("real-query.json");
        await Client(handler).GetReadingAsync("SN");

        var expected = typeof(FoxCloudClient).Assembly.GetName().Version!.ToString(3);
        var product = Assert.Single(handler.Requests[0].Request.Headers.UserAgent).Product!;
        Assert.Equal("FoxBatteryTaskbar", product.Name);
        Assert.Equal(expected, product.Version);
    }

    [Fact]
    public async Task Http_failure_throws_HttpRequestException()
    {
        var handler = FakeHttpHandler.Json("{}", HttpStatusCode.InternalServerError);
        await Assert.ThrowsAsync<HttpRequestException>(() => Client(handler).GetReadingAsync("SN"));
    }

    [Fact]
    public async Task GetFirstDeviceSerialAsync_returns_first_device()
    {
        var sn = await Client(FakeHttpHandler.Sample("device-list.json")).GetFirstDeviceSerialAsync();
        Assert.Equal("TESTSN000000001", sn);
    }

    [Fact]
    public async Task GetFirstDeviceSerialAsync_throws_when_account_has_no_devices()
    {
        const string json = """{"errno":0,"result":{"total":0,"data":[]}}""";
        await Assert.ThrowsAsync<FoxCloudException>(() => Client(FakeHttpHandler.Json(json)).GetFirstDeviceSerialAsync());
    }
}
