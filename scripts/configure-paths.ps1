[CmdletBinding()]
param(
    [switch]$Apply,
    [switch]$SetUserEnvironment
)

. (Join-Path $PSScriptRoot 'common.ps1')
$repo = Get-RepoRoot
$config = Get-PathConfig
$paths = @($config.SharedToolsRoot, $config.SharedAppsRoot, $config.SharedCacheRoot, $config.OperationsToolsRoot, $config.ArtifactsRoot, $config.TemporaryTestRoot, $config.LogRoot)
foreach ($path in $paths) {
    if ($Apply) {
        New-Item -ItemType Directory -Force -Path $path | Out-Null
        Write-Host "Ensured $path"
    } else {
        Write-Host "Would ensure $path"
    }
}
if ($SetUserEnvironment) {
    if (-not $Apply) { throw "-SetUserEnvironment requires -Apply." }
    [Environment]::SetEnvironmentVariable('NUGET_PACKAGES', (Join-Path $config.SharedCacheRoot 'NuGet'), 'User')
    [Environment]::SetEnvironmentVariable('DOTNET_CLI_HOME', (Join-Path $config.SharedCacheRoot 'DotNetCliHome'), 'User')
    Write-Host "Set user-level NUGET_PACKAGES and DOTNET_CLI_HOME. No machine-wide environment was changed."
}
Write-Host "Path configuration file: $(Join-Path $repo 'config\paths.json')"
