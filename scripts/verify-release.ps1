[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')
Set-ProjectProcessEnvironment
$repo = Resolve-Path (Join-Path $PSScriptRoot '..')
Push-Location $repo
try {
    & (Join-Path $PSScriptRoot 'build.ps1')
    & (Join-Path $PSScriptRoot 'test.ps1')
    & (Join-Path $PSScriptRoot 'publish-portable.ps1')
    & (Join-Path $PSScriptRoot 'build-installer.ps1')
    $zip = Join-Path $repo 'artifacts\InventoryPhotoOps-win-x64-portable.zip'
    if (-not (Test-Path -LiteralPath $zip)) { throw "Release ZIP was not produced: $zip" }
    if (-not (Test-Path -LiteralPath ($zip + '.sha256'))) { throw "Release checksum was not produced: $zip.sha256" }
    $installer = Join-Path $repo 'artifacts\InventoryPhotoOps-script-installer.zip'
    if (-not (Test-Path -LiteralPath $installer)) { throw "Installer ZIP was not produced: $installer" }
    Write-Host "Release verification passed: $zip"
} finally {
    Pop-Location
}
