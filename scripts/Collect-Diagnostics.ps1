[CmdletBinding()]
param(
    [string]$OutputRoot = '',
    [switch]$IncludePrivateFiles
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')
Set-ProjectProcessEnvironment

$repo = Get-RepoRoot
$config = Get-PathConfig
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $config.OperationsRoot 'DiagnosticBundles'
}

$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$bundleRoot = Join-Path $OutputRoot "DiagnosticBundle-$timestamp"
$zipPath = "$bundleRoot.zip"
New-Item -ItemType Directory -Force -Path $bundleRoot | Out-Null

$gitCommit = if (Test-Path -LiteralPath (Join-Path $repo '.git')) { git -C $repo rev-parse HEAD 2>$null } else { 'unavailable' }
$gitStatus = if (Test-Path -LiteralPath (Join-Path $repo '.git')) { git -C $repo status --short 2>$null } else { 'unavailable' }

Set-Content -LiteralPath (Join-Path $bundleRoot 'SUMMARY.md') -Encoding UTF8 -Value @"
# Diagnostic Bundle

- Created: $(Get-Date -Format o)
- Repository: $repo
- Git commit: $gitCommit
- Private files included: $IncludePrivateFiles

This bundle intentionally excludes secrets, raw inventory databases, raw images, buyer data, and token files.
"@

[ordered]@{
    os = [Environment]::OSVersion.VersionString
    machine = $env:COMPUTERNAME
    user = $env:USERNAME
    powershell = $PSVersionTable.PSVersion.ToString()
    dotnet = (& dotnet --info 2>$null | Out-String)
} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $bundleRoot 'environment.json') -Encoding UTF8

[ordered]@{
    product = 'eBay Assistance'
    version = if (Test-Path -LiteralPath (Join-Path $repo 'VERSION.txt')) { (Get-Content -LiteralPath (Join-Path $repo 'VERSION.txt') -Raw).Trim() } else { 'unknown' }
    repository = $repo
    schemaVersion = 'pending runtime check'
} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $bundleRoot 'application.json') -Encoding UTF8

[ordered]@{
    gitCommit = $gitCommit
    gitStatus = @($gitStatus)
    buildRoot = $env:EA_BUILD_ROOT
    releaseRoot = $env:EA_RELEASE_ROOT
} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $bundleRoot 'git-build-info.json') -Encoding UTF8

Set-Content -LiteralPath (Join-Path $bundleRoot 'capabilities.json') -Encoding UTF8 -Value (@{
    windows = $true
    linux = $false
    ios = $false
    android = $false
    ebaySandbox = $false
    ebayProduction = $false
} | ConvertTo-Json -Depth 4)

Set-Content -LiteralPath (Join-Path $bundleRoot 'recent-actions.json') -Encoding UTF8 -Value '[]'
Set-Content -LiteralPath (Join-Path $bundleRoot 'database-integrity.txt') -Encoding UTF8 -Value 'Not run by diagnostics bundle.'
Set-Content -LiteralPath (Join-Path $bundleRoot 'config-redacted.json') -Encoding UTF8 -Value (Redact-Text (($config | ConvertTo-Json -Depth 5) | Out-String))
Set-Content -LiteralPath (Join-Path $bundleRoot 'installed-dependencies.json') -Encoding UTF8 -Value (@{ dotnetSdks = @(& dotnet --list-sdks 2>$null); dotnetRuntimes = @(& dotnet --list-runtimes 2>$null) } | ConvertTo-Json -Depth 4)
Set-Content -LiteralPath (Join-Path $bundleRoot 'failed-jobs.json') -Encoding UTF8 -Value '[]'
Set-Content -LiteralPath (Join-Path $bundleRoot 'reproduction-steps.txt') -Encoding UTF8 -Value 'Describe the action that failed here before sending the bundle.'

$logsOut = Join-Path $bundleRoot 'logs'
New-Item -ItemType Directory -Force -Path $logsOut | Out-Null
foreach ($candidateLogRoot in @($config.LogRoot, (Join-Path $repo 'artifacts\failure-bundles'))) {
    if (Test-Path -LiteralPath $candidateLogRoot) {
        Get-ChildItem -LiteralPath $candidateLogRoot -File -Recurse -ErrorAction SilentlyContinue |
            Where-Object { $_.Length -lt 1048576 } |
            Select-Object -First 25 |
            ForEach-Object {
                $target = Join-Path $logsOut ($_.Name + '.redacted.txt')
                Set-Content -LiteralPath $target -Encoding UTF8 -Value (Redact-Text (Get-Content -Raw -LiteralPath $_.FullName -ErrorAction SilentlyContinue))
            }
    }
}

if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
Compress-Archive -Path (Join-Path $bundleRoot '*') -DestinationPath $zipPath -Force
Write-Host $zipPath

