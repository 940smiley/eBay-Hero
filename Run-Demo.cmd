@echo off
setlocal
cd /d "%~dp0"
echo Starting Demo / Launching App...
powershell -ExecutionPolicy Bypass -NoProfile -File "csharp\scripts\launch.ps1" %*
pause

