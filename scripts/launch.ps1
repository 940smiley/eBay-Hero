[CmdletBinding()]
param(
    [switch]$Demo,
    [switch]$Live,
    [switch]$DryRun
)

. (Join-Path $PSScriptRoot 'common.ps1')
$repo = Get-RepoRoot
$exe = Join-Path $repo 'src\InventoryPhotoOps.App\bin\Release\net8.0-windows\InventoryPhotoOps.App.exe'
if (-not (Test-Path -LiteralPath $exe)) {
    $exe = Join-Path $repo 'artifacts\publish\InventoryPhotoOps-win-x64\InventoryPhotoOps.App.exe'
}
if (-not (Test-Path -LiteralPath $exe)) { throw "App executable not found. Build or publish first." }
$args = @()
if ($Live) { $args += '--allow-live' }
if ($DryRun) {
    Write-Host "Would launch $exe $($args -join ' ')"
    exit 0
}
Start-Process -FilePath $exe -ArgumentList $args -WorkingDirectory (Split-Path -Parent $exe)
