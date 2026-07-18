# ADR 0001: Canonical Architecture

Status: Accepted

Date: 2026-07-11

## Context

The consolidation repository starts empty, but local discovery found two primary sources:

- `D:\WORK\Projects\ACTIVE\eBayHero` at commit `b7f67096ad0def040146db6d3d32f50a132d5ac9`, a .NET 8 solution with WPF UI, domain/core projects, EF Core SQLite, OCR, file scanning, eBay export, CLI, migrator, scripts, and tests.
- `D:\WORK\GitRepos\PERSONAL\cardops` at commit `b0d1aa79de3a28955990da1d47162d7fc17626d7`, a Python FastAPI plus React/Vite local-first CardOps app with image ingestion, OCR fallback, eBay-safe CSV exports, demo data, and launch scripts.

CardOps contains local `.ENV`, token data, logs, thumbnails, and SQLite runtime data. It is valuable, but it needs selective import and migration handling rather than a full repository copy.

## Decision

Use the eBayHero .NET 8 solution as the initial canonical production codebase for Windows, while preserving platform-neutral domain and infrastructure boundaries for Linux and mobile work.

The canonical architecture is:

- `eBayHero.Core`: platform-neutral domain models, enums, service contracts, and deterministic business rules.
- `eBayHero.Infrastructure`: EF Core SQLite persistence, migrations, maintenance, and legacy data imports.
- `eBayHero.FileSystem`: image root scanning, hashing, file safety, and operation planning.
- `eBayHero.Ocr`: local OCR, image preprocessing, and nondestructive edit operations.
- `eBayHero.Export`: eBay-safe CSV/export services.
- `eBayHero.App`: Windows WPF production shell for the first public release.
- `tools/*`: CLI and migrator entrypoints for automation and release checks.

CardOps will be consolidated through:

- the existing `CardOpsImportService` for SQLite migration;
- selective behavior/reference import for image root management, OCR fallback, and eBay-safe workflows;
- explicit exclusion of CardOps secrets, logs, local databases, thumbnails, and token files.

## Alternatives Considered

### Tauri 2 + React + Python/FastAPI

This matches CardOps more closely and offers a cross-platform story, but it requires packaging Python, coordinating local service processes, and reworking existing Windows production functionality. It is a good future direction only if the .NET desktop path cannot meet Linux/mobile goals.

### Avalonia-first rewrite

Avalonia would provide a cleaner Windows/Linux desktop target than WPF, but a rewrite before the Windows release would discard a working WPF vertical slice and delay packaging. Avalonia should be introduced after the domain, persistence, OCR, and export services are stable in the canonical repository.

### Keep both apps permanently

Keeping WPF eBayHero and CardOps as independent applications would preserve functionality short term but create duplicated domain models, incompatible databases, and divergent release processes. This is not acceptable for a production consolidation.

## Consequences

- Windows release reliability is prioritized first.
- Linux support will be implemented by reusing core/domain/infrastructure code and adding a Linux-compatible UI later.
- iOS and Android will start as scaffolds sharing contracts and behavior rather than claiming verified builds from Windows.
- CardOps secrets and runtime data are not imported.
- The product name is centralized in documentation and release configuration first; namespace renaming is deferred until after the imported solution builds and tests cleanly.


