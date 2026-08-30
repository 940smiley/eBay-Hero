@echo off
setlocal
cd /d "%~dp0"
echo Publishing Deployment...
powershell -ExecutionPolicy Bypass -NoProfile -File "csharp\scripts\publish-windows.ps1" %*
pause

