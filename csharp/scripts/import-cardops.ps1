[CmdletBinding(DefaultParameterSetName='DryRun')]
param(
    [Parameter(ParameterSetName='DryRun')][switch]$DryRun,
    [Parameter(ParameterSetName='Apply')][switch]$Apply,
    [switch]$AllowLive,
    [string]$SourceDb = '',
    [string]$Database = ''
)

. (Join-Path $PSScriptRoot 'common.ps1')
Set-ProjectProcessEnvironment

if ([string]::IsNullOrWhiteSpace($SourceDb)) {
    if (-not [string]::IsNullOrWhiteSpace($env:CARDOPS_DB)) {
        $SourceDb = $env:CARDOPS_DB
    } else {
        $SourceDb = 'D:\WORK\GitRepos\PERSONAL\cardops\data\cardops.db'
    }
}

if (-not (Test-Path -LiteralPath $SourceDb)) {
    throw "CardOps database was not found: $SourceDb"
}

$repo = Get-RepoRoot
Push-Location $repo
try {
    $args = @('run','--project','.\tools\eBayHero.Cli','--','import','cardops','--source-db',$SourceDb)
    if ($Apply) { $args += '--apply' }
    if ($AllowLive) { $args += '--allow-live' }
    if (-not [string]::IsNullOrWhiteSpace($Database)) { $args += @('--database', $Database) }
    Invoke-LoggedCommand -FilePath 'dotnet' -ArgumentList $args
} finally {
    Pop-Location
}

