[CmdletBinding()]
param([switch]$DryRun)

. (Join-Path $PSScriptRoot 'common.ps1')
$repo = Get-RepoRoot
$config = Get-PathConfig
Set-ProjectProcessEnvironment
$reportRoot = Join-Path $repo 'artifacts\logs'
if (-not $DryRun) { New-Item -ItemType Directory -Force -Path $reportRoot | Out-Null }
$report = [ordered]@{
    Repository = $repo
    ExpectedRepository = $config.RepositoryRoot
    RepositoryMatches = ([IO.Path]::GetFullPath($repo).TrimEnd('\') -ieq [IO.Path]::GetFullPath($config.RepositoryRoot).TrimEnd('\'))
    WindowsVersion = [Environment]::OSVersion.VersionString
    PowerShellVersion = $PSVersionTable.PSVersion.ToString()
    DotNetVersion = (& dotnet --version 2>$null)
    TesseractPath = $config.TesseractPath
    TesseractFound = Test-Path -LiteralPath $config.TesseractPath
    FreeSpaceRepositoryDriveGB = [math]::Round((Get-PSDrive -Name ([IO.Path]::GetPathRoot($repo).Substring(0,1))).Free / 1GB, 2)
}
$json = $report | ConvertTo-Json -Depth 5
Write-Host $json
if (-not $DryRun) { Set-Content -LiteralPath (Join-Path $reportRoot 'doctor.json') -Value $json -Encoding UTF8 }
if (-not $report.RepositoryMatches) { exit 2 }
