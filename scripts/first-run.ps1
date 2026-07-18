[CmdletBinding()]
param(
    [switch]$Demo,
    [switch]$Live,
    [switch]$ApplyMigration,
    [switch]$DryRun
)

. (Join-Path $PSScriptRoot 'common.ps1')
$repo = Get-RepoRoot
$reportRoot = Join-Path $repo ("artifacts\first-run\" + (Get-Date -Format 'yyyyMMdd-HHmmss'))
if (-not $DryRun) { New-Item -ItemType Directory -Force -Path $reportRoot | Out-Null }
$steps = New-Object System.Collections.Generic.List[object]
function Step {
    param([string]$Name, [scriptblock]$Action)
    $start = Get-Date
    Write-Host "== $Name"
    try {
        & $Action
        $steps.Add([pscustomobject]@{ name=$Name; status='passed'; started=$start; ended=(Get-Date) })
    } catch {
        $steps.Add([pscustomobject]@{ name=$Name; status='failed'; started=$start; ended=(Get-Date); error=(Redact-Text $_.Exception.ToString()) })
        if (-not $DryRun) { $steps | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $reportRoot 'FIRST-RUN-REPORT.json') -Encoding UTF8 }
        throw
    }
}
Step 'Confirm repository path' { if ((Get-RepoRoot) -ne 'D:\WORK\Projects\ACTIVE\eBayHero') { throw 'Repository path mismatch.' } }
Step 'Configure project-safe paths' { & (Join-Path $PSScriptRoot 'configure-paths.ps1') -Apply:(!$DryRun) }
Step 'Doctor diagnostics' { & (Join-Path $PSScriptRoot 'doctor.ps1') -DryRun:$DryRun }
Step 'Restore dependencies' { & (Join-Path $PSScriptRoot 'restore.ps1') -DryRun:$DryRun }
Step 'Parse PowerShell scripts' {
    Get-ChildItem -LiteralPath $repo -Recurse -File -Filter *.ps1 | Where-Object { $_.FullName -notmatch '\\legacy-powershell\\' } | ForEach-Object {
        $tokens = $null
        $errors = $null
        [System.Management.Automation.Language.Parser]::ParseFile($_.FullName, [ref]$tokens, [ref]$errors) | Out-Null
        if ($errors.Count -gt 0) { throw "Parse error in $($_.FullName): $($errors[0].Message)" }
    }
}
Step 'Build Debug' { Invoke-LoggedCommand -FilePath 'dotnet' -ArgumentList @('build',(Join-Path $repo 'eBayHero.sln'),'-c','Debug') -DryRun:$DryRun }
Step 'Run tests' { if ($DryRun) { Write-Host 'Would run test suite.' } else { & (Join-Path $PSScriptRoot 'test.ps1') } }
Step 'Migration dry run' { & (Join-Path $PSScriptRoot 'migrate.ps1') -DryRun }
if ($ApplyMigration) {
    if (-not $Live) { throw '-ApplyMigration requires -Live.' }
    Step 'Apply live migration' { & (Join-Path $PSScriptRoot 'migrate.ps1') -Apply -AllowLive }
}
Step 'Build Release' { if ($DryRun) { Write-Host 'Would build Release.' } else { & (Join-Path $PSScriptRoot 'build.ps1') } }
Step 'Publish portable' { & (Join-Path $PSScriptRoot 'publish-portable.ps1') -DryRun:$DryRun }
Step 'Build installer' { & (Join-Path $PSScriptRoot 'build-installer.ps1') -DryRun:$DryRun }
if (-not $DryRun) { $steps | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $reportRoot 'FIRST-RUN-REPORT.json') -Encoding UTF8 }
if ($Demo -and -not $DryRun) { & (Join-Path $PSScriptRoot 'launch.ps1') -Demo }
if ($Live -and -not $DryRun) { & (Join-Path $PSScriptRoot 'launch.ps1') -Live }

