[CmdletBinding()]
param(
    [string]$Source = '',
    [string]$Destination = '',
    [string]$ReportPath = '',
    [switch]$VerboseLogging
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')
Set-ProjectProcessEnvironment

$repo = Get-RepoRoot
Push-Location $repo
try {
    $args = @('run', '--project', '.\tools\eBayHero.Cli', '--', 'database', 'check')
    if (-not [string]::IsNullOrWhiteSpace($Destination)) { $args += @('--database', $Destination) }
    $output = & dotnet @args 2>&1 | Out-String
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


