# Installer

`scripts/build-installer.ps1` currently produces a script installer package:

```text
artifacts\InventoryPhotoOps-script-installer.zip
```

The package contains:

- A self-contained win-x64 portable payload.
- `Install-InventoryPhotoOps.ps1`, which installs per-user and creates Start Menu shortcuts.

MSI/MSIX packaging is the next packaging step.

