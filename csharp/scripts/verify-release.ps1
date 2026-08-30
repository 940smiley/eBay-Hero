[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')
Set-ProjectProcessEnvironment
$repo = Resolve-Path (Join-Path $PSScriptRoot '..')
$releaseRoot = if ($env:EA_RELEASE_ROOT) { $env:EA_RELEASE_ROOT } else { Join-Path $repo 'artifacts\release\windows' }
Push-Location $repo
try {
    foreach ($scriptName in @('build.ps1', 'test.ps1', 'publish-portable.ps1', 'build-installer.ps1')) {
        & (Join-Path $PSScriptRoot $scriptName)
        if ($LASTEXITCODE -ne 0) {
            throw "$scriptName failed with exit code $LASTEXITCODE."
        }
    }
    $zip = Join-Path $releaseRoot 'eBay-Assistance-win-x64-portable.zip'
    if (-not (Test-Path -LiteralPath $zip)) { throw "Release ZIP was not produced: $zip" }
    if (-not (Test-Path -LiteralPath ($zip + '.sha256'))) { throw "Release checksum was not produced: $zip.sha256" }
    $installer = Join-Path $releaseRoot 'eBay-Assistance-script-installer.zip'
    if (-not (Test-Path -LiteralPath $installer)) { throw "Installer ZIP was not produced: $installer" }
    Write-Host "Release verification passed: $zip"
} finally {
    Pop-Location
}
