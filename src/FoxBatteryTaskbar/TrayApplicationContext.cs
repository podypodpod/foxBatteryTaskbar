using Microsoft.Win32;

namespace FoxBatteryTaskbar;

/// <summary>Owns the tray icon, its menu and the polling timer.</summary>
public sealed class TrayApplicationContext : ApplicationContext
{
    const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunValueName = "FoxBatteryTaskbar";
    // NotifyIcon.Text throws if too long (63 chars on older .NET); stay safely under that.
    const int MaxTooltipLength = 63;

    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };

    readonly NotifyIcon tray = new();
    readonly System.Windows.Forms.Timer timer = new();
    readonly ToolStripMenuItem startupItem;
    readonly AppSettings settings = AppSettings.Load();
    Icon? currentIcon;
    bool refreshing;

    public TrayApplicationContext()
    {
        startupItem = new ToolStripMenuItem("Start with Windows", null, (_, _) => ToggleStartup())
        {
            Checked = IsStartupEnabled(),
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Refresh now", null, async (_, _) => await RefreshAsync());
        menu.Items.Add("Set API key…", null, async (_, _) => { if (PromptForApiKey()) await RefreshAsync(); });
        menu.Items.Add("Open Fox Cloud", null, (_, _) => ApiKeyDialog.OpenUrl(settings.BaseUrl));
        menu.Items.Add("Open settings folder", null, (_, _) => OpenSettingsFolder());
        menu.Items.Add(startupItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());

        tray.ContextMenuStrip = menu;
        tray.DoubleClick += async (_, _) => await RefreshAsync();
        SetIcon(TrayIconRenderer.Render("…", TrayIconRenderer.Neutral));
        SetTooltip("Fox battery: loading…");
        tray.Visible = true;

        timer.Interval = (int)TimeSpan.FromMinutes(Math.Max(1, settings.PollMinutes)).TotalMilliseconds;
        timer.Tick += async (_, _) => await RefreshAsync();
        timer.Start();

        _ = RefreshAsync();
    }

    async Task RefreshAsync()
    {
        if (refreshing) return;
        refreshing = true;
        try
        {
            var apiKey = AppSettings.LoadApiKey();
            if (apiKey is null)
            {
                ShowError("no API key set (right-click → Set API key…)");
                if (PromptForApiKey()) apiKey = AppSettings.LoadApiKey();
                if (apiKey is null) return;
            }

            var client = new FoxCloudClient(Http, settings.BaseUrl, apiKey);
            if (string.IsNullOrWhiteSpace(settings.DeviceSerial))
            {
                settings.DeviceSerial = await client.GetFirstDeviceSerialAsync();
                settings.Save();
            }

            ShowReading(await client.GetReadingAsync(settings.DeviceSerial));
        }
        catch (Exception ex) when (ex is FoxCloudException or HttpRequestException or TaskCanceledException)
        {
            ShowError(ex.Message);
        }
        finally
        {
            refreshing = false;
        }
    }

    void ShowReading(BatteryReading r)
    {
        int soc = (int)Math.Round(r.StateOfCharge);
        bool charging = r.ChargePower > 0.05;
        SetIcon(TrayIconRenderer.Render(soc.ToString(), TrayIconRenderer.ColorFor(soc), soc, charging));

        string state = charging ? $"charging {r.ChargePower:0.0}kW"
            : r.DischargePower > 0.05 ? $"using {r.DischargePower:0.0}kW"
            : "idle";
        // Grid: + importing, - exporting
        double grid = r.GridExportPower > 0.05 ? -r.GridExportPower : r.GridImportPower;

        SetTooltip(
            $"{soc}% {state}\n" +
            $"Solar {r.SolarPower:0.0} House {r.HousePower:0.0} Grid {grid:+0.0;-0.0;0.0}kW\n" +
            $"@{r.FetchedAt:HH:mm}");
    }

    void ShowError(string message)
    {
        SetIcon(TrayIconRenderer.Render("!", TrayIconRenderer.Bad));
        SetTooltip("Fox battery: " + message);
    }

    void SetIcon(Icon icon)
    {
        var old = currentIcon;
        tray.Icon = currentIcon = icon;
        TrayIconRenderer.Destroy(old);
    }

    void SetTooltip(string text) =>
        tray.Text = text.Length > MaxTooltipLength ? text[..MaxTooltipLength] : text;

    bool PromptForApiKey()
    {
        using var dialog = new ApiKeyDialog();
        if (dialog.ShowDialog() != DialogResult.OK || dialog.ApiKey.Length == 0) return false;
        AppSettings.SaveApiKey(dialog.ApiKey);
        settings.DeviceSerial = null; // a new key may belong to a different account
        settings.Save();
        return true;
    }

    static void OpenSettingsFolder()
    {
        Directory.CreateDirectory(AppSettings.Directory);
        ApiKeyDialog.OpenUrl(AppSettings.Directory);
    }

    static bool IsStartupEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        return key?.GetValue(RunValueName) is not null;
    }

    void ToggleStartup()
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (IsStartupEnabled()) key.DeleteValue(RunValueName, throwOnMissingValue: false);
        else key.SetValue(RunValueName, $"\"{Environment.ProcessPath}\"");
        startupItem.Checked = IsStartupEnabled();
    }

    protected override void ExitThreadCore()
    {
        timer.Dispose();
        tray.Visible = false;
        tray.Dispose();
        TrayIconRenderer.Destroy(currentIcon);
        base.ExitThreadCore();
    }
}
