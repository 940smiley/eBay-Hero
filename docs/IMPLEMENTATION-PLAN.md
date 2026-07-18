# eBay Hero Production Rewrite Implementation Plan

## Scope

Build a production Windows desktop application from the PowerShell prototype using C#, .NET 8, WPF, MVVM, SQLite, Entity Framework Core, dependency injection, hosting, structured logging, persisted jobs, and safe asynchronous file operations.

The PowerShell implementation is retained as a reference under `legacy-powershell/` and remains in place until the new application has migrated the existing JSON catalog.

## Safety Defaults

- Development operations must not modify `F:\Inventory` or `D:\INVENTORY_PHOTO_OPS` unless an explicit safe-mode override is supplied.
- Tests must use temporary directories.
- The migrator must default to dry-run behavior unless `--apply` is supplied.
- Import, rename, move, organize, and export workflows must preview changes before writing files.
- SQLite changes that correspond to file changes must occur transactionally after successful file operations.
- A file-operation journal and audit events must be written for reversible operations.

## Solution Structure

```text
eBayHero.sln
src/
  eBayHero.App/
  eBayHero.Core/
  eBayHero.Infrastructure/
  eBayHero.Ocr/
  eBayHero.FileSystem/
  eBayHero.Export/
tests/
  eBayHero.Core.Tests/
  eBayHero.Infrastructure.Tests/
  eBayHero.IntegrationTests/
  eBayHero.UiTests/
tools/
  eBayHero.Migrator/
  eBayHero.Cli/
installer/
scripts/
docs/
legacy-powershell/
```

## Implementation Phases

1. **Foundation**
   - Create the .NET solution and projects.
   - Add package references for EF Core SQLite, hosting, logging, CommunityToolkit.Mvvm, test tooling, and packaging helpers.
   - Add domain models, enums, DTOs, and service interfaces.
   - Add application settings with safe default paths and path validation.

2. **Persistence**
   - Implement `InventoryDbContext`.
   - Create EF Core migrations.
   - Implement repositories/query services for photos, items, source roots, OCR, exports, settings, jobs, and audit events.
   - Add database backup and integrity checks.

3. **Migration**
   - Implement idempotent JSON migration from `D:\INVENTORY_PHOTO_OPS\db\inventory-index.json`.
   - Preserve IDs, paths, groups, tags, metadata, OCR text, statuses, and missing-file status.
   - Write timestamped backup and migration reports.
   - Provide dry-run and apply commands in `eBayHero.Migrator` and `eBayHero.Cli`.

4. **File System**
   - Implement path normalization, filename/template rendering, collision detection, hashing, perceptual hash, import planning, rename planning, move planning, journaling, rollback, and undo.
   - Implement scanning for multiple source roots with exclusions.
   - Implement safe copy/move/index-in-place import workflows.

5. **Thumbnails**
   - Implement async thumbnail generation and cache invalidation under `D:\INVENTORY_PHOTO_OPS\cache\thumbnails`.
   - Load images without locking originals.
   - Provide placeholder thumbnails for missing files.

6. **OCR**
   - Implement configurable Tesseract invocation with TSV parsing.
   - Implement OCR profiles, preprocessing plans, multi-pass candidate collection, scoring, and persisted candidates.
   - Implement adaptive learning through token/field corrections and vocabulary/user-words generation.
   - Implement training-data sample export without claiming it is neural model training.

7. **Export**
   - Implement eBay temp export that copies images, preserves item ordering, creates CSV/JSON manifests and README, and offers status updates to `Drafted` or `Currently Listed`.
   - Add an eBay service abstraction without live API integration.

8. **Background Jobs**
   - Implement persisted jobs for scans, imports, hashing, thumbnails, OCR, moves, renames, exports, missing-file checks, and duplicate detection.
   - Support cancellation, retry, progress, restart-safe resume where possible, and diagnostics.

9. **WPF Application**
   - Build a virtualized main grid with sortable/resizable columns, column chooser, saved layouts, combined filters, context menus, keyboard shortcuts, and no clipped headers/rows.
   - Build image preview with zoom, pan, rotate, previous/next, group strip, open folder, copy path, and primary-image action.
   - Build Settings, OCR Review, Metadata Editor, Source Roots, Export, Jobs, Diagnostics, and Migration screens.
   - Use async commands and cancellation tokens for long operations.

10. **Tests**
    - Unit tests for path, naming, templates, grouping, status transitions, OCR scoring, corrections, metadata extraction, duplicate detection, and export ordering.
    - Integration tests for SQLite migrations, JSON migration, import modes, rollback, OCR fixture invocation, thumbnails, export manifests, and backup/restore.
    - UI smoke tests for startup, visible grid headers/first row, filters, selection, OCR review, settings, grouping, and export dialog.

11. **Packaging and CI**
    - Add build, test, publish, installer, verify, and clean scripts.
    - Produce a self-contained win-x64 portable ZIP.
    - Add installer scaffold and document current installer limitations if WiX/MSIX tooling is unavailable locally.
    - Add GitHub Actions for build, tests, static analysis, dependency review, CodeQL, and release artifacts.

## Acceptance Checklist

- Solution restores.
- Release build succeeds.
- Unit and integration tests pass.
- WPF app launches.
- JSON migration dry-run and apply paths are implemented and idempotent.
- At least hundreds of records can load asynchronously without blocking the UI.
- Grid headers and first row are visible.
- OCR jobs store candidates and confidence.
- OCR corrections regenerate custom vocabulary.
- Groups support ordered multi-image items.
- eBay export generates adjacent images plus CSV/JSON/README.
- File operations have preview, journal, and rollback hooks.
- Repeated scans avoid duplicate database entries.
- Missing files are reported without crashing.
- Portable package is produced.
- Known limitations are documented.


