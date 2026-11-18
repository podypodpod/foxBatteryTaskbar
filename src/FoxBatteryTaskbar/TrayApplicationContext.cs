using Microsoft.Win32;

namespace FoxBatteryTaskbar;

/// <summary>Owns the tray icon, its menu and the polling timer.</summary>
public sealed class TrayApplicationContext : ApplicationContext
{
    const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunValueName = "FoxBatteryTaskbar";

    /// <summary>After waking from sleep, wait for the network before refreshing.</summary>
    static readonly TimeSpan ResumeDelay = TimeSpan.FromSeconds(15);

    static readonly HttpClient Http = BatteryReader.CreateHttpClient();

    readonly NotifyIcon tray = new();
    readonly System.Windows.Forms.Timer pollTimer = new();
    readonly System.Windows.Forms.Timer resumeTimer = new();
    readonly ToolStripMenuItem startupItem;
    readonly AppSettings settings = AppSettings.Load();
    TrayPalette palette = TrayPalette.ForCurrentTheme();
    Icon? currentIcon;
    Func<TrayPalette, Icon> renderCurrent = p => TrayIconRenderer.Render("…", p.Stale, p);
    BatteryReading? lastReading;
    bool refreshing;

    public TrayApplicationContext()
    {
        startupItem = new ToolStripMenuItem("Start with Windows", null, (_, _) => ToggleStartup())
        {
            Checked = IsStartupEnabled(),
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add(new ToolStripMenuItem($"Fox Battery Taskbar {AppInfo.Version}") { Enabled = false });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Refresh now", null, async (_, _) => await RefreshAsync(promptIfNoKey: true));
        menu.Items.Add("Set API key…", null, async (_, _) => { if (PromptForApiKey()) await RefreshAsync(promptIfNoKey: false); });
        menu.Items.Add("Open Fox Cloud", null, (_, _) => Shell.Open(settings.EffectiveBaseUrl)); // validated https Fox URL only
        menu.Items.Add("Open settings folder", null, (_, _) => OpenSettingsFolder());
        menu.Items.Add(startupItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("About / check for updates", null, (_, _) => Shell.Open(AppInfo.ReleasesUrl));
        menu.Items.Add("Exit", null, (_, _) => ExitThread());

        tray.ContextMenuStrip = menu;
        tray.DoubleClick += async (_, _) => await RefreshAsync(promptIfNoKey: true);
        SetIcon(renderCurrent);
        tray.Text = TrayText.Fit("Fox battery: loading…");
        tray.Visible = true;

        // Redraw at the new size when display scaling changes, and in new colours when the theme changes.
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        // After sleep the shown value is old: mark it stale, then refresh once the network is back.
        SystemEvents.PowerModeChanged += OnPowerModeChanged;

        resumeTimer.Interval = (int)ResumeDelay.TotalMilliseconds;
        resumeTimer.Tick += async (_, _) => { resumeTimer.Stop(); await RefreshAsync(promptIfNoKey: false); };

        pollTimer.Interval = (int)TimeSpan.FromMinutes(settings.EffectivePollMinutes).TotalMilliseconds;
        // Timer ticks never prompt: a cancelled key dialog must not reappear every few minutes.
        pollTimer.Tick += async (_, _) => await RefreshAsync(promptIfNoKey: false);
        pollTimer.Start();

        _ = RefreshAsync(promptIfNoKey: true);
    }

    async Task RefreshAsync(bool promptIfNoKey)
    {
        if (refreshing) return;
        refreshing = true;
        try
        {
            var apiKey = AppSettings.LoadApiKey();
            if (apiKey is null)
            {
                ShowError(BatteryReader.MissingKeyMessage);
                if (!promptIfNoKey || !PromptForApiKey()) return;
                apiKey = AppSettings.LoadApiKey();
                if (apiKey is null) return;
            }

            ShowReading(await BatteryReader.ReadAsync(Http, settings, apiKey, settings.Save));
        }
        catch (Exception ex)
        {
            // This runs from async-void UI handlers: anything that escapes would crash the app.
            // Show it on the icon instead and try again on the next tick.
            ShowError(ex.Message);
        }
        finally
        {
            refreshing = false;
        }
    }

    void ShowReading(BatteryReading r, bool stale = false)
    {
        lastReading = r;
        int soc = (int)Math.Round(r.StateOfCharge);
        bool charging = !stale && TrayText.IsCharging(r);
        SetIcon(p => TrayIconRenderer.Render(soc.ToString(), stale ? p.Stale : p.ColorFor(soc), p, soc, charging));
        tray.Text = TrayText.ForReading(r, stale);
    }

    void ShowError(string message)
    {
        SetIcon(p => TrayIconRenderer.Render("!", p.Bad, p));
        tray.Text = TrayText.ForError(message);
    }

    void SetIcon(Func<TrayPalette, Icon> render)
    {
        renderCurrent = render;
        var old = currentIcon;
        tray.Icon = currentIcon = render(palette);
        TrayIconRenderer.Destroy(old);
    }

    void OnDisplaySettingsChanged(object? sender, EventArgs e) => SetIcon(renderCurrent);

    void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category != UserPreferenceCategory.General) return; // light/dark switches raise General
        var updated = TrayPalette.ForCurrentTheme();
        if (updated == palette) return;
        palette = updated;
        SetIcon(renderCurrent);
    }

    void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode != PowerModes.Resume) return;
        if (lastReading is not null) ShowReading(lastReading, stale: true);
        resumeTimer.Stop();
        resumeTimer.Start();
    }

    bool PromptForApiKey()
    {
        using var dialog = new ApiKeyDialog();
        if (dialog.ShowDialog() != DialogResult.OK || dialog.ApiKey.Length == 0) return false;
        AppSettings.SaveApiKey(dialog.ApiKey);
        settings.DeviceSerial = null; // a new key may belong to a different account
        settings.Save();

        if (AppSettings.ApiKeyFromEnvironment() is not null)
        {
            MessageBox.Show(
                $"The key was saved, but the {AppSettings.ApiKeyEnvironmentVariable} environment variable is set " +
                "and takes priority. Remove that variable to use the key you just entered.",
                "Fox Battery Taskbar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        return true;
    }

    static void OpenSettingsFolder()
    {
        Directory.CreateDirectory(AppSettings.Directory);
        Shell.Open(AppSettings.Directory);
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
        // SystemEvents are static: unsubscribe or they keep this object alive.
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        pollTimer.Dispose();
        resumeTimer.Dispose();
        tray.Visible = false;
        tray.Dispose();
        TrayIconRenderer.Destroy(currentIcon);
        base.ExitThreadCore();
    }
}
