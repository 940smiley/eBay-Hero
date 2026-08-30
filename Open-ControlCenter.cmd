@echo off
setlocal
cd /d "%~dp0"
echo Opening Control Center...
powershell -ExecutionPolicy Bypass -NoProfile -File "csharp\scripts\eBayHero-ControlCenter.ps1" %*
pause

