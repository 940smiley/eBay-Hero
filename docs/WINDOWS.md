# Windows

Status: functional beta baseline.

Implemented:

- .NET 8 WPF app.
- SQLite infrastructure.
- Image scanning, hashing, OCR services, pricing, lot builder, listing drafts, audits, export.
- CLI and migrator tools.
- Build and test scripts.
- Portable publish script.

Verified:

```powershell
$env:EA_BUILD_ROOT='D:\WORK\BuildArtifacts\ebay-assistance'
$env:NUGET_PACKAGES='D:\WORK\.nuget-packages'
dotnet build .\InventoryPhotoOps.sln -c Release --no-restore
dotnet test .\InventoryPhotoOps.sln -c Release --no-build
```

Release verification also passed with:

```powershell
$env:EA_RELEASE_ROOT='D:\WORK\BuildArtifacts\ebay-assistance\release\windows'
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-release.ps1
```

Artifacts produced:

- `D:\WORK\BuildArtifacts\ebay-assistance\release\windows\eBay-Assistance-win-x64-portable.zip`
- `D:\WORK\BuildArtifacts\ebay-assistance\release\windows\eBay-Assistance-script-installer.zip`
- matching `.sha256` checksum files

Blockers for public release:

- Local `C:` drive has too little free space for release packaging unless `EA_RELEASE_ROOT` is set.
- The current installer artifact is a script-installer ZIP. A maintained installer technology and code signing are still required before public distribution.
