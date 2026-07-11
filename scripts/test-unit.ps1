[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')
Set-ProjectProcessEnvironment

$repo = Get-RepoRoot
Push-Location $repo
try {
    & dotnet test .\tests\InventoryPhotoOps.Core.Tests\InventoryPhotoOps.Core.Tests.csproj -c Release
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    & dotnet test .\tests\InventoryPhotoOps.Infrastructure.Tests\InventoryPhotoOps.Infrastructure.Tests.csproj -c Release
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}

