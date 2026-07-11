# First Run

Use the Control Center or PowerShell:

```powershell
.\scripts\first-run.ps1 -Demo
```

Default first run is safe/demo mode. Live migration requires:

```powershell
.\scripts\first-run.ps1 -Live -ApplyMigration
```

The workflow checks the repository path, Windows/PowerShell/.NET, Tesseract, path configuration, script parsing, build, tests, migration dry-run, release build, portable package, and installer package. Failures create `artifacts\failure-bundles\<timestamp>`.
