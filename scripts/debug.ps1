[CmdletBinding()]
param([switch]$Demo)

$env:DOTNET_ENVIRONMENT = 'Development'
& (Join-Path $PSScriptRoot 'launch.ps1') -Demo:$Demo

