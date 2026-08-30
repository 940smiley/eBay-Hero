[CmdletBinding()]
param(
    [string]$Path = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')

if ([string]::IsNullOrWhiteSpace($Path)) {
    $Path = Join-Path (Get-RepoRoot) 'artifacts\diagnostics'
}

if (-not (Test-Path -LiteralPath $Path)) {
    Write-Host "No diagnostics path found to verify: $Path"
    exit 0
}

$patterns = @(
    '(?i)(client[_ -]?secret|cert[_ -]?id|access[_ -]?token|refresh[_ -]?token|password)\s*[:=]\s*["'']?[^"''\s\[]+',
    '(?i)Authorization:\s*(Bearer|Basic)\s+[A-Za-z0-9._~+/=-]+'
)

$hits = if ((Get-Item -LiteralPath $Path).PSIsContainer) {
    Get-ChildItem -LiteralPath $Path -File -Recurse | Select-String -Pattern $patterns -ErrorAction SilentlyContinue
} else {
    Select-String -LiteralPath $Path -Pattern $patterns -ErrorAction SilentlyContinue
}

if ($hits) {
    $hits | Select-Object Path, LineNumber, Line | Format-Table -AutoSize
    throw "Potential unredacted secret found in $Path"
}

Write-Host "Secret redaction verification passed for $Path"

