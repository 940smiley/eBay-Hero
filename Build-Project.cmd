@echo off
setlocal
cd /d "%~dp0"
echo Building Project...
powershell -ExecutionPolicy Bypass -NoProfile -File "csharp\scripts\build.ps1" %*
pause

