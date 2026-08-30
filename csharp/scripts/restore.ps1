[CmdletBinding()]
param([switch]$DryRun)

. (Join-Path $PSScriptRoot 'common.ps1')
Set-ProjectProcessEnvironment
$repo = Get-RepoRoot
Push-Location $repo
try {
    Invoke-LoggedCommand -FilePath 'dotnet' -ArgumentList @('tool','restore') -DryRun:$DryRun
    Invoke-LoggedCommand -FilePath 'dotnet' -ArgumentList @('restore','eBayHero.sln') -DryRun:$DryRun
} finally {
    Pop-Location
}

