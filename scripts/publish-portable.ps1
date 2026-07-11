[CmdletBinding()]
param([switch]$DryRun)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')
Set-ProjectProcessEnvironment
$repo = Resolve-Path (Join-Path $PSScriptRoot '..')
$artifacts = Join-Path $repo 'artifacts'
$publishRoot = Join-Path $artifacts 'publish'
$portableRoot = Join-Path $publishRoot 'InventoryPhotoOps-win-x64'
$zipPath = Join-Path $artifacts 'InventoryPhotoOps-win-x64-portable.zip'

if ($DryRun) {
    Write-Host "Would publish portable app to $portableRoot"
    Write-Host "Would create $zipPath"
    exit 0
}

Push-Location $repo
try {
    New-Item -ItemType Directory -Force -Path $artifacts, $publishRoot | Out-Null
    if (Test-Path -LiteralPath $portableRoot) { Remove-Item -LiteralPath $portableRoot -Recurse -Force }
    & dotnet publish .\src\InventoryPhotoOps.App\InventoryPhotoOps.App.csproj -c Release -r win-x64 --self-contained true -o $portableRoot
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    & dotnet publish .\tools\InventoryPhotoOps.Cli\InventoryPhotoOps.Cli.csproj -c Release -r win-x64 --self-contained true -o (Join-Path $portableRoot 'tools\cli')
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    & dotnet publish .\tools\InventoryPhotoOps.Migrator\InventoryPhotoOps.Migrator.csproj -c Release -r win-x64 --self-contained true -o (Join-Path $portableRoot 'tools\migrator')
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Copy-Item -LiteralPath .\docs -Destination (Join-Path $portableRoot 'docs') -Recurse -Force
    Copy-Item -LiteralPath .\scripts -Destination (Join-Path $portableRoot 'scripts') -Recurse -Force
    Copy-Item -LiteralPath .\config -Destination (Join-Path $portableRoot 'config') -Recurse -Force
    Copy-Item -LiteralPath .\InventoryPhotoOps-ControlCenter.bat -Destination $portableRoot -Force
    Copy-Item -LiteralPath .\eBay-Hero-ControlCenter.bat -Destination $portableRoot -Force
    Copy-Item -LiteralPath .\README.md -Destination $portableRoot -Force
    Copy-Item -LiteralPath .\VERSION.txt -Destination $portableRoot -Force
    Copy-Item -LiteralPath .\EBAY_HERO_INTEGRATION_REPORT.md -Destination $portableRoot -Force
    $manifest = [ordered]@{
        name = 'eBay-Hero'
        version = (Get-Content -LiteralPath .\VERSION.txt | Select-Object -First 1).Trim()
        createdUtc = (Get-Date).ToUniversalTime().ToString('o')
        runtime = 'win-x64 self-contained'
        entryPoints = @('InventoryPhotoOps.App.exe', 'eBay-Hero-ControlCenter.bat', 'InventoryPhotoOps-ControlCenter.bat', 'tools\cli\InventoryPhotoOps.Cli.exe', 'tools\migrator\InventoryPhotoOps.Migrator.exe')
    } | ConvertTo-Json -Depth 5
    Set-Content -LiteralPath (Join-Path $portableRoot 'release-manifest.json') -Value $manifest -Encoding UTF8
    if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
    Compress-Archive -Path (Join-Path $portableRoot '*') -DestinationPath $zipPath -Force
    $hash = Get-CompatibleFileHash -Algorithm SHA256 -LiteralPath $zipPath
    Set-Content -LiteralPath ($zipPath + '.sha256') -Value "$($hash.Hash)  $(Split-Path -Leaf $zipPath)" -Encoding ASCII
    Write-Host "Portable package: $zipPath"
} finally {
    Pop-Location
}
