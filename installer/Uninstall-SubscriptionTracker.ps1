[CmdletBinding()]
param(
    [string]$InstallDirectory = (Split-Path -Parent $PSCommandPath),
    [string]$StartMenuShortcutPath = (Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\Subscription Tracker.lnk"),
    [string]$DesktopShortcutPath = "",
    [string]$UserDataDirectory = (Join-Path $env:LOCALAPPDATA "SubscriptionTracker"),
    [switch]$RemoveUserData,
    [switch]$SkipShortcuts,
    [switch]$SkipRegistryEntry
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($DesktopShortcutPath)) {
    $desktopDirectory = [Environment]::GetFolderPath("Desktop")
    if (-not [string]::IsNullOrWhiteSpace($desktopDirectory)) {
        $DesktopShortcutPath = Join-Path $desktopDirectory "Subscription Tracker.lnk"
    }
}

function Remove-PathIfExists {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (Test-Path $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
}

if (-not $SkipShortcuts) {
    if (Test-Path $StartMenuShortcutPath) {
        Remove-Item -LiteralPath $StartMenuShortcutPath -Force
    }

    if (-not [string]::IsNullOrWhiteSpace($DesktopShortcutPath) -and (Test-Path $DesktopShortcutPath)) {
        Remove-Item -LiteralPath $DesktopShortcutPath -Force
    }
}

if (-not $SkipRegistryEntry) {
    $registryPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\SubscriptionTracker"
    if (Test-Path $registryPath) {
        Remove-Item -LiteralPath $registryPath -Recurse -Force
    }
}

if ($RemoveUserData) {
    Remove-PathIfExists -Path $UserDataDirectory
}

if (Test-Path $InstallDirectory) {
    $cleanupScript = Join-Path $env:TEMP ("SubscriptionTracker.Cleanup." + [guid]::NewGuid().ToString("N") + ".ps1")
    @"
Start-Sleep -Seconds 1
if (Test-Path '$InstallDirectory') {
    Remove-Item -LiteralPath '$InstallDirectory' -Recurse -Force
}
Remove-Item -LiteralPath '$cleanupScript' -Force
"@ | Set-Content -Path $cleanupScript -Encoding UTF8

    Start-Process -FilePath "powershell.exe" `
        -ArgumentList "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "`"$cleanupScript`"" `
        -WindowStyle Hidden
}

Write-Host "Subscription Tracker uninstall has been scheduled."
