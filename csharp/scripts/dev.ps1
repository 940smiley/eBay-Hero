[CmdletBinding()]
param([switch]$Demo)

& (Join-Path $PSScriptRoot 'build.ps1')
& (Join-Path $PSScriptRoot 'launch.ps1') -Demo:$Demo

