[CmdletBinding()]
param([switch]$DryRun)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')
Set-ProjectProcessEnvironment

$repo = Resolve-Path (Join-Path $PSScriptRoot '..')
$releaseRoot = if ($env:EA_RELEASE_ROOT) { $env:EA_RELEASE_ROOT } else { Join-Path $repo 'artifacts\release\windows' }
$installerRoot = Join-Path $releaseRoot 'installer'
$installerPath = Join-Path $installerRoot 'Install-eBay-Assistance.ps1'
$packagePath = Join-Path $releaseRoot 'eBay-Assistance-script-installer.zip'

if ($DryRun) {
    Write-Host "Would create script installer package at $packagePath"
    exit 0
}

Push-Location $repo
try {
    & (Join-Path $PSScriptRoot 'publish-portable.ps1')
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    New-Item -ItemType Directory -Force -Path $installerRoot | Out-Null
    @'
[CmdletBinding()]
param(
    [string]$InstallRoot = "$env:LOCALAPPDATA\EbayAssistance",
    [switch]$DesktopShortcut
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'eBay-Assistance-win-x64'
if (-not (Test-Path -LiteralPath $source)) { throw "Portable payload missing: $source" }
New-Item -ItemType Directory -Force -Path $InstallRoot | Out-Null
Copy-Item -LiteralPath (Join-Path $source '*') -Destination $InstallRoot -Recurse -Force

$shell = New-Object -ComObject WScript.Shell
$programs = [Environment]::GetFolderPath('Programs')
$shortcutDir = Join-Path $programs 'eBay Assistance'
New-Item -ItemType Directory -Force -Path $shortcutDir | Out-Null

$shortcut = $shell.CreateShortcut((Join-Path $shortcutDir 'eBay Assistance.lnk'))
$shortcut.TargetPath = Join-Path $InstallRoot 'InventoryPhotoOps.App.exe'
$shortcut.WorkingDirectory = $InstallRoot
$shortcut.Save()

$production = $shell.CreateShortcut((Join-Path $shortcutDir 'eBay Assistance Production.lnk'))
$production.TargetPath = Join-Path $InstallRoot 'InventoryPhotoOps.App.exe'
$production.Arguments = '--allow-live'
$production.WorkingDirectory = $InstallRoot
$production.Save()

$control = $shell.CreateShortcut((Join-Path $shortcutDir 'eBay Assistance Control Center.lnk'))
$control.TargetPath = 'powershell.exe'
$control.Arguments = '-NoProfile -ExecutionPolicy Bypass -File "' + (Join-Path $InstallRoot 'scripts\InventoryPhotoOps-ControlCenter.ps1') + '"'
$control.WorkingDirectory = $InstallRoot
$control.Save()

if ($DesktopShortcut) {
    $desktop = [Environment]::GetFolderPath('DesktopDirectory')
    $desktopShortcut = $shell.CreateShortcut((Join-Path $desktop 'eBay Assistance.lnk'))
    $desktopShortcut.TargetPath = Join-Path $InstallRoot 'InventoryPhotoOps.App.exe'
    $desktopShortcut.WorkingDirectory = $InstallRoot
    $desktopShortcut.Save()
}

Write-Host "Installed eBay Assistance to $InstallRoot"
'@ | Set-Content -LiteralPath $installerPath -Encoding UTF8

    $portableRoot = Join-Path $releaseRoot 'publish\eBay-Assistance-win-x64'
    $installerPayload = Join-Path $installerRoot 'eBay-Assistance-win-x64'
    if (Test-Path -LiteralPath $installerPayload) { Remove-Item -LiteralPath $installerPayload -Recurse -Force }
    Copy-Item -LiteralPath $portableRoot -Destination $installerPayload -Recurse -Force

    if (Test-Path -LiteralPath $packagePath) { Remove-Item -LiteralPath $packagePath -Force }
    Compress-Archive -Path (Join-Path $installerRoot '*') -DestinationPath $packagePath -Force
    $hash = Get-CompatibleFileHash -Algorithm SHA256 -LiteralPath $packagePath
    Set-Content -LiteralPath ($packagePath + '.sha256') -Value "$($hash.Hash)  $(Split-Path -Leaf $packagePath)" -Encoding ASCII
    Write-Host "Installer package: $packagePath"
}
finally {
    Pop-Location
}

