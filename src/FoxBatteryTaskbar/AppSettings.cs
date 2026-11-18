using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FoxBatteryTaskbar;

/// <summary>
/// User settings stored in %APPDATA%\FoxBatteryTaskbar\settings.json.
/// The API key is stored separately, encrypted with Windows DPAPI (current user only).
/// </summary>
public sealed class AppSettings
{
    public const string ApiKeyEnvironmentVariable = "FOXESS_API_KEY";

    public static string Directory { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FoxBatteryTaskbar");

    static string SettingsPath => Path.Combine(Directory, "settings.json");
    static string KeyPath => Path.Combine(Directory, "apikey.bin");

    static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>Fox Cloud base URL. Europe: https://www.foxesscloud.com, US: https://portal.foxesscloud.us</summary>
    public string BaseUrl { get; set; } = "https://www.foxesscloud.com";

    /// <summary>Inverter serial number. Left empty, the first device on the account is used.</summary>
    public string? DeviceSerial { get; set; }

    /// <summary>How often to poll Fox Cloud. Fox data only updates every few minutes and the API is rate limited.</summary>
    public int PollMinutes { get; set; } = 5;

    public const int MinPollMinutes = 2;
    public const int MaxPollMinutes = 24 * 60;

    /// <summary><see cref="PollMinutes"/> clamped to a safe range, whatever is in settings.json.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public int EffectivePollMinutes => Math.Clamp(PollMinutes, MinPollMinutes, MaxPollMinutes);

    public static AppSettings Load() => LoadFrom(SettingsPath);

    internal static AppSettings LoadFrom(string path)
    {
        try
        {
            if (File.Exists(path))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path)) ?? new AppSettings();
        }
        catch (JsonException)
        {
            // Corrupt settings: fall back to defaults rather than refusing to start.
        }
        return new AppSettings();
    }

    public void Save()
    {
        System.IO.Directory.CreateDirectory(Directory);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, JsonOptions));
    }

    /// <summary>API key from the FOXESS_API_KEY environment variable, else the encrypted key file.</summary>
    public static string? LoadApiKey()
    {
        var env = ApiKeyFromEnvironment();
        if (env is not null) return env;
        if (!File.Exists(KeyPath)) return null;
        try
        {
            var bytes = ProtectedData.Unprotect(File.ReadAllBytes(KeyPath), null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (Exception ex) when (ex is CryptographicException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public static string? ApiKeyFromEnvironment()
    {
        var env = Environment.GetEnvironmentVariable(ApiKeyEnvironmentVariable);
        return string.IsNullOrWhiteSpace(env) ? null : env.Trim();
    }

    public static void SaveApiKey(string apiKey)
    {
        System.IO.Directory.CreateDirectory(Directory);
        var bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(apiKey.Trim()), null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(KeyPath, bytes);
    }
}
