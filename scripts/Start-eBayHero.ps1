#!/usr/bin/env pwsh
<#
.SYNOPSIS
    eBay Hero — Start All Services

.DESCRIPTION
    Starts the API bridge (Python/FastAPI) and the PWA dev server (Vite) in parallel.
    For production, build the PWA first with: npm run build

.PARAMETER Production
    If set, uses the pre-built Vite dist and starts only the API bridge.

.EXAMPLE
    .\Start-eBayHero.ps1
    .\Start-eBayHero.ps1 -Production
#>

param(
    [switch]$Production
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot

Write-Host ""
Write-Host "  ███████╗██████╗  █████╗ ██╗   ██╗    ██╗  ██╗███████╗██████╗  ██████╗ " -ForegroundColor Cyan
Write-Host "  ██╔════╝██╔══██╗██╔══██╗╚██╗ ██╔╝    ██║  ██║██╔════╝██╔══██╗██╔═══██╗" -ForegroundColor Cyan
Write-Host "  █████╗  ██████╔╝███████║ ╚████╔╝     ███████║█████╗  ██████╔╝██║   ██║" -ForegroundColor Blue
Write-Host "  ██╔══╝  ██╔══██╗██╔══██║  ╚██╔╝      ██╔══██║██╔══╝  ██╔══██╗██║   ██║" -ForegroundColor Blue
Write-Host "  ███████╗██████╔╝██║  ██║   ██║       ██║  ██║███████╗██║  ██║╚██████╔╝" -ForegroundColor Magenta
Write-Host "  ╚══════╝╚═════╝ ╚═╝  ╚═╝   ╚═╝       ╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝ ╚═════╝ " -ForegroundColor Magenta
Write-Host ""
Write-Host "  eBay Hero Command Center — Startup Script" -ForegroundColor White
Write-Host "  ────────────────────────────────────────────────────────────────────────" -ForegroundColor DarkGray
Write-Host ""

# ─── API Bridge Setup ────────────────────────────────────────────────────────

$BridgeDir = Join-Path $Root "src\eBayHero.Bridge"
$EnvFile   = Join-Path $BridgeDir ".env"

if (-not (Test-Path $EnvFile)) {
    $Example = Join-Path $BridgeDir ".env.example"
    if (Test-Path $Example) {
        Copy-Item $Example $EnvFile
        Write-Host "  [SETUP] Created .env from .env.example — fill in your eBay credentials!" -ForegroundColor Yellow
        Write-Host "          Path: $EnvFile" -ForegroundColor Yellow
        Write-Host ""
    }
}

# Check Python
$PythonCmd = "python"
try {
    & python --version 2>&1 | Out-Null
} catch {
    $PythonCmd = "python3"
}

# Install requirements if needed
$ReqFile = Join-Path $BridgeDir "requirements.txt"
Write-Host "  [BRIDGE] Installing/checking Python dependencies..." -ForegroundColor DarkCyan
& $PythonCmd -m pip install -q -r $ReqFile

Write-Host ""
Write-Host "  [BRIDGE] Starting API Bridge on http://127.0.0.1:8000" -ForegroundColor Green

$BridgeJob = Start-Job -ScriptBlock {
    param($dir, $cmd)
    Set-Location $dir
    & $cmd -m uvicorn api_bridge:app --host 127.0.0.1 --port 8000 --reload 2>&1
} -ArgumentList $BridgeDir, $PythonCmd

# ─── PWA Setup ───────────────────────────────────────────────────────────────

$WebDir = Join-Path $Root "src\eBayHero.Web"

if ($Production) {
    Write-Host "  [WEB]    Production mode — Serve dist with: npx serve dist" -ForegroundColor Yellow
    Write-Host "           Or deploy dist/ to any static host (Netlify, Vercel, GitHub Pages)" -ForegroundColor DarkGray
} else {
    if (-not (Test-Path (Join-Path $WebDir "node_modules"))) {
        Write-Host "  [WEB]    Installing npm packages..." -ForegroundColor DarkCyan
        Push-Location $WebDir
        npm install
        Pop-Location
    }

    Write-Host "  [WEB]    Starting Vite dev server on http://localhost:5173" -ForegroundColor Green

    $WebJob = Start-Job -ScriptBlock {
        param($dir)
        Set-Location $dir
        npm run dev 2>&1
    } -ArgumentList $WebDir
}

Write-Host ""
Write-Host "  ✅ eBay Hero is starting up!" -ForegroundColor Green
Write-Host ""
Write-Host "     API Bridge:  http://127.0.0.1:8000" -ForegroundColor Cyan
Write-Host "     PWA:         http://localhost:5173" -ForegroundColor Cyan
Write-Host "     API Docs:    http://127.0.0.1:8000/docs" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Press Ctrl+C to stop all services." -ForegroundColor DarkGray
Write-Host ""

try {
    while ($true) {
        # Stream job output
        Receive-Job -Job $BridgeJob | Write-Host -ForegroundColor DarkGray
        if (-not $Production) {
            Receive-Job -Job $WebJob | Write-Host -ForegroundColor DarkGray
        }
        Start-Sleep -Milliseconds 500
    }
} finally {
    Write-Host "`n  Stopping all services..." -ForegroundColor Yellow
    Stop-Job $BridgeJob
    Remove-Job $BridgeJob
    if (-not $Production -and $WebJob) {
        Stop-Job $WebJob
        Remove-Job $WebJob
    }
    Write-Host "  Stopped." -ForegroundColor DarkGray
}
