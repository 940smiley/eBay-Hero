[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')
Set-ProjectProcessEnvironment

$repo = Get-RepoRoot
Push-Location $repo
try {
    & dotnet build .\InventoryPhotoOps.sln -c Release
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    & dotnet test .\tests\InventoryPhotoOps.UiTests\InventoryPhotoOps.UiTests.csproj -c Release --no-build
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}

