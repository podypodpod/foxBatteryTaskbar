# Changelog

All notable changes to this project are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project uses [Semantic Versioning](https://semver.org/).

## [0.1.1] - 2026-11-18

### Added
- App icon for the exe.
- Menu shows the version, plus an "About / check for updates" link.
- After waking from sleep, the icon greys out until fresh data arrives (about 15 s later).
- Colours adapt to a light taskbar, and update when you switch Windows between light and dark.
- ARM64 build (`FoxBatteryTaskbar-arm64.exe`) for Windows on ARM.
- Build provenance attestation on releases (`gh attestation verify`).

### Changed
- Tooltip numbers use your regional decimal separator.

## [0.1.0] - 2026-11-18

### Added
- First release: tray icon showing Fox ESS battery state of charge, with charge/discharge, solar, house and grid power in the tooltip.
- API key stored encrypted with Windows DPAPI. Optional `FOXESS_API_KEY` environment variable.
- The API key is only sent over HTTPS to Fox Cloud hosts (EU and US).
- `--test` command-line self-check.
- Single-file x64 exe with `SHA256SUMS.txt`.

[0.1.1]: https://github.com/podypodpod/foxBatteryTaskbar/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/podypodpod/foxBatteryTaskbar/releases/tag/v0.1.0
