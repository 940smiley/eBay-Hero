# Storage Audit

Run:

```powershell
.\scripts\storage\Audit-And-Relocate-DevStorage.ps1 -DryRun -ProjectOnly
```

The audit defaults to dry run, inventories project-owned artifacts/test results and optionally caches, classifies candidates, and writes a manifest under `E:\eBayHero-Tools\manifests`.

Protected Windows, Program Files, ProgramData, user profile, AppData-as-a-whole, driver, recovery, registry-backed, and service-owned directories are not moved.

