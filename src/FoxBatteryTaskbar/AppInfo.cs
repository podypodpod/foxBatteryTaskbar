namespace FoxBatteryTaskbar;

static class AppInfo
{
    public const string RepositoryUrl = "https://github.com/podypodpod/foxBatteryTaskbar";
    public const string ReleasesUrl = RepositoryUrl + "/releases";

    /// <summary>Version from the project file, e.g. "0.1.1".</summary>
    public static string Version { get; } =
        typeof(AppInfo).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
}
