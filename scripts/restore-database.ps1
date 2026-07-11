[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Backup,
    [Parameter(Mandatory)][string]$Database,
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $Backup)) { throw "Backup not found: $Backup" }
if ((Test-Path -LiteralPath $Database) -and -not $Force) {
    throw "Destination exists. Re-run with -Force after confirming restore target: $Database"
}
Copy-Item -LiteralPath $Backup -Destination $Database -Force
Write-Host $Database

