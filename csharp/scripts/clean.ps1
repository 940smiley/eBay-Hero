[CmdletBinding()]
param([switch]$DryRun)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..')
$targets = @(
    Join-Path $repo 'artifacts',
    Join-Path $repo 'TestResults'
) + @(Get-ChildItem -LiteralPath $repo -Directory -Recurse -Force | Where-Object { $_.Name -in @('bin','obj') } | ForEach-Object { $_.FullName })

foreach ($target in $targets | Sort-Object -Unique) {
    $resolved = [IO.Path]::GetFullPath($target)
    if (-not $resolved.StartsWith([IO.Path]::GetFullPath($repo), [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean outside repository: $resolved"
    }

    if ($DryRun) {
        Write-Host "Would remove $resolved"
    } elseif (Test-Path -LiteralPath $resolved) {
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}

