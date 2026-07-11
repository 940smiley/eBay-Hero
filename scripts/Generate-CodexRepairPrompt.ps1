[CmdletBinding()]
param(
    [string]$FailureBundle = '',
    [string]$OutputPath = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')

$repo = Get-RepoRoot
if ([string]::IsNullOrWhiteSpace($FailureBundle)) {
    $bundleRoot = Join-Path $repo 'artifacts\failure-bundles'
    if (Test-Path -LiteralPath $bundleRoot) {
        $FailureBundle = (Get-ChildItem -LiteralPath $bundleRoot -Directory | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName
    }
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $repo 'artifacts\CODEX-REPAIR-PROMPT.md'
}

$failureText = if ($FailureBundle -and (Test-Path -LiteralPath $FailureBundle)) {
    Get-ChildItem -LiteralPath $FailureBundle -File -Recurse | Where-Object { $_.Name -match 'FAILURE|stderr|commands|environment' } | ForEach-Object {
        "## $($_.Name)`n`n" + (Get-Content -Raw -LiteralPath $_.FullName -ErrorAction SilentlyContinue)
    } | Out-String
} else {
    'No failure bundle was found. Re-run the failed script to generate one.'
}

$prompt = @"
You are Codex working in:
$repo

Goal:
Repair the failing eBay Assistance build, test, migration, packaging, or diagnostic workflow.

Failure context:
$(Redact-Text $failureText)

Required steps:
1. Inspect the failing command and source files.
2. Apply a minimal fix.
3. Rerun the failed command.
4. Rerun `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test.ps1`.
5. Report exact files changed and verification results.
"@

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputPath) | Out-Null
Set-Content -LiteralPath $OutputPath -Value $prompt -Encoding UTF8
Write-Host $OutputPath

