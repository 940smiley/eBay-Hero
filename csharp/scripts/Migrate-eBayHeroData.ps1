[CmdletBinding()]
param(
    [switch]$DryRun,
    [switch]$Apply,
    [switch]$Backup,
    [string]$Source = '',
    [string]$Destination = '',
    [string]$ReportPath = '',
    [switch]$VerboseLogging,
    [switch]$AllowLive
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')
Set-ProjectProcessEnvironment

if ($DryRun -and $Apply) { throw 'Specify either -DryRun or -Apply, not both.' }
if (-not $DryRun -and -not $Apply) { $DryRun = $true }

if ($Backup -and -not [string]::IsNullOrWhiteSpace($Destination) -and (Test-Path -LiteralPath $Destination)) {
    $backupPath = "$Destination.eBayHero-backup.$(Get-Date -Format 'yyyyMMdd-HHmmss').sqlite"
    Copy-Item -LiteralPath $Destination -Destination $backupPath -Force
    Write-Host "Backup created: $backupPath"
}

$repo = Get-RepoRoot
Push-Location $repo
try {
    $dotnetArgs = @('run', '--project', '.\tools\eBayHero.Migrator', '--')
    if ($DryRun) { $dotnetArgs += '--dry-run' } else { $dotnetArgs += '--apply' }
    if ($AllowLive) { $dotnetArgs += '--allow-live' }
    if (-not [string]::IsNullOrWhiteSpace($Source)) { $dotnetArgs += @('--json', $Source) }
    if (-not [string]::IsNullOrWhiteSpace($Destination)) { $dotnetArgs += @('--database', $Destination) }

    $output = & dotnet @dotnetArgs 2>&1 | Out-String
    $safeOutput = Redact-Text $output
    Write-Host $safeOutput
    if (-not [string]::IsNullOrWhiteSpace($ReportPath)) {
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $ReportPath) | Out-Null
        Set-Content -LiteralPath $ReportPath -Value $safeOutput -Encoding UTF8
    }
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}


