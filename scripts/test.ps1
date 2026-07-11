[CmdletBinding()]
param([switch]$VerboseBuild)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')
Set-ProjectProcessEnvironment
$repo = Resolve-Path (Join-Path $PSScriptRoot '..')
Push-Location $repo
try {
    $args = @('test', 'InventoryPhotoOps.sln', '-c', 'Release', '--logger', 'trx', '--results-directory', 'TestResults')
    if ($VerboseBuild -or $VerbosePreference -eq 'Continue') { $args += '-v:normal' }
    & dotnet @args
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
} finally {
    Pop-Location
}
