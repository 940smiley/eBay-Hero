# Production Completion Plan

eBay Hero is now a .NET 8 WPF/SQLite application with preserved PowerShell legacy code. The current repository is not a Git checkout, so no Git checkpoint can be created from this folder. Baseline build and tests were run on 2026-07-01 before new implementation work.

## Baseline

- Repository path: `D:\WORK\Projects\ACTIVE\eBayHero`
- Build: `dotnet build .\eBayHero.sln -c Release` passed.
- Tests: `dotnet test .\eBayHero.sln -c Release --no-build` passed, 9 total.
- Existing live migration dry-run from the previous release read 826 photos with no missing files.
- Current script packaging produces a portable ZIP and a script installer.

## Current Architecture

- `eBayHero.Core`: domain entities, options, service contracts, metadata helpers.
- `eBayHero.Infrastructure`: EF Core SQLite context, migrations, JSON migration, database maintenance.
- `eBayHero.FileSystem`: scanning, hashing, safe import planning.
- `eBayHero.Ocr`: Tesseract invocation, candidate scoring, correction/vocabulary learning.
- `eBayHero.Export`: copy-only eBay export manifests.
- `eBayHero.App`: WPF host, inventory grid, preview, basic scan/OCR/export commands.
- `tools`: CLI and migrator.
- `scripts`: build, test, release, packaging helpers.

## Audit Findings

- The visible WPF surface is primarily an inventory grid. It has working commands for refresh, migration dry run, scan roots in live mode, OCR selected, export selected, and database check.
- OCR runs multiple Tesseract PSM strategies but did not previously generate real derived/preprocessed images or crop artifacts.
- The database schema lacks production tables for pricing, lots, marketplace listings, listing audits, detailed image edits, OCR image artifacts, eBay connection state, and background job attempts.
- Source roots exist as data but there is no full root-management workspace.
- File planning is safe and dry-run oriented, but execution, journaling, and rollback are incomplete.
- eBay export exists as local file preparation. OAuth, secure token storage, capabilities, and official API adapters are not complete.
- The launcher scripts are useful but not consolidated into a control center.
- Dependency path policy is implicit in options and docs, not yet governed by `config\paths.json`.
- No KnowledgeBase discovery tooling exists.
- PowerShell cleanup is repository-scoped and safe, but first-run/failure-bundle/storage-audit workflows are missing.
- Existing installer is script-based, not MSI/MSIX/WiX.

## Implementation Sequence

1. Add production domain model extensions while preserving the initial schema and migration path.
2. Add service contracts and deterministic implementations for image preprocessing, OCR review data, grouping, pricing, lot building, listings, audits, eBay capability status, secret redaction, jobs, and path safety.
3. Add EF migration for the production foundation tables.
4. Extend tests with temporary-directory coverage for nondestructive operations and credential-safe behavior.
5. Expand WPF from a single grid into operational tabs backed by real data and commands.
6. Add a data-driven PowerShell Control Center and script manifest.
7. Add first-run, doctor, restore, migration, launch, failure-bundle, path verification, KnowledgeBase discovery, and storage-audit scripts.
8. Add eBay setup docs and a mock/sandbox-capable provider. Publishing remains disabled until credentials, OAuth consent, and explicit user confirmation are present.
9. Improve release scripts to produce manifests, checksums, and installer packages.
10. Run build, tests, PowerShell parse checks, migration dry-run, release verification, and app/control-center smoke checks.

## Safety Rules

- Default mode remains safe/demo.
- Live inventory paths are `D:\INVENTORY_PHOTO_OPS` and `F:\Inventory`.
- Live scan, migration, launch, publish, move, rename, and delete require explicit live authorization.
- Originals are never overwritten by OCR/image-edit workflows unless an explicit overwrite command is added and confirmed.
- Secret values must not be written to source, committed config, logs, SQLite plain-text fields, reports, or UI failure output.
- KnowledgeBase discovery is read-only by default.
- Storage relocation scripts default to dry run and may only operate on allowlisted project-owned caches, artifacts, or portable tools.

## External Blocks

- eBay production OAuth and publishing require the user's eBay developer application credentials, RuName/redirect setup, user consent, and any eBay approvals required for the selected APIs.
- Seller order/sales history and analytics availability depends on eBay account/API authorization. The app must display capability status rather than faking sold comparable access.
- MSI/MSIX production signing depends on a signing certificate and installer technology decision.

## Acceptance Tracking

The production acceptance list is tracked in `docs/FEATURE-CAPABILITY-MATRIX.md`. Features marked Complete have working code and tests. Credential-gated or external-approval-gated features must still have local interfaces, secure storage, mock/sandbox paths, docs, and disabled production actions.

