# Contributing

1. Use a branch named `codex/<purpose>` or another descriptive feature branch.
2. Do not commit secrets, runtime databases, raw inventory exports, logs, thumbnails, or generated release artifacts.
3. Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test.ps1
```

4. For low disk space machines, set:

```powershell
$env:EA_BUILD_ROOT='D:\WORK\BuildArtifacts\ebay-assistance'
$env:NUGET_PACKAGES='D:\WORK\.nuget-packages'
```

