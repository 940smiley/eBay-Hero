[CmdletBinding()]
param(
    [switch]$Demo,
    [switch]$Live,
    [switch]$DryRun
)

. (Join-Path $PSScriptRoot 'common.ps1')
$repo = Get-RepoRoot
$exe = Join-Path $repo 'src\eBayHero.App\bin\Release\net8.0-windows\eBayHero.App.exe'
if (-not (Test-Path -LiteralPath $exe)) {
    $exe = Join-Path $repo 'artifacts\publish\eBayHero-win-x64\eBayHero.App.exe'
}
if (-not (Test-Path -LiteralPath $exe)) { throw "App executable not found. Build or publish first." }
$args = @()
if ($Live) { $args += '--allow-live' }
if ($DryRun) {
    Write-Host "Would launch $exe $($args -join ' ')"
    exit 0
}
Start-Process -FilePath $exe -ArgumentList $args -WorkingDirectory (Split-Path -Parent $exe)

