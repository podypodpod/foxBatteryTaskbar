# Security

## Reporting a vulnerability

Please report security issues privately through GitHub: **Security → Report a vulnerability** on this repository. Don't open a public issue.

## How your API key is handled

- **What the key can do.** A Fox Cloud API key gives full API access to your account. That includes changing battery charge and discharge schedules, not just reading data. This app only reads, but treat the key like a password.
- **Storage.** The key is stored in `%APPDATA%\FoxBatteryTaskbar\apikey.bin`, encrypted with Windows DPAPI for your user account. Other Windows users and offline copies of the file can't read it. Any program running as *you* can decrypt it, which is the normal limit of per-user secret storage on Windows. If you think your account or PC is compromised, revoke the key in Fox Cloud (User Profile → API Management) and generate a new one.
- **Environment variable.** If `FOXESS_API_KEY` is set it takes priority. Environment variables are visible to every process you run.
- **Where it is sent.** The key is only ever sent over HTTPS to `www.foxesscloud.com` or `portal.foxesscloud.us`. Any other `BaseUrl` in `settings.json` (another host, `http://`, a file path) is ignored and the default is used.
- **Logs.** The app doesn't log the key or write it anywhere else.

## Verifying a download

Each release includes `SHA256SUMS.txt`. Check your download against it:

```powershell
Get-FileHash .\FoxBatteryTaskbar.exe -Algorithm SHA256
```

Public releases also carry a GitHub build provenance attestation, which proves the exe was built from this repository by its CI workflow:

```bash
gh attestation verify FoxBatteryTaskbar.exe --repo podypodpod/foxBatteryTaskbar
```

The exe isn't code-signed, so Windows SmartScreen may warn on first run.
