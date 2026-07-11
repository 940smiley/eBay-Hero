[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Database,
    [string]$OutputPath = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $Database)) { throw "Database not found: $Database" }
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = "$Database.backup.$(Get-Date -Format 'yyyyMMdd-HHmmss').sqlite"
}
Copy-Item -LiteralPath $Database -Destination $OutputPath -Force
Write-Host $OutputPath

