param(
    [switch]$DryRun,
    [string[]]$SourceRoots = @("D:\WORK\GitRepos", "D:\WORK\Projects"),
    [string]$OutputPath = "artifacts\source-discovery.json",
    [switch]$IncludeNonGitProjects,
    [switch]$VerboseLogging
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$ExcludeDirectoryNames = @(
    ".git", "node_modules", ".venv", "venv", "bin", "obj", "dist", "build",
    "out", "coverage", ".cache", ".pytest_cache", ".ruff_cache", ".mypy_cache",
    ".tox", "__pycache__", "packages", "artifacts", "release", "releases",
    "temp", "tmp", "backups", "site-packages"
)
$ProjectMarkerFiles = @(
    "*.sln", "*.csproj", "package.json", "pyproject.toml", "requirements.txt",
    "setup.py", "vite.config.*", "tauri.conf.json", "next.config.*",
    "electron-builder.*", "Cargo.toml", "*.sql", "*.sqlite", "*.db"
)
$SafeTextMarkers = @(
    "README.md", "README.txt", "package.json", "pyproject.toml",
    "requirements.txt", "Cargo.toml", "appsettings.json", "*.csproj", "*.sln"
)
$CapabilityKeywords = [ordered]@{
    "Inventory management" = @("inventory", "sku", "stock", "quantity", "catalog")
    "Image ingestion" = @("ingest", "import image", "image root", "scan images", "photo")
    "Image root management" = @("image root", "source root", "watch folder", "directory scan")
    "OCR" = @("ocr", "tesseract", "pytesseract", "easyocr", "vision")
    "OpenCV preprocessing" = @("opencv", "cv2", "emgu", "deskew", "threshold")
    "Card detection" = @("card detection", "detect card", "bounding box", "contour")
    "Sports-card identification" = @("sports card", "player", "team", "rookie", "parallel")
    "TCG identification" = @("tcg", "ccg", "pokemon", "magic", "yugioh")
    "Front/back image pairing" = @("front", "back", "pair", "paired")
    "Duplicate detection" = @("duplicate", "hash", "sha256", "perceptual", "phash")
    "Metadata normalization" = @("normalize", "metadata", "canonical", "field mapping")
    "File renaming" = @("rename", "filename", "sanitize")
    "File organization" = @("organize", "move file", "copy file", "folder")
    "Pricing" = @("pricing", "price", "value", "valuation", "fees")
    "Comparable sales" = @("comps", "sold", "comparable", "sales history")
    "Lot recommendations" = @("lot", "bundle", "recommendation")
    "eBay listing generation" = @("ebay", "listing", "title", "description", "item specifics")
    "Existing eBay listing import" = @("active listing", "inventory api", "sell api", "listing import")
    "Listing audits" = @("audit", "validation", "mismatch", "stale")
    "CSV import/export" = @("csv", "export", "import")
    "Background jobs" = @("job", "queue", "worker", "background")
    "Desktop UI" = @("wpf", "avalonia", "electron", "tauri", "desktop")
    "Linux compatibility" = @("linux", "appimage", "deb", "flatpak", "gtk")
    "iOS compatibility" = @("ios", "xcode", "swift", "swiftui")
    "Android compatibility" = @("android", "gradle", "kotlin", "java")
    "Tests" = @("test", "xunit", "nunit", "jest", "vitest", "pytest", "playwright")
    "Installer/release automation" = @("installer", "publish", "release", "wix", "squirrel", "msix", "inno")
    "Logging and diagnostics" = @("logging", "serilog", "diagnostic", "crash", "telemetry")
}

function Write-Trace {
    param([string]$Message)
    if ($VerboseLogging) {
        Write-Host "[discover] $Message"
    }
}

function Get-ValueCount {
    param([object]$Value)

    if ($null -eq $Value) {
        return 0
    }
    if ($Value -is [System.Array]) {
        return $Value.Length
    }
    if ($Value -is [System.Collections.ICollection]) {
        return $Value.Count
    }
    return 1
}

function Test-ExcludedDirectory {
    param([System.IO.DirectoryInfo]$Directory)
    return Test-ExcludedPath -Path $Directory.FullName
}

function Test-ExcludedPath {
    param([string]$Path)

    $parts = $Path -split '[\\/]' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    foreach ($part in $parts) {
        if ($ExcludeDirectoryNames -contains $part) {
            return $true
        }
        if ($part -like ".venv*") {
            return $true
        }
    }
    return $false
}

function Get-ChildDirectoriesSafe {
    param([System.IO.DirectoryInfo]$Directory)
    try {
        return Get-ChildItem -LiteralPath $Directory.FullName -Directory -Force -ErrorAction Stop |
            Where-Object { -not (Test-ExcludedDirectory $_) }
    }
    catch {
        Write-Trace "Skipping inaccessible directory: $($Directory.FullName)"
        return @()
    }
}

function Get-FilesSafe {
    param(
        [string]$Path,
        [string[]]$Patterns,
        [switch]$Recurse,
        [int]$Limit = 500
    )

    $results = New-Object System.Collections.Generic.List[object]
    foreach ($pattern in $Patterns) {
        try {
            $items = Get-ChildItem -LiteralPath $Path -Filter $pattern -File -Force -ErrorAction Stop -Recurse:$Recurse |
                Where-Object { -not (Test-ExcludedPath -Path $_.DirectoryName) } |
                Select-Object -First $Limit
            foreach ($item in $items) {
                $results.Add($item) | Out-Null
            }
        }
        catch {
            Write-Trace "File scan skipped for $Path / $pattern"
        }
    }
    return $results
}

function Get-DirectProjectMarkersFast {
    param([string]$Path)

    $markers = New-Object System.Collections.Generic.List[string]
    try {
        $files = Get-ChildItem -LiteralPath $Path -File -Force -ErrorAction Stop
        foreach ($file in $files) {
            $name = $file.Name
            if (
                $name -eq "package.json" -or
                $name -eq "pyproject.toml" -or
                $name -eq "requirements.txt" -or
                $name -eq "setup.py" -or
                $name -eq "tauri.conf.json" -or
                $name -eq "Cargo.toml" -or
                $name -like "*.sln" -or
                $name -like "*.csproj" -or
                $name -like "vite.config.*" -or
                $name -like "next.config.*" -or
                $name -like "electron-builder.*" -or
                $name -like "*.sql" -or
                $name -like "*.sqlite" -or
                $name -like "*.db"
            ) {
                $markers.Add($name) | Out-Null
            }
        }
    }
    catch {
        Write-Trace "Direct marker scan skipped for $Path"
    }
    return @($markers | Sort-Object -Unique)
}

function Invoke-Git {
    param(
        [string]$Path,
        [string[]]$Arguments
    )
    try {
        $output = & git -C $Path @Arguments 2>$null
        if ($LASTEXITCODE -eq 0) {
            return ($output -join "`n").Trim()
        }
    }
    catch {
        return $null
    }
    return $null
}

function Get-GitMetadata {
    param([string]$Path)

    $topLevel = Invoke-Git -Path $Path -Arguments @("rev-parse", "--show-toplevel")
    if ([string]::IsNullOrWhiteSpace($topLevel)) {
        return [ordered]@{
            isGit = $false
            root = $null
            remote = $null
            branch = $null
            latestCommit = $null
            dirtyState = $null
        }
    }

    $status = Invoke-Git -Path $topLevel -Arguments @("status", "--porcelain")
    $remote = Invoke-Git -Path $topLevel -Arguments @("remote", "get-url", "origin")
    $branch = Invoke-Git -Path $topLevel -Arguments @("rev-parse", "--abbrev-ref", "HEAD")
    $commit = Invoke-Git -Path $topLevel -Arguments @("log", "-1", "--format=%H %ci %s")

    return [ordered]@{
        isGit = $true
        root = $topLevel
        remote = $remote
        branch = $branch
        latestCommit = $commit
        dirtyState = if ([string]::IsNullOrWhiteSpace($status)) { "clean" } else { "dirty" }
    }
}

function Get-SafeTextSample {
    param([string]$Path)

    $parts = New-Object System.Collections.Generic.List[string]
    $files = Get-FilesSafe -Path $Path -Patterns $SafeTextMarkers -Limit 100
    foreach ($file in $files) {
        if ($file.Length -gt 1048576) {
            continue
        }
        try {
            $content = Get-Content -LiteralPath $file.FullName -Raw -ErrorAction Stop
            if (-not [string]::IsNullOrWhiteSpace($content)) {
                $parts.Add($file.Name) | Out-Null
                $parts.Add($content.Substring(0, [Math]::Min($content.Length, 12000))) | Out-Null
            }
        }
        catch {
            Write-Trace "Unable to read marker file $($file.FullName)"
        }
    }
    return ($parts -join "`n").ToLowerInvariant()
}

function Get-ProjectMarkers {
    param([string]$Path)
    $markers = Get-FilesSafe -Path $Path -Patterns $ProjectMarkerFiles -Limit 300
    return @($markers | ForEach-Object { $_.Name } | Sort-Object -Unique)
}

function Get-LatestMeaningfulModification {
    param(
        [string]$Path,
        [object]$GitMetadata
    )

    if ($GitMetadata -and $GitMetadata.latestCommit -match "^[a-f0-9]{40}\s+([0-9-]{10}\s+[0-9:]{8}\s+[+-][0-9]{4})") {
        return $Matches[1]
    }

    $latest = $null
    $queue = New-Object System.Collections.Generic.Queue[object]
    $queue.Enqueue([pscustomobject]@{ Directory = (Get-Item -LiteralPath $Path); Depth = 0 })
    $maxDepth = 4

    try {
        while ($queue.Count -gt 0) {
            $entry = $queue.Dequeue()
            $directory = $entry.Directory
            $depth = [int]$entry.Depth
            if (Test-ExcludedDirectory $directory) {
                continue
            }
            foreach ($file in (Get-ChildItem -LiteralPath $directory.FullName -File -Force -ErrorAction SilentlyContinue)) {
                if (-not $latest -or $file.LastWriteTime -gt $latest) {
                    $latest = $file.LastWriteTime
                }
            }
            if ($depth -lt $maxDepth) {
                foreach ($child in (Get-ChildDirectoriesSafe -Directory $directory)) {
                    $queue.Enqueue([pscustomobject]@{ Directory = $child; Depth = $depth + 1 })
                }
            }
        }
    }
    catch {
        Write-Trace "Latest modification scan skipped for $Path"
    }
    if ($latest) {
        return $latest.ToString("o")
    }
    return $null
}

function Get-FrameworkAndCommands {
    param(
        [string]$Path,
        [string[]]$Markers,
        [string]$Sample
    )

    $frameworks = New-Object System.Collections.Generic.List[string]
    $buildCommands = New-Object System.Collections.Generic.List[string]
    $testCommands = New-Object System.Collections.Generic.List[string]
    $db = New-Object System.Collections.Generic.List[string]
    $languages = New-Object System.Collections.Generic.List[string]

    if ($Markers -contains "package.json") {
        $languages.Add("TypeScript/JavaScript") | Out-Null
        if ($Sample -match "next") { $frameworks.Add("Next.js") | Out-Null }
        if ($Sample -match "vite") { $frameworks.Add("Vite") | Out-Null }
        if ($Sample -match "react") { $frameworks.Add("React") | Out-Null }
        if ($Sample -match "electron") { $frameworks.Add("Electron") | Out-Null }
        if ($Sample -match "tauri") { $frameworks.Add("Tauri") | Out-Null }
        $buildCommands.Add("npm install; npm run build") | Out-Null
        $testCommands.Add("npm test") | Out-Null
    }
    if ($Markers | Where-Object { $_ -like "*.csproj" -or $_ -like "*.sln" }) {
        $languages.Add("C#") | Out-Null
        if ($Sample -match "wpf") { $frameworks.Add("WPF") | Out-Null }
        if ($Sample -match "avalonia") { $frameworks.Add("Avalonia") | Out-Null }
        if ($Sample -match "maui") { $frameworks.Add(".NET MAUI") | Out-Null }
        if ($Sample -match "aspnetcore|microsoft.aspnetcore") { $frameworks.Add("ASP.NET Core") | Out-Null }
        $buildCommands.Add("dotnet build") | Out-Null
        $testCommands.Add("dotnet test") | Out-Null
    }
    if (($Markers -contains "pyproject.toml") -or ($Markers -contains "requirements.txt") -or ($Markers -contains "setup.py")) {
        $languages.Add("Python") | Out-Null
        if ($Sample -match "fastapi") { $frameworks.Add("FastAPI") | Out-Null }
        if ($Sample -match "flask") { $frameworks.Add("Flask") | Out-Null }
        if ($Sample -match "django") { $frameworks.Add("Django") | Out-Null }
        if ($Sample -match "opencv|cv2") { $frameworks.Add("OpenCV") | Out-Null }
        if ($Sample -match "tesseract|pytesseract|easyocr") { $frameworks.Add("OCR") | Out-Null }
        $buildCommands.Add("python -m compileall .") | Out-Null
        $testCommands.Add("pytest") | Out-Null
    }
    if ($Markers -contains "Cargo.toml") {
        $languages.Add("Rust") | Out-Null
        if ($Sample -match "tauri") { $frameworks.Add("Tauri") | Out-Null }
        $buildCommands.Add("cargo build") | Out-Null
        $testCommands.Add("cargo test") | Out-Null
    }

    if ($Sample -match "sqlite|sqlitepcl|better-sqlite|microsoft.data.sqlite") { $db.Add("SQLite") | Out-Null }
    if ($Sample -match "postgres|npgsql|psycopg") { $db.Add("PostgreSQL") | Out-Null }
    if ($Sample -match "entityframework|ef core|migrations") { $db.Add("Entity Framework Core") | Out-Null }
    if ($Sample -match "prisma") { $db.Add("Prisma") | Out-Null }

    $appType = "Unknown"
    if ($frameworks -contains "WPF" -or $frameworks -contains "Avalonia" -or $frameworks -contains "Electron" -or $frameworks -contains "Tauri") {
        $appType = "Desktop"
    }
    elseif ($frameworks -contains "Next.js" -or $frameworks -contains "React" -or $frameworks -contains "Vite") {
        $appType = "Web"
    }
    elseif ($frameworks -contains "FastAPI" -or $frameworks -contains "Flask" -or $frameworks -contains "Django" -or $frameworks -contains "ASP.NET Core") {
        $appType = "Service/API"
    }

    return [ordered]@{
        primaryLanguage = (@($languages | Sort-Object -Unique) -join ", ")
        framework = (@($frameworks | Sort-Object -Unique) -join ", ")
        applicationType = $appType
        buildCommand = (@($buildCommands | Sort-Object -Unique) -join " OR ")
        testCommand = (@($testCommands | Sort-Object -Unique) -join " OR ")
        databaseTechnology = (@($db | Sort-Object -Unique) -join ", ")
    }
}

function Get-RelevantFeatures {
    param([string]$Sample)
    $features = New-Object System.Collections.Generic.List[string]
    foreach ($capability in $CapabilityKeywords.Keys) {
        foreach ($keyword in $CapabilityKeywords[$capability]) {
            if ($Sample -match [regex]::Escape($keyword)) {
                $features.Add($capability) | Out-Null
                break
            }
        }
    }
    return @($features | Sort-Object -Unique)
}

function Get-SecretExposureRisk {
    param(
        [string]$Path,
        [string]$Sample
    )

    $riskSignals = New-Object System.Collections.Generic.List[string]
    $secretLikeNames = @(".env", ".env.local", "secrets.json", "*.pem", "*.pfx", "*.key", "id_rsa", "appsettings.Production.json")
    $secretFiles = Get-FilesSafe -Path $Path -Patterns $secretLikeNames -Limit 50
    foreach ($file in $secretFiles) {
        $riskSignals.Add("secret-like file name: $($file.Name)") | Out-Null
    }
    if ($Sample -match "api[_-]?key|client[_-]?secret|oauth|token|password") {
        $riskSignals.Add("secret-like config references") | Out-Null
    }

    if ((Get-ValueCount $riskSignals) -eq 0) {
        return "low"
    }
    return "review required: $(@($riskSignals | Sort-Object -Unique) -join '; ')"
}

function Get-License {
    param(
        [string]$Path,
        [string]$Sample
    )
    $licenseFiles = Get-FilesSafe -Path $Path -Patterns @("LICENSE", "LICENSE.*", "COPYING") -Limit 10
    if ((Get-ValueCount $licenseFiles) -gt 0) {
        return ($licenseFiles | Select-Object -First 1).Name
    }
    if ($Sample -match '"license"\s*:\s*"([^"]+)"') {
        return $Matches[1]
    }
    return "unknown"
}

function Get-Disposition {
    param(
        [string]$Name,
        [string[]]$Features,
        [string]$SecretRisk,
        [string]$License
    )

    if ($License -ne "unknown" -and $License -match "gpl|agpl") {
        return "REFERENCE_ONLY"
    }
    if ($SecretRisk -ne "low") {
        return "REFERENCE_ONLY"
    }
    if ($Name -match "(?i)card\s*ops|cardops|inventory\s*photo|ebay|inventory") {
        if ((Get-ValueCount $Features) -ge 6) {
            return "IMPORT_AND_REFACTOR"
        }
        return "REFERENCE_ONLY"
    }
    if ((Get-ValueCount $Features) -ge 8) {
        return "IMPORT_AND_REFACTOR"
    }
    if ((Get-ValueCount $Features) -ge 3) {
        return "REFERENCE_ONLY"
    }
    return "REJECT"
}

function Get-Completeness {
    param(
        [string[]]$Markers,
        [string[]]$Features,
        [string]$BuildCommand,
        [string]$TestCommand
    )
    $score = 0
    if ((Get-ValueCount $Markers) -gt 0) { $score += 1 }
    if ((Get-ValueCount $Features) -ge 5) { $score += 2 } elseif ((Get-ValueCount $Features) -ge 2) { $score += 1 }
    if (-not [string]::IsNullOrWhiteSpace($BuildCommand)) { $score += 1 }
    if (-not [string]::IsNullOrWhiteSpace($TestCommand)) { $score += 1 }

    if ($score -ge 5) { return "high candidate" }
    if ($score -ge 3) { return "medium candidate" }
    if ($score -ge 1) { return "low candidate" }
    return "unknown"
}

function Analyze-Candidate {
    param([string]$Path)

    $resolved = (Resolve-Path -LiteralPath $Path).Path
    Write-Trace "Analyzing $resolved"
    $markers = Get-ProjectMarkers -Path $resolved
    $sample = Get-SafeTextSample -Path $resolved
    $git = Get-GitMetadata -Path $resolved
    $tech = Get-FrameworkAndCommands -Path $resolved -Markers $markers -Sample $sample
    $features = Get-RelevantFeatures -Sample $sample
    $license = Get-License -Path $resolved -Sample $sample
    $secretRisk = Get-SecretExposureRisk -Path $resolved -Sample $sample
    $disposition = Get-Disposition -Name (Split-Path $resolved -Leaf) -Features $features -SecretRisk $secretRisk -License $license
    $completeness = Get-Completeness -Markers $markers -Features $features -BuildCommand $tech.buildCommand -TestCommand $tech.testCommand

    return [ordered]@{
        projectName = Split-Path $resolved -Leaf
        fullPath = $resolved
        gitRemote = $git.remote
        gitRoot = $git.root
        currentBranch = $git.branch
        latestCommit = $git.latestCommit
        dirtyState = $git.dirtyState
        primaryLanguage = $tech.primaryLanguage
        framework = $tech.framework
        applicationType = $tech.applicationType
        buildCommand = $tech.buildCommand
        testCommand = $tech.testCommand
        databaseTechnology = $tech.databaseTechnology
        approximateCompleteness = $completeness
        lastMeaningfulModification = Get-LatestMeaningfulModification -Path $resolved -GitMetadata $git
        relevantFeatures = $features
        duplicateOrOverlappingFeatures = @($features | Where-Object { $_ -match "Inventory|Image|OCR|Pricing|Listing|Lot|CSV|Diagnostics" })
        license = $license
        reuseRisk = if ($license -eq "unknown") { "license review required" } elseif ($disposition -eq "REFERENCE_ONLY") { "manual review required" } else { "manageable" }
        secretExposureRisk = $secretRisk
        recommendedDisposition = $disposition
        projectMarkers = $markers
    }
}

function Find-CandidatePaths {
    param([string[]]$Roots)

    $candidatePaths = [ordered]@{}
    foreach ($root in $Roots) {
        if (-not (Test-Path -LiteralPath $root)) {
            Write-Warning "Source root not found: $root"
            continue
        }
        $rootInfo = Get-Item -LiteralPath $root
        $queue = New-Object System.Collections.Generic.Queue[System.IO.DirectoryInfo]
        $queue.Enqueue($rootInfo)

        while ($queue.Count -gt 0) {
            $directory = $queue.Dequeue()
            if (Test-ExcludedDirectory $directory) {
                continue
            }

            $gitDirectory = Join-Path $directory.FullName ".git"
            if (Test-Path -LiteralPath $gitDirectory) {
                $candidatePaths[$directory.FullName] = $true
                continue
            }

            if ($IncludeNonGitProjects) {
                $markers = Get-DirectProjectMarkersFast -Path $directory.FullName
                if ((Get-ValueCount $markers) -gt 0) {
                    $candidatePaths[$directory.FullName] = $true
                    continue
                }
            }

            foreach ($child in (Get-ChildDirectoriesSafe -Directory $directory)) {
                $queue.Enqueue($child)
            }
        }
    }

    return @($candidatePaths.Keys)
}

function New-DiscoveryReport {
    param([array]$Projects)

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("# Source Discovery Report") | Out-Null
    $lines.Add("") | Out-Null
    $lines.Add("Generated: $(Get-Date -Format o)") | Out-Null
    $lines.Add("") | Out-Null
    $lines.Add("Source roots: $($SourceRoots -join ', ')") | Out-Null
    $lines.Add("") | Out-Null
    $lines.Add("Projects found: $(Get-ValueCount $Projects)") | Out-Null
    $lines.Add("") | Out-Null
    $lines.Add("## High-priority candidates") | Out-Null
    $lines.Add("") | Out-Null

    $priority = @($Projects | Where-Object { $_.recommendedDisposition -in @("IMPORT_AND_REFACTOR", "IMPORT_AS_IS", "REIMPLEMENT_USING_EXISTING_BEHAVIOR") } | Sort-Object projectName)
    if ((Get-ValueCount $priority) -eq 0) {
        $lines.Add("No import candidates were identified automatically. Review the JSON output for low-signal projects.") | Out-Null
    }
    else {
        foreach ($project in $priority) {
            $lines.Add("### $($project.projectName)") | Out-Null
            $lines.Add("") | Out-Null
            $lines.Add("- Path: ``$($project.fullPath)``") | Out-Null
            $lines.Add("- Remote: ``$($project.gitRemote)``") | Out-Null
            $lines.Add("- Branch: ``$($project.currentBranch)``") | Out-Null
            $lines.Add("- Latest commit: ``$($project.latestCommit)``") | Out-Null
            $lines.Add("- Stack: $($project.primaryLanguage); $($project.framework); $($project.applicationType)") | Out-Null
            $lines.Add("- Completeness: $($project.approximateCompleteness)") | Out-Null
            $lines.Add("- Features: $($project.relevantFeatures -join ', ')") | Out-Null
            $lines.Add("- License: $($project.license)") | Out-Null
            $lines.Add("- Secret risk: $($project.secretExposureRisk)") | Out-Null
            $lines.Add("- Recommended disposition: $($project.recommendedDisposition)") | Out-Null
            $lines.Add("") | Out-Null
        }
    }

    $lines.Add("## All candidates") | Out-Null
    $lines.Add("") | Out-Null
    $lines.Add("| Project | Type | Stack | Features | Disposition | Risk |") | Out-Null
    $lines.Add("| --- | --- | --- | --- | --- | --- |") | Out-Null
    foreach ($project in ($Projects | Sort-Object projectName)) {
        $featureText = if ((Get-ValueCount $project.relevantFeatures) -gt 0) { @($project.relevantFeatures) -join ", " } else { "none detected" }
        $stack = "$($project.primaryLanguage) $($project.framework)".Trim()
        $lines.Add("| $($project.projectName) | $($project.applicationType) | $stack | $featureText | $($project.recommendedDisposition) | $($project.reuseRisk) |") | Out-Null
    }
    return $lines -join "`n"
}

function New-CapabilityMatrix {
    param([array]$Projects)

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("# Source Capability Matrix") | Out-Null
    $lines.Add("") | Out-Null
    $lines.Add("This matrix records automatically detected capability signals. It is an input to manual import review, not a substitute for code inspection.") | Out-Null
    $lines.Add("") | Out-Null
    $header = @("Project") + @($CapabilityKeywords.Keys) + @("Disposition")
    $lines.Add("| $($header -join ' | ') |") | Out-Null
    $lines.Add("| $((@('---') * (Get-ValueCount $header)) -join ' | ') |") | Out-Null
    foreach ($project in ($Projects | Sort-Object projectName)) {
        $row = New-Object System.Collections.Generic.List[string]
        $row.Add($project.projectName) | Out-Null
        foreach ($capability in $CapabilityKeywords.Keys) {
            if ($project.relevantFeatures -contains $capability) {
                $row.Add("yes") | Out-Null
            }
            else {
                $row.Add("") | Out-Null
            }
        }
        $row.Add($project.recommendedDisposition) | Out-Null
        $lines.Add("| $($row -join ' | ') |") | Out-Null
    }
    return $lines -join "`n"
}

$candidatePaths = Find-CandidatePaths -Roots $SourceRoots
$projects = New-Object System.Collections.Generic.List[object]
foreach ($path in $candidatePaths) {
    try {
        $projects.Add((Analyze-Candidate -Path $path)) | Out-Null
    }
    catch {
        $lineNumber = $_.InvocationInfo.ScriptLineNumber
        $lineText = $_.InvocationInfo.Line.Trim()
        Write-Warning "Failed to analyze ${path}: $($_.Exception.Message) at line ${lineNumber}: ${lineText}"
    }
}

$projectArray = @($projects | ForEach-Object { $_ })
$result = @{
    generatedAt = (Get-Date).ToString("o")
    sourceRoots = $SourceRoots
    includeNonGitProjects = [bool]$IncludeNonGitProjects
    projectCount = (Get-ValueCount -Value $projectArray)
    projects = $projectArray
}

if ($DryRun) {
    $result | ConvertTo-Json -Depth 20
    exit 0
}

$resolvedOutputPath = if ([System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath
}
else {
    Join-Path $RepoRoot $OutputPath
}
$outputDirectory = Split-Path $resolvedOutputPath -Parent
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $RepoRoot "docs") | Out-Null

$result | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $resolvedOutputPath -Encoding UTF8
New-DiscoveryReport -Projects $projectArray | Set-Content -LiteralPath (Join-Path $RepoRoot "docs\SOURCE-DISCOVERY-REPORT.md") -Encoding UTF8
New-CapabilityMatrix -Projects $projectArray | Set-Content -LiteralPath (Join-Path $RepoRoot "docs\SOURCE-CAPABILITY-MATRIX.md") -Encoding UTF8

Write-Host "Discovered $(Get-ValueCount $projects) candidate project(s)."
Write-Host "JSON: $resolvedOutputPath"
Write-Host "Report: $(Join-Path $RepoRoot 'docs\SOURCE-DISCOVERY-REPORT.md')"
Write-Host "Matrix: $(Join-Path $RepoRoot 'docs\SOURCE-CAPABILITY-MATRIX.md')"
