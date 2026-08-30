[CmdletBinding()]
param([string]$DestinationRoot = 'E:\eBayHero-Tools', [switch]$DryRun)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$sourceRoot = Join-Path $PSScriptRoot 'storage'
$destRoot = $DestinationRoot
$files = @('Audit-And-Relocate-DevStorage.ps1','Verify-And-Cleanup-DevStorage.ps1')
foreach ($folder in @($destRoot, (Join-Path $destRoot 'manifests'), (Join-Path $destRoot 'logs'), (Join-Path $destRoot 'quarantine'))) {
    if ($DryRun) { Write-Host "Would ensure $folder" } else { New-Item -ItemType Directory -Force -Path $folder | Out-Null }
}
foreach ($file in $files) {
    $src = Join-Path $sourceRoot $file
    $dst = Join-Path $destRoot $file
    if ($DryRun) { Write-Host "Would copy $src to $dst" } else { Copy-Item -LiteralPath $src -Destination $dst -Force; Write-Host "Copied $dst" }
}

