[CmdletBinding()]
param()

Write-Error 'Linux packaging is blocked by the current WPF-only UI. The accepted architecture keeps domain services portable and requires an Avalonia or other Linux UI shell before AppImage/.deb packaging.'
exit 2

