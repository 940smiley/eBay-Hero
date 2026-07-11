# Storage Relocation

Relocation is manifest-driven and conservative.

Current tooling produces an audit manifest and verification report. Apply mode is intentionally limited until item-level approval is implemented. Installed applications must be relocated only through official installer support, supported reinstall, Windows Settings move support, or package-manager parameters.

Install reviewed storage tools:

```powershell
.\scripts\install-storage-tools.ps1
```
