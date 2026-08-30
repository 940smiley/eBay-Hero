# Rename-ToEBayHero.ps1
# Script to rename "eBayHero" to "eBayHero" / "eBay Hero" in contents, files, and folders.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Write-Host "Rebranding repository at $RepoRoot..."

# 1. File content replacement
$Extensions = @("*.cs", "*.xaml", "*.csproj", "*.sln", "*.md", "*.json", "*.ps1", "*.txt", "*.manifest", "*.config")
$ExcludeFolders = @(".git", "node_modules", ".venv", "bin", "obj", "artifacts", "packages", ".gemini")

Write-Host "Updating file contents..."
Get-ChildItem -Path $RepoRoot -Recurse -File | Where-Object {
    $file = $_
    $relative = $file.FullName.Substring($RepoRoot.Length + 1)
    $skip = $false
    foreach ($folder in $ExcludeFolders) {
        if ($relative -like "$folder\*" -or $relative -eq $folder -or $relative -like "*\$folder\*") {
            $skip = $true
            break
        }
    }
    # Check extension
    if (-not $skip) {
        $matchedExt = $false
        foreach ($ext in $Extensions) {
            if ($file.Name -like $ext) {
                $matchedExt = $true
                break
            }
        }
        $skip = -not $matchedExt
    }
    -not $skip
} | ForEach-Object {
    $filePath = $_.FullName
    $content = Get-Content -Raw -LiteralPath $filePath
    $modified = $false
    
    if ($content -match "eBayHero") {
        $content = $content -replace "eBayHero", "eBayHero"
        $modified = $true
    }
    if ($content -match "eBay Hero") {
        $content = $content -replace "eBay Hero", "eBay Hero"
        $modified = $true
    }
    if ($content -match "ebay-hero") {
        $content = $content -replace "ebay-hero", "ebay-hero"
        $modified = $true
    }
    
    if ($modified) {
        Write-Host "  Updated content: $filePath"
        Set-Content -LiteralPath $filePath -Value $content -Encoding UTF8
    }
}

# 2. Rename files
Write-Host "Renaming files..."
Get-ChildItem -Path $RepoRoot -Recurse -File | Where-Object {
    $file = $_
    $relative = $file.FullName.Substring($RepoRoot.Length + 1)
    $skip = $false
    foreach ($folder in $ExcludeFolders) {
        if ($relative -like "$folder\*" -or $relative -eq $folder -or $relative -like "*\$folder\*") {
            $skip = $true
            break
        }
    }
    $file.Name -match "eBayHero" -and -not $skip
} | ForEach-Object {
    $oldPath = $_.FullName
    $newName = $_.Name -replace "eBayHero", "eBayHero"
    $newPath = Join-Path $_.DirectoryName $newName
    Write-Host "  Renaming file: $oldPath -> $newName"
    Rename-Item -LiteralPath $oldPath -NewName $newName
}

# 3. Rename directories
Write-Host "Renaming directories..."
# We sort by depth descending to rename child directories before parent directories
Get-ChildItem -Path $RepoRoot -Recurse -Directory | Where-Object {
    $dir = $_
    $relative = $dir.FullName.Substring($RepoRoot.Length + 1)
    $skip = $false
    foreach ($folder in $ExcludeFolders) {
        if ($relative -like "$folder\*" -or $relative -eq $folder -or $relative -like "*\$folder\*") {
            $skip = $true
            break
        }
    }
    $dir.Name -match "eBayHero" -and -not $skip
} | Sort-Object { $_.FullName.Length } -Descending | ForEach-Object {
    $oldPath = $_.FullName
    $newName = $_.Name -replace "eBayHero", "eBayHero"
    Write-Host "  Renaming folder: $oldPath -> $newName"
    Rename-Item -LiteralPath $oldPath -NewName $newName
}

Write-Host "Rebranding complete!"

