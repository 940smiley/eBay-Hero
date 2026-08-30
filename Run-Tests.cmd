@echo off
setlocal
cd /d "%~dp0"
echo Running tests...
powershell -ExecutionPolicy Bypass -NoProfile -File "csharp\scripts\test.ps1" %*
pause

