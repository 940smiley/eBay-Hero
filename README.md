# eBay Assistance

eBay Assistance is the canonical consolidation repository for CardOps and eBay Hero.

Current implementation status: Windows production baseline imported from `eBayHero`, with .NET 8 WPF UI, SQLite persistence, image scanning, OCR services, pricing/lot/listing domain services, eBay-safe export, CardOps SQLite import support, CLI/migrator tools, tests, and release scripts.

## Build

This machine's `C:` drive is nearly full, so use redirected build roots:

```powershell
$env:EA_BUILD_ROOT='D:\WORK\BuildArtifacts\ebay-assistance'
$env:NUGET_PACKAGES='D:\WORK\.nuget-packages'
dotnet restore .\eBayHero.sln --packages $env:NUGET_PACKAGES
dotnet build .\eBayHero.sln -c Release --no-restore
dotnet test .\eBayHero.sln -c Release --no-build
```

Or use:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test.ps1
```

## Product build flavors

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build-flavors.ps1 -Flavor All
```

- `Development`: unlimited premium testing.
- `Public`: 14-day/25-action premium trial with an upgrade gate.

Set `EA_UPGRADE_URL` to the hosted production checkout URL before distributing the public build. Neither flavor enables live eBay publication.

## Key Docs

- [Architecture](docs/adr/0001-canonical-architecture.md)
- [Source discovery](docs/SOURCE-DISCOVERY-REPORT.md)
- [Import provenance](docs/IMPORT-PROVENANCE.md)
- [Roadmap](ROADMAP.md)
- [Monetization plan](MONETIZATION_PLAN.md)
- [User actions required](docs/USER-ACTION-REQUIRED.md)
- [Diagnostics](docs/DIAGNOSTICS.md)

## Current Verification

- `dotnet restore`: passed
- `dotnet build -c Release --no-restore`: passed, 0 warnings, 0 errors
- `dotnet test -c Release --no-build`: passed, 37 tests

## Safety Defaults

- Live eBay publishing is not enabled by default.
- CardOps runtime data, `.ENV`, OAuth token data, logs, thumbnails, and local SQLite data were not imported.
- Build outputs are ignored and can be redirected with `EA_BUILD_ROOT`.
- Public Windows packaging is blocked on available local disk space or a redirected release root.


