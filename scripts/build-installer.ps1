[CmdletBinding()]
param([switch]$DryRun)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')
Set-ProjectProcessEnvironment
$repo = Resolve-Path (Join-Path $PSScriptRoot '..')
$artifacts = Join-Path $repo 'artifacts'
$installerRoot = Join-Path $artifacts 'installer'
$installerPath = Join-Path $installerRoot 'Install-InventoryPhotoOps.ps1'
$packagePath = Join-Path $artifacts 'InventoryPhotoOps-script-installer.zip'

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
    [string]$InstallRoot = "$env:LOCALAPPDATA\InventoryPhotoOps",
    [switch]$DesktopShortcut
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'InventoryPhotoOps-win-x64'
if (-not (Test-Path -LiteralPath $source)) { throw "Portable payload missing: $source" }
New-Item -ItemType Directory -Force -Path $InstallRoot | Out-Null
Copy-Item -LiteralPath (Join-Path $source '*') -Destination $InstallRoot -Recurse -Force
$shell = New-Object -ComObject WScript.Shell
$programs = [Environment]::GetFolderPath('Programs')
$shortcutDir = Join-Path $programs 'eBay-Hero'
New-Item -ItemType Directory -Force -Path $shortcutDir | Out-Null
$shortcut = $shell.CreateShortcut((Join-Path $shortcutDir 'eBay-Hero.lnk'))
$shortcut.TargetPath = Join-Path $InstallRoot 'InventoryPhotoOps.App.exe'
$shortcut.Arguments = '--allow-live'
$shortcut.WorkingDirectory = $InstallRoot
$shortcut.Save()
$control = $shell.CreateShortcut((Join-Path $shortcutDir 'eBay-Hero Control Center.lnk'))
$control.TargetPath = Join-Path $InstallRoot 'eBay-Hero-ControlCenter.bat'
$control.WorkingDirectory = $InstallRoot
$control.Save()
$demo = $shell.CreateShortcut((Join-Path $shortcutDir 'eBay-Hero Demo.lnk'))
$demo.TargetPath = Join-Path $InstallRoot 'InventoryPhotoOps.App.exe'
$demo.WorkingDirectory = $InstallRoot
$demo.Save()
if ($DesktopShortcut) {
    $desktop = [Environment]::GetFolderPath('DesktopDirectory')
    $desktopShortcut = $shell.CreateShortcut((Join-Path $desktop 'eBay-Hero.lnk'))
    $desktopShortcut.TargetPath = Join-Path $InstallRoot 'InventoryPhotoOps.App.exe'
    $desktopShortcut.Arguments = '--allow-live'
    $desktopShortcut.WorkingDirectory = $InstallRoot
    $desktopShortcut.Save()
}
Write-Host "Installed eBay-Hero to $InstallRoot"
'@ | Set-Content -LiteralPath $installerPath -Encoding UTF8
    $portableRoot = Join-Path $artifacts 'publish\InventoryPhotoOps-win-x64'
    Copy-Item -LiteralPath $portableRoot -Destination (Join-Path $installerRoot 'InventoryPhotoOps-win-x64') -Recurse -Force
    if (Test-Path -LiteralPath $packagePath) { Remove-Item -LiteralPath $packagePath -Force }
    Compress-Archive -Path (Join-Path $installerRoot '*') -DestinationPath $packagePath -Force
    $hash = Get-CompatibleFileHash -Algorithm SHA256 -LiteralPath $packagePath
    Set-Content -LiteralPath ($packagePath + '.sha256') -Value "$($hash.Hash)  $(Split-Path -Leaf $packagePath)" -Encoding ASCII
    Write-Host "Installer package: $packagePath"
} finally {
    Pop-Location
}
