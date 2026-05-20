[CmdletBinding()]
param(
    [string]$SourceDirectory = "",
    [string]$InstallDirectory = (Join-Path $env:LOCALAPPDATA "Programs\SubscriptionTracker"),
    [string]$StartMenuShortcutPath = (Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\Subscription Tracker.lnk"),
    [string]$DesktopShortcutPath = "",
    [switch]$CreateDesktopShortcut,
    [switch]$SkipStartMenuShortcut,
    [switch]$SkipRegistryEntry,
    [switch]$NoLaunch
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($SourceDirectory)) {
    $SourceDirectory = Join-Path $PSScriptRoot "app"
}

if ([string]::IsNullOrWhiteSpace($DesktopShortcutPath)) {
    $desktopDirectory = [Environment]::GetFolderPath("Desktop")
    if (-not [string]::IsNullOrWhiteSpace($desktopDirectory)) {
        $DesktopShortcutPath = Join-Path $desktopDirectory "Subscription Tracker.lnk"
    }
}

function New-Shortcut {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ShortcutPath,
        [Parameter(Mandatory = $true)]
        [string]$TargetPath,
        [string]$Arguments = "",
        [string]$WorkingDirectory = "",
        [string]$IconLocation = ""
    )

    $directory = Split-Path -Parent $ShortcutPath
    if (-not [string]::IsNullOrWhiteSpace($directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($ShortcutPath)
    $shortcut.TargetPath = $TargetPath
    $shortcut.Arguments = $Arguments
    $shortcut.WorkingDirectory = $WorkingDirectory
    if (-not [string]::IsNullOrWhiteSpace($IconLocation)) {
        $shortcut.IconLocation = $IconLocation
    }

    $shortcut.Save()
}

function Set-UninstallRegistryEntry {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ApplicationDirectory
    )

    $registryPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\SubscriptionTracker"
    $uninstallScript = Join-Path $ApplicationDirectory "Uninstall-SubscriptionTracker.ps1"
    $uninstallCommand = "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$uninstallScript`""
    $iconPath = Join-Path $ApplicationDirectory "SubscriptionTracker.Wpf.exe"

    New-Item -Path $registryPath -Force | Out-Null
    New-ItemProperty -Path $registryPath -Name "DisplayName" -Value "Subscription Tracker" -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $registryPath -Name "DisplayVersion" -Value "0.1.1" -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $registryPath -Name "Publisher" -Value "Baaltazamon" -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $registryPath -Name "InstallLocation" -Value $ApplicationDirectory -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $registryPath -Name "DisplayIcon" -Value $iconPath -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $registryPath -Name "UninstallString" -Value $uninstallCommand -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $registryPath -Name "NoModify" -Value 1 -PropertyType DWord -Force | Out-Null
    New-ItemProperty -Path $registryPath -Name "NoRepair" -Value 1 -PropertyType DWord -Force | Out-Null
}

if (-not (Test-Path $SourceDirectory)) {
    throw "Application files were not found. Expected app payload directory: $SourceDirectory"
}

New-Item -ItemType Directory -Path $InstallDirectory -Force | Out-Null
Copy-Item -Path (Join-Path $SourceDirectory "*") -Destination $InstallDirectory -Recurse -Force

$localUninstallScript = Join-Path $PSScriptRoot "Uninstall-SubscriptionTracker.ps1"
if (Test-Path $localUninstallScript) {
    Copy-Item -Path $localUninstallScript -Destination (Join-Path $InstallDirectory "Uninstall-SubscriptionTracker.ps1") -Force
}

$licenseCandidates = @(
    (Join-Path $PSScriptRoot "LICENSE"),
    (Join-Path $PSScriptRoot "LICENSE.txt"),
    (Join-Path (Split-Path $PSScriptRoot -Parent) "LICENSE")
)

$licensePath = $licenseCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not [string]::IsNullOrWhiteSpace($licensePath)) {
    Copy-Item -Path $licensePath -Destination (Join-Path $InstallDirectory "LICENSE.txt") -Force
}

$exePath = Join-Path $InstallDirectory "SubscriptionTracker.Wpf.exe"
if (-not (Test-Path $exePath)) {
    throw "Installed executable was not found: $exePath"
}

if (-not $SkipStartMenuShortcut) {
    New-Shortcut -ShortcutPath $StartMenuShortcutPath -TargetPath $exePath -WorkingDirectory $InstallDirectory -IconLocation $exePath
}

if ($CreateDesktopShortcut) {
    if ([string]::IsNullOrWhiteSpace($DesktopShortcutPath)) {
        throw "Desktop shortcut path could not be resolved on this machine."
    }

    New-Shortcut -ShortcutPath $DesktopShortcutPath -TargetPath $exePath -WorkingDirectory $InstallDirectory -IconLocation $exePath
}

if (-not $SkipRegistryEntry) {
    Set-UninstallRegistryEntry -ApplicationDirectory $InstallDirectory
}

if (-not $NoLaunch) {
    Start-Process -FilePath $exePath -WorkingDirectory $InstallDirectory
}

Write-Host "Subscription Tracker was installed to $InstallDirectory"
