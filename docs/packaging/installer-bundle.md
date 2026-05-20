# Installer Bundle

## Why this approach

This repository does not currently build a true `.msix` package in the local environment because the required desktop packaging toolchain is not installed:

- Windows SDK / `makeappx.exe`
- `signtool.exe`
- Visual Studio desktop packaging tools

As a practical release-ready fallback, the project now includes a local per-user installer bundle.

## What the installer does

- copies the published app to `%LocalAppData%\Programs\SubscriptionTracker`
- creates a Start Menu shortcut
- optionally creates a desktop shortcut
- registers an uninstall entry under:
  - `HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\SubscriptionTracker`

## Files

- `installer\Install-SubscriptionTracker.ps1`
- `installer\Uninstall-SubscriptionTracker.ps1`
- `installer\Create-InstallerBundle.ps1`

## Build the installer bundle

```powershell
.\installer\Create-InstallerBundle.ps1 -Version v0.1.1
```

Output:

```text
artifacts\release\SubscriptionTracker-v0.1.1-win-x64-installer.zip
```

## Manual install

Extract the installer ZIP and run:

```powershell
.\Install-SubscriptionTracker.ps1
```

Optional desktop shortcut:

```powershell
.\Install-SubscriptionTracker.ps1 -CreateDesktopShortcut
```

## Uninstall

Run:

```powershell
.\Uninstall-SubscriptionTracker.ps1
```

To also remove the SQLite database and settings:

```powershell
.\Uninstall-SubscriptionTracker.ps1 -RemoveUserData
```
