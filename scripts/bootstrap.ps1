[CmdletBinding()]
param([switch]$DryRun)

. (Join-Path $PSScriptRoot 'common.ps1')
& (Join-Path $PSScriptRoot 'configure-paths.ps1') -Apply:(!$DryRun)
& (Join-Path $PSScriptRoot 'restore.ps1') -DryRun:$DryRun
& (Join-Path $PSScriptRoot 'doctor.ps1') -DryRun:$DryRun
