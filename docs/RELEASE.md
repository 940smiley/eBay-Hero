# Release

Run:

```powershell
.\scripts\verify-release.ps1
.\scripts\build-installer.ps1
```

Artifacts:

```text
artifacts\InventoryPhotoOps-win-x64-portable.zip
artifacts\InventoryPhotoOps-script-installer.zip
```

## Known Limitations

- The installer is currently a script installer package, not an MSI/MSIX.
- Full in-app settings editing is not yet complete.
- OCR preprocessing now creates derived working images. Freehand custom crop drawing in the WPF UI is still a next UI refinement; the service accepts crop JSON.
- File move/rename rollback APIs are planned beyond import/export planning.
- UI automation tests currently validate XAML structure rather than driving a live window.
- Live eBay API integration is intentionally deferred.
