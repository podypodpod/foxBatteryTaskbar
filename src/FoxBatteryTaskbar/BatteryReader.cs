namespace FoxBatteryTaskbar;

/// <summary>
/// The one place that turns settings + API key into a reading: picks the device
/// (discovering and remembering it on first use) and fetches its latest data.
/// Used by both the tray app and the --test self-check.
/// </summary>
public static class BatteryReader
{
    public static async Task<BatteryReading> ReadAsync(
        HttpClient http, AppSettings settings, string apiKey, Action? onSettingsChanged = null, CancellationToken ct = default)
    {
        var client = new FoxCloudClient(http, settings.EffectiveBaseUrl, apiKey);
        if (string.IsNullOrWhiteSpace(settings.DeviceSerial))
        {
            settings.DeviceSerial = await client.GetFirstDeviceSerialAsync(ct);
            onSettingsChanged?.Invoke();
        }
        return await client.GetReadingAsync(settings.DeviceSerial, ct);
    }

    /// <summary>Largest API response accepted; real responses are a few KB.</summary>
    public const int MaxResponseBytes = 1024 * 1024;

    /// <summary>HttpClient used for Fox Cloud: short timeout and a cap on response size.</summary>
    public static HttpClient CreateHttpClient() => new()
    {
        Timeout = TimeSpan.FromSeconds(20),
        MaxResponseContentBufferSize = MaxResponseBytes,
    };

    public static string MissingKeyMessage =>
        $"no API key set (right-click → Set API key…, or set {AppSettings.ApiKeyEnvironmentVariable})";
}
