# Fox Battery Taskbar

A tiny Windows tray app that shows your **Fox ESS** home battery's state of charge in the taskbar.

- The icon shows the battery percentage, coloured **green** (≥ 50%), **amber** (≥ 20%) or **red**, with a fill bar along the bottom.
- A green corner marker means the battery is **charging**. `!` means something went wrong; hover for details.
- The tooltip shows charge/discharge power, solar, house load and grid import/export.
- It polls Fox Cloud every 5 minutes by default. Double-click the icon to refresh now.

> Not affiliated with or endorsed by Fox ESS. It uses the public [Fox ESS Cloud Open API](https://www.foxesscloud.com/public/i18n/en/OpenApiDocument.html).

## Install

1. Download `FoxBatteryTaskbar.exe` from the [latest release](../../releases/latest). It's a single self-contained file, so you don't need to install .NET.
2. Run it. On first run it asks for your **Fox Cloud API key**.
3. Optional: right-click the icon and tick **Start with Windows**.

Windows 11 hides new tray icons. To keep it visible, drag it from the `^` overflow onto the taskbar. Or go to **Settings → Personalization → Taskbar → Other system tray icons** and switch **Fox Battery Taskbar** on.

### Getting an API key

1. Sign in at [foxesscloud.com](https://www.foxesscloud.com/login).
2. Open your profile (top right) → **User Profile** → **API Management**.
3. Click **Generate API key** and paste it into the app.

## Configuration

Settings live in `%APPDATA%\FoxBatteryTaskbar\`. Use **Open settings folder** in the tray menu to get there.

| File | Purpose |
|---|---|
| `apikey.bin` | Your API key, encrypted with Windows DPAPI so only your Windows account can read it. |
| `settings.json` | Optional settings (below). Created automatically after the first successful read. |

```json
{
  "BaseUrl": "https://www.foxesscloud.com",
  "DeviceSerial": null,
  "PollMinutes": 5
}
```

- **BaseUrl**: `https://www.foxesscloud.com` (Europe/default) or `https://portal.foxesscloud.us` (US).
- **DeviceSerial**: your inverter serial number. If it's empty, the app uses the first device on your account.
- **PollMinutes**: how often to refresh. Fox Cloud data only updates every few minutes and the API has a daily call limit, so keep this ≥ 2.

You can also supply the key with the `FOXESS_API_KEY` environment variable, which takes priority over the stored key.

## Build from source

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet build
```

Run a one-off check that prints a reading to the terminal:

```bash
dotnet run --project src/FoxBatteryTaskbar -- --test
```

Produce a single self-contained exe in `publish/`:

```bash
dotnet publish src/FoxBatteryTaskbar -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o publish
```

Pushing a tag such as `v0.1.0` builds the exe and attaches it to a GitHub release (see [`.github/workflows/build.yml`](.github/workflows/build.yml)).

## How it works

- [`FoxCloudClient.cs`](src/FoxBatteryTaskbar/FoxCloudClient.cs) calls `POST /op/v0/device/real/query`. Each request is signed with `md5(path + "\r\n" + apiKey + "\r\n" + timestampMs)`, where `\r\n` is the literal four characters, as Fox's docs specify.
- [`TrayIconRenderer.cs`](src/FoxBatteryTaskbar/TrayIconRenderer.cs) draws the icon at the system tray size, so it stays sharp on high-DPI screens.
- [`TrayApplicationContext.cs`](src/FoxBatteryTaskbar/TrayApplicationContext.cs) handles the tray icon, menu, polling and the "Start with Windows" `HKCU\...\Run` entry.

## Licence

[MIT](LICENSE)
