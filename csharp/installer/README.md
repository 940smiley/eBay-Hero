# Installer

`scripts/build-installer.ps1` currently produces a script installer package:

```text
artifacts\eBayHero-script-installer.zip
```

The package contains:

- A self-contained win-x64 portable payload.
- `Install-eBayHero.ps1`, which installs per-user and creates Start Menu shortcuts.

MSI/MSIX packaging is the next packaging step.


