[CmdletBinding()]
param([switch]$DryRun)

& (Join-Path $PSScriptRoot 'publish-portable.ps1') -DryRun:$DryRun

