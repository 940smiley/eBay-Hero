[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')
Set-ProjectProcessEnvironment

$repo = Get-RepoRoot
Push-Location $repo
try {
    & dotnet test .\tests\eBayHero.IntegrationTests\eBayHero.IntegrationTests.csproj -c Release
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}


