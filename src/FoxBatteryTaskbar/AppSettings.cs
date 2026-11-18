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

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath)) ?? new AppSettings();
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
        var env = Environment.GetEnvironmentVariable(ApiKeyEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(env)) return env.Trim();
        if (!File.Exists(KeyPath)) return null;
        try
        {
            var bytes = ProtectedData.Unprotect(File.ReadAllBytes(KeyPath), null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    public static void SaveApiKey(string apiKey)
    {
        System.IO.Directory.CreateDirectory(Directory);
        var bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(apiKey.Trim()), null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(KeyPath, bytes);
    }
}
