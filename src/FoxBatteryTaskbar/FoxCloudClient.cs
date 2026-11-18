using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FoxBatteryTaskbar;

/// <summary>A snapshot of the battery and power flows. Powers are in kW.</summary>
public sealed record BatteryReading(
    double StateOfCharge,
    double ChargePower,
    double DischargePower,
    double SolarPower,
    double HousePower,
    double GridImportPower,
    double GridExportPower,
    double? BatteryTemperature,
    DateTimeOffset FetchedAt);

public sealed class FoxCloudException(string message) : Exception(message);

/// <summary>
/// Minimal client for the Fox ESS Cloud Open API (API-key authentication).
/// Docs: https://www.foxesscloud.com/public/i18n/en/OpenApiDocument.html
/// </summary>
public sealed class FoxCloudClient(HttpClient http, string baseUrl, string apiKey)
{
    static readonly string[] Variables =
    [
        "SoC", "batChargePower", "batDischargePower", "pvPower", "loadsPower",
        "gridConsumptionPower", "feedinPower", "batTemperature",
    ];

    public async Task<string> GetFirstDeviceSerialAsync(CancellationToken ct = default)
    {
        var result = await PostAsync("/op/v0/device/list", new { currentPage = 1, pageSize = 10 }, ct);
        var sn = ParseShape(() => result?["data"]?.AsArray().FirstOrDefault()?["deviceSN"]?.GetValue<string>());
        return sn ?? throw new FoxCloudException("No devices found on this Fox Cloud account.");
    }

    public async Task<BatteryReading> GetReadingAsync(string serial, CancellationToken ct = default)
    {
        var result = await PostAsync("/op/v0/device/real/query", new { sn = serial, variables = Variables }, ct);
        var values = ParseShape(() => ParseValues(result));

        if (!values.TryGetValue("SoC", out var soc))
            throw new FoxCloudException("Fox Cloud did not return a state of charge.");

        double Get(string k) => values.GetValueOrDefault(k);
        return new BatteryReading(
            soc, Get("batChargePower"), Get("batDischargePower"), Get("pvPower"), Get("loadsPower"),
            Get("gridConsumptionPower"), Get("feedinPower"),
            values.TryGetValue("batTemperature", out var t) ? t : null,
            DateTimeOffset.Now);
    }

    static Dictionary<string, double> ParseValues(JsonNode? result)
    {
        var datas = result?.AsArray().FirstOrDefault()?["datas"]?.AsArray()
            ?? throw new FoxCloudException("Unexpected response from Fox Cloud.");

        var values = new Dictionary<string, double>();
        foreach (var d in datas)
        {
            var name = d?["variable"]?.ToString();
            var value = d?["value"];
            if (name is null || value is null) continue;
            if (double.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                values[name] = v;
        }
        return values;
    }

    async Task<JsonNode?> PostAsync(string path, object body, CancellationToken ct)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);

        using var request = new HttpRequestMessage(HttpMethod.Post, baseUrl.TrimEnd('/') + path)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("token", apiKey);
        request.Headers.Add("timestamp", timestamp);
        request.Headers.Add("signature", Sign(path, apiKey, timestamp));
        request.Headers.Add("lang", "en");
        request.Headers.UserAgent.Add(UserAgent);

        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        var text = await response.Content.ReadAsStringAsync(ct);

        JsonNode? json;
        try
        {
            json = JsonNode.Parse(text);
        }
        catch (JsonException)
        {
            // e.g. an HTML maintenance page served with 200 OK
            throw new FoxCloudException("Fox Cloud returned a response that isn't JSON.");
        }

        var errnoNode = json?["errno"];
        var errno = errnoNode is not null && int.TryParse(errnoNode.ToString(), out var n) ? n : -1;
        if (errno != 0)
            throw new FoxCloudException($"Fox Cloud error {errno}: {json?["msg"]}");
        return json?["result"];
    }

    static readonly ProductInfoHeaderValue UserAgent = new(
        "FoxBatteryTaskbar",
        typeof(FoxCloudClient).Assembly.GetName().Version?.ToString(3) ?? "0.0.0");

    /// <summary>Navigating a JSON shape we didn't expect throws InvalidOperationException; report it as an API problem.</summary>
    static T ParseShape<T>(Func<T> parse)
    {
        try
        {
            return parse();
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException)
        {
            throw new FoxCloudException("Unexpected response from Fox Cloud.");
        }
    }

    /// <summary>
    /// md5(path + "\r\n" + token + "\r\n" + timestamp), lowercase hex.
    /// Fox uses the literal 4-character sequence backslash-r-backslash-n as the separator.
    /// </summary>
    internal static string Sign(string path, string token, string timestamp)
    {
        var source = $@"{path}\r\n{token}\r\n{timestamp}";
        return Convert.ToHexStringLower(MD5.HashData(Encoding.UTF8.GetBytes(source)));
    }
}
