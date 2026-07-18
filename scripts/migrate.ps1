[CmdletBinding(DefaultParameterSetName='DryRun')]
param(
    [Parameter(ParameterSetName='DryRun')][switch]$DryRun,
    [Parameter(ParameterSetName='Apply')][switch]$Apply,
    [Parameter(ParameterSetName='Check')][switch]$DatabaseCheck,
    [switch]$AllowLive
)

. (Join-Path $PSScriptRoot 'common.ps1')
Set-ProjectProcessEnvironment
$repo = Get-RepoRoot
Push-Location $repo
try {
    if ($DatabaseCheck) {
        $args = @('run','--project','.\tools\eBayHero.Cli','--','database','check')
        if ($AllowLive) { $args += '--allow-live' }
        Invoke-LoggedCommand -FilePath 'dotnet' -ArgumentList $args
    } else {
        $args = @('run','--project','.\tools\eBayHero.Migrator','--')
        if ($Apply) { $args += '--apply' } else { $args += '--dry-run' }
        if ($AllowLive) { $args += '--allow-live' }
        Invoke-LoggedCommand -FilePath 'dotnet' -ArgumentList $args
    }
} finally {
    Pop-Location
}

