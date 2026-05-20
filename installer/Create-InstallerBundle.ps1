[CmdletBinding()]
param(
    [string]$Version = "v0.1.1",
    [string]$PublishDirectory = "",
    [string]$OutputDirectory = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent
$PublishDirectory = if ([string]::IsNullOrWhiteSpace($PublishDirectory)) { Join-Path $repoRoot "artifacts\publish\wpf" } else { $PublishDirectory }
$OutputDirectory = if ([string]::IsNullOrWhiteSpace($OutputDirectory)) { Join-Path $repoRoot "artifacts\release" } else { $OutputDirectory }
$stagingRoot = Join-Path $OutputDirectory "staging"
$bundleName = "SubscriptionTracker-$Version-win-x64-installer"
$bundleDirectory = Join-Path $stagingRoot $bundleName
$zipPath = Join-Path $OutputDirectory ($bundleName + ".zip")

if (-not (Test-Path $PublishDirectory)) {
    throw "Publish output was not found: $PublishDirectory"
}

Remove-Item -LiteralPath $bundleDirectory -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path (Join-Path $bundleDirectory "app") -Force | Out-Null

Copy-Item -Path (Join-Path $PublishDirectory "*") -Destination (Join-Path $bundleDirectory "app") -Recurse -Force
Copy-Item -Path (Join-Path $PSScriptRoot "Install-SubscriptionTracker.ps1") -Destination $bundleDirectory -Force
Copy-Item -Path (Join-Path $PSScriptRoot "Uninstall-SubscriptionTracker.ps1") -Destination $bundleDirectory -Force
Copy-Item -Path (Join-Path $repoRoot "LICENSE") -Destination (Join-Path $bundleDirectory "LICENSE") -Force

$readmePath = Join-Path $bundleDirectory "INSTALL.txt"
@"
Subscription Tracker Installer Bundle

1. Run Install-SubscriptionTracker.ps1
2. The application will be copied to `%LocalAppData%\Programs\SubscriptionTracker`
3. A Start Menu shortcut will be created automatically

Optional:
- pass -CreateDesktopShortcut to add a desktop shortcut
- run Uninstall-SubscriptionTracker.ps1 later to remove the app
"@ | Set-Content -Path $readmePath -Encoding UTF8

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
Compress-Archive -Path (Join-Path $bundleDirectory "*") -DestinationPath $zipPath -Force

Write-Host "Installer bundle created: $zipPath"
