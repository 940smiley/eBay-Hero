[CmdletBinding()]
param(
    [switch]$DryRun = $true,
    [switch]$Apply,
    [switch]$ProjectOnly,
    [switch]$IncludeCaches,
    [switch]$IncludePortableApps,
    [switch]$IncludeSupportedReinstalls,
    [string]$DestinationRoot = 'E:\eBayHero-Tools',
    [string]$ManifestPath,
    [switch]$VerboseLogging
)

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$manifestRoot = Join-Path $DestinationRoot 'manifests'
$logRoot = Join-Path $DestinationRoot 'logs'
$quarantineRoot = Join-Path $DestinationRoot 'quarantine'
New-Item -ItemType Directory -Force -Path $manifestRoot, $logRoot, $quarantineRoot | Out-Null
if (-not $ManifestPath) { $ManifestPath = Join-Path $manifestRoot ("storage-audit-" + (Get-Date -Format 'yyyyMMdd-HHmmss') + ".json") }

$protected = @('C:\Windows','C:\Program Files','C:\Program Files (x86)','C:\ProgramData','C:\Users','C:\Recovery')
function Is-ProtectedPath([string]$Path) {
    $full = [IO.Path]::GetFullPath($Path)
    foreach ($p in $protected) {
        if ($full.StartsWith($p, [StringComparison]::OrdinalIgnoreCase)) { return $true }
    }
    return $false
}
function Measure-Directory([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return [pscustomobject]@{ Size=0; Count=0 } }
    $items = Get-ChildItem -LiteralPath $Path -Recurse -File -ErrorAction SilentlyContinue
    return [pscustomobject]@{ Size=($items | Measure-Object Length -Sum).Sum; Count=($items | Measure-Object).Count }
}

$candidates = New-Object System.Collections.Generic.List[object]
$allow = @(
    @{ Source = Join-Path $repo 'artifacts'; Class='Safe to recreate'; Why='Project-owned release artifacts.' },
    @{ Source = Join-Path $repo 'TestResults'; Class='Safe to recreate'; Why='Project-owned test output.' }
)
if ($IncludeCaches) {
    $allow += @{ Source = Join-Path $env:USERPROFILE '.nuget\packages'; Class='Safe to recreate'; Why='NuGet package cache; can be recreated by restore.' }
}
foreach ($entry in $allow) {
    $source = [string]$entry.Source
    $measure = Measure-Directory $source
    $blocked = Is-ProtectedPath $source -and -not $source.StartsWith($repo, [StringComparison]::OrdinalIgnoreCase)
    $candidates.Add([pscustomobject]@{
        Source = $source
        Destination = Join-Path $DestinationRoot ('relocated\' + (Split-Path -Leaf $source))
        SizeBytes = [int64]$measure.Size
        FileCount = [int]$measure.Count
        Classification = if ($blocked) { 'Do not move' } else { $entry.Class }
        Reason = if ($blocked) { 'Protected/system-owned path boundary.' } else { $entry.Why }
        ApplyAllowed = (-not $blocked) -and $source.StartsWith($repo, [StringComparison]::OrdinalIgnoreCase)
        RequiredProcessShutdown = 'None for dry run; close app/build tools before apply.'
        RollbackMethod = 'Restore from recorded source backup.'
        VerificationMethod = 'File count/hash sample plus dotnet build/test.'
        RegistryOrServiceReferences = 'Not checked for project-owned paths; installed apps require supported reinstall.'
        InUse = $false
    })
}
$manifest = [pscustomobject]@{
    CreatedUtc = (Get-Date).ToUniversalTime()
    Repository = $repo
    DestinationRoot = $DestinationRoot
    DryRun = -not $Apply
    Candidates = $candidates
}
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $ManifestPath -Encoding UTF8
Write-Host "Storage audit manifest: $ManifestPath"
if ($Apply) {
    throw "Apply mode is intentionally limited in this version. Review the manifest and perform approved project-owned moves only with a future item-selection manifest."
}

