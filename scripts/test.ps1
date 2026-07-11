[CmdletBinding()]
param([switch]$VerboseBuild)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')
Set-ProjectProcessEnvironment
$repo = Resolve-Path (Join-Path $PSScriptRoot '..')
Push-Location $repo
try {
    $resultsDirectory = if ($env:EA_BUILD_ROOT) { Join-Path $env:EA_BUILD_ROOT 'TestResults' } else { 'TestResults' }
    New-Item -ItemType Directory -Force -Path $resultsDirectory | Out-Null
    $args = @('test', 'InventoryPhotoOps.sln', '-c', 'Release', '--logger', 'trx', '--results-directory', $resultsDirectory)
    if ($VerboseBuild -or $VerbosePreference -eq 'Continue') { $args += '-v:normal' }
    & dotnet @args
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
} finally {
    Pop-Location
}
