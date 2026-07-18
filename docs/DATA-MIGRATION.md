# Data Migration

eBay-Hero has two supported import paths:

- Legacy eBay Hero JSON catalog migration.
- CardOps SQLite import.

Both paths are idempotent and default to dry-run/read-only behavior.

## Legacy JSON

Dry-run:

```powershell
dotnet run --project .\tools\eBayHero.Migrator -- --dry-run
```

Apply to live operations root:

```powershell
dotnet run --project .\tools\eBayHero.Migrator -- --apply --allow-live
```

The migrator:

- Preserves legacy photo IDs.
- Preserves group IDs as inventory item IDs.
- Preserves paths, tags, statuses, metadata, OCR text, and confidence.
- Reports missing files with photo IDs, stored paths, file names, and up to five same-name candidate paths found near the missing location.
- Creates migration backups and reports.
- Writes the final `ReportPath` into both console output and the saved report JSON.
- Is idempotent.
- Never deletes the original JSON catalog.

## CardOps SQLite

Dry-run:

```powershell
.\scripts\import-cardops.ps1 -DryRun
```

Apply to live operations root:

```powershell
.\scripts\import-cardops.ps1 -Apply -AllowLive
```

Override the source database:

```powershell
.\scripts\import-cardops.ps1 -DryRun -SourceDb D:\WORK\GitRepos\PERSONAL\cardops\data\cardops.db
```

The CardOps importer:

- Reads `card_instances`, `image_assets`, and `directory_roots`.
- Imports CardOps cards as `InventoryItem` records with stable `cardops-card-*` IDs.
- Imports CardOps image assets as `Photo` records with stable `cardops-image-*` IDs.
- Links CardOps images to imported CardOps cards when `card_instance_id` is present.
- Imports CardOps roots, tags, and CardOps-only fields as `CustomFieldValue` records named `CardOps.*`.
- Marks missing CardOps image paths as missing instead of dropping the records.
- Creates a target SQLite backup before live apply.
- Tolerates repeat runs and existing records with the same photo/root paths.

