# Installation

## Portable

Run:

```powershell
.\scripts\publish-portable.ps1
```

Extract:

```text
artifacts\InventoryPhotoOps-win-x64-portable.zip
```

Launch:

```text
InventoryPhotoOps.App.exe
```

## Script Installer

Run:

```powershell
.\scripts\build-installer.ps1
```

This creates:

```text
artifacts\InventoryPhotoOps-script-installer.zip
```

Extract it and run:

```powershell
.\Install-InventoryPhotoOps.ps1
```

The script installs per-user under `%LOCALAPPDATA%\InventoryPhotoOps` and creates a Start Menu shortcut. It does not delete user data on uninstall.

Installed shortcuts pass `--allow-live` so the app uses the configured live operations root. Development launches omit that flag and use a safe local dev root.
