[CmdletBinding()]
param(
    [string]$KnowledgeBaseRoot = 'D:\KnowledgeBase',
    [switch]$DryRun
)

. (Join-Path $PSScriptRoot 'common.ps1')
$repo = Get-RepoRoot
$outRoot = Join-Path $repo 'artifacts\knowledgebase'
if (-not $DryRun) { New-Item -ItemType Directory -Force -Path $outRoot | Out-Null }
if (-not (Test-Path -LiteralPath $KnowledgeBaseRoot)) {
    Write-Host "KnowledgeBase not found: $KnowledgeBaseRoot"
    exit 0
}
$patterns = 'ebay|oauth|openai|api|inventory|listing|ocr|card|csv|template|secret|credential'
function Get-RelativePathCompat {
    param([string]$Root, [string]$Path)
    $rootFull = [IO.Path]::GetFullPath($Root).TrimEnd('\') + '\'
    $pathFull = [IO.Path]::GetFullPath($Path)
    if ($pathFull.StartsWith($rootFull, [StringComparison]::OrdinalIgnoreCase)) {
        return $pathFull.Substring($rootFull.Length)
    }
    return $pathFull
}
$files = Get-ChildItem -LiteralPath $KnowledgeBaseRoot -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match $patterns -or $_.Name -match $patterns } |
    ForEach-Object {
        $relative = Get-RelativePathCompat -Root $KnowledgeBaseRoot -Path $_.FullName
        if ($relative -match '(?i)(secret|credential|token|api[_ -]?key)' -or $_.DirectoryName -match '(?i)\\secrets(\\|$)') {
            $relative = Join-Path ([IO.Path]::GetDirectoryName($relative)) '[REDACTED-FILENAME]'
        }
        [pscustomobject]@{
            RelativePath = $relative
            Length = $_.Length
            LastWriteTime = $_.LastWriteTime
        }
    }
$redacted = $files | ConvertTo-Json -Depth 3
Write-Host $redacted
if (-not $DryRun) {
    Set-Content -LiteralPath (Join-Path $outRoot 'knowledgebase-discovery.json') -Value $redacted -Encoding UTF8
}
