Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
}

function Get-PathConfig {
    $repo = Get-RepoRoot
    $path = Join-Path $repo 'config\paths.json'
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing path config: $path" }
    return Get-Content -Raw -LiteralPath $path | ConvertFrom-Json
}

function Set-ProjectProcessEnvironment {
    $config = Get-PathConfig
    New-Item -ItemType Directory -Force -Path $config.SharedCacheRoot, $config.TemporaryTestRoot, $config.LogRoot | Out-Null
    $env:NUGET_PACKAGES = Join-Path $config.SharedCacheRoot 'NuGet'
    $env:DOTNET_CLI_HOME = Join-Path $config.SharedCacheRoot 'DotNetCliHome'
    $env:IPO_PATH_CONFIG = Join-Path (Get-RepoRoot) 'config\paths.json'
}

function Redact-Text {
    param([AllowNull()][string]$Text)
    if ($null -eq $Text) { return '' }
    $redacted = $Text -replace '(?i)(client[_ -]?secret|cert[_ -]?id|access[_ -]?token|refresh[_ -]?token|password)\s*[:=]\s*["'']?[^"'']+', '$1=[REDACTED]'
    $redacted = $redacted -replace '(?i)(Authorization:\s*(Bearer|Basic)\s+)[A-Za-z0-9._~+/=-]+', '$1[REDACTED]'
    return $redacted
}

function New-FailureBundle {
    param(
        [Parameter(Mandatory)][string]$Command,
        [Parameter(Mandatory)][int]$ExitCode,
        [AllowNull()][string]$StdOut,
        [AllowNull()][string]$StdErr
    )

    $repo = Get-RepoRoot
    $timestamp = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
    $root = Join-Path $repo "artifacts\failure-bundles\$timestamp"
    $logs = Join-Path $root 'logs'
    New-Item -ItemType Directory -Force -Path $logs | Out-Null
    $safeOut = Redact-Text $StdOut
    $safeErr = Redact-Text $StdErr
    $report = ''
    $prompt = ''
    Set-Content -LiteralPath (Join-Path $logs 'stdout.log') -Value $safeOut -Encoding UTF8
    Set-Content -LiteralPath (Join-Path $logs 'stderr.log') -Value $safeErr -Encoding UTF8
    Set-Content -LiteralPath (Join-Path $root 'commands.txt') -Value $Command -Encoding UTF8
    $dirty = if (Test-Path -LiteralPath (Join-Path $repo '.git')) { git -C $repo status --short 2>$null } else { 'No .git directory present.' }
    $commit = if (Test-Path -LiteralPath (Join-Path $repo '.git')) { git -C $repo rev-parse HEAD 2>$null } else { 'No git commit available.' }
    $environment = [ordered]@{
        repository = $repo
        currentCommit = $commit
        dirtyFiles = @($dirty)
        dotnet = (& dotnet --version 2>$null)
        powershell = $PSVersionTable.PSVersion.ToString()
        os = [Environment]::OSVersion.VersionString
    } | ConvertTo-Json -Depth 4
    Set-Content -LiteralPath (Join-Path $root 'environment.json') -Value $environment -Encoding UTF8
    $report = @"
# Failure Report

- Repository: $repo
- Command: `$Command`
- Exit code: $ExitCode

## Error Tail

```
$($safeErr -split "`r?`n" | Select-Object -Last 80 | Out-String)
```
"@
    Set-Content -LiteralPath (Join-Path $root 'FAILURE-REPORT.md') -Value $report -Encoding UTF8
    $prompt = @"
You are working in $repo.

Fix the root cause of this failure, rerun the failed command, then rerun the complete test suite.

Command:
$Command

Exit code:
$ExitCode

Error:
$safeErr

Git state:
$dirty
"@
    Set-Content -LiteralPath (Join-Path $root 'CODEX-FIX-PROMPT.md') -Value $prompt -Encoding UTF8
    return $root
}

function New-CompatibleTemporaryFile {
    $tempRoot = [System.IO.Path]::GetTempPath()
    for ($i = 0; $i -lt 25; $i++) {
        $path = Join-Path $tempRoot ("InventoryPhotoOps." + [System.Guid]::NewGuid().ToString('N') + ".tmp")
        try {
            $stream = [System.IO.File]::Open($path, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::Read)
            $stream.Dispose()
            return $path
        } catch [System.IO.IOException] {
            continue
        }
    }

    throw "Unable to create a temporary file under $tempRoot."
}

function Get-CompatibleFileHash {
    param(
        [Parameter(Mandatory)][string]$LiteralPath,
        [string]$Algorithm = 'SHA256'
    )

    if (-not (Test-Path -LiteralPath $LiteralPath)) {
        throw "File not found: $LiteralPath"
    }

    $nativeCommand = Get-Command -Name Get-FileHash -ErrorAction SilentlyContinue
    if ($nativeCommand) {
        return Get-FileHash -Algorithm $Algorithm -LiteralPath $LiteralPath
    }

    if ($Algorithm -ne 'SHA256') {
        throw "Managed fallback only supports SHA256 when Get-FileHash is unavailable."
    }

    $stream = [System.IO.File]::Open($LiteralPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::Read)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = $sha.ComputeHash($stream)
        $hash = ($bytes | ForEach-Object { $_.ToString('x2') }) -join ''
        return [pscustomobject]@{
            Algorithm = 'SHA256'
            Hash = $hash.ToUpperInvariant()
            Path = (Resolve-Path -LiteralPath $LiteralPath).Path
        }
    } finally {
        $sha.Dispose()
        $stream.Dispose()
    }
}

function Invoke-LoggedCommand {
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [string[]]$ArgumentList = @(),
        [switch]$DryRun
    )

    $command = $FilePath + ' ' + ($ArgumentList -join ' ')
    if ($DryRun) {
        Write-Host "Would run: $command"
        return 0
    }

    $stdout = New-CompatibleTemporaryFile
    $stderr = New-CompatibleTemporaryFile
    try {
        $process = Start-Process -FilePath $FilePath -ArgumentList $ArgumentList -NoNewWindow -Wait -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr
        $outText = Get-Content -Raw -LiteralPath $stdout -ErrorAction SilentlyContinue
        $errText = Get-Content -Raw -LiteralPath $stderr -ErrorAction SilentlyContinue
        if ($outText) { Write-Host (Redact-Text $outText) }
        if ($errText) { Write-Error (Redact-Text $errText) -ErrorAction Continue }
        if ($process.ExitCode -ne 0) {
            $bundle = New-FailureBundle -Command $command -ExitCode $process.ExitCode -StdOut $outText -StdErr $errText
            throw "Command failed with exit code $($process.ExitCode). Failure bundle: $bundle"
        }
        return $process.ExitCode
    } finally {
        Remove-Item -LiteralPath $stdout, $stderr -Force -ErrorAction SilentlyContinue
    }
}
