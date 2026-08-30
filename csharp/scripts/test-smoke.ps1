[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')
Set-ProjectProcessEnvironment

$repo = Get-RepoRoot
Push-Location $repo
try {
    & dotnet build .\eBayHero.sln -c Release
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    & dotnet test .\tests\eBayHero.UiTests\eBayHero.UiTests.csproj -c Release --no-build
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}


