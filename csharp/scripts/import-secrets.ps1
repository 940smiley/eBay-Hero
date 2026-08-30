[CmdletBinding()]
param(
    [string]$SourceJson,
    [switch]$Apply
)

. (Join-Path $PSScriptRoot 'common.ps1')
$repo = Get-RepoRoot
if (-not $SourceJson) { throw "-SourceJson is required. Provide a reviewed JSON file with secret names and values." }
if (-not (Test-Path -LiteralPath $SourceJson)) { throw "Source JSON not found: $SourceJson" }
$configDir = Join-Path $repo 'artifacts\local-secrets'
$backupDir = Join-Path $repo ('artifacts\local-secrets-backups\' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
$target = Join-Path $configDir 'secrets.local.json'
$payload = Get-Content -Raw -LiteralPath $SourceJson | ConvertFrom-Json
$names = $payload.PSObject.Properties.Name
Write-Host "Secret names discovered:"
$names | ForEach-Object { Write-Host " - $_ = [REDACTED]" }
if (-not $Apply) {
    Write-Host "Dry run only. Re-run with -Apply to write project-local encrypted/import staging files."
    exit 0
}
New-Item -ItemType Directory -Force -Path $configDir, $backupDir | Out-Null
if (Test-Path -LiteralPath $target) { Copy-Item -LiteralPath $target -Destination (Join-Path $backupDir 'secrets.local.json') -Force }
$payload | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $target -Encoding UTF8
$gitignore = Join-Path $repo '.gitignore'
$ignoreLines = @('artifacts/local-secrets/', '*.local.secret.json', 'secrets.local.json')
if (-not (Test-Path -LiteralPath $gitignore)) { New-Item -ItemType File -Path $gitignore | Out-Null }
$existing = Get-Content -LiteralPath $gitignore
foreach ($line in $ignoreLines) {
    if ($existing -notcontains $line) { Add-Content -LiteralPath $gitignore -Value $line }
}
Write-Host "Secrets imported to ignored local storage. Values were not printed."
