# Import Provenance

This file records reviewed source imports into the canonical `eBay Assistance` repository.

## eBayHero Production Source

- Original repository: `D:\WORK\Projects\ACTIVE\eBayHero`
- Original commit: `b7f67096ad0def040146db6d3d32f50a132d5ac9`
- Original branch: `ebay-hero/integration`
- Original remote: none configured locally
- Original license: unknown; treat as user-owned local code pending final license selection
- Destination paths:
  - `eBayHero.sln`
  - `src/eBayHero.*`
  - `tests/eBayHero.*`
  - `tools/eBayHero.*`
  - selected `scripts/`
  - selected `.github/workflows/`
  - selected `docs/`
- Import classification: `IMPORT_AND_REFACTOR`
- Reason for importing: strongest discovered Windows production implementation with domain, SQLite, OCR, file scanning, eBay export, CardOps migration support, scripts, and tests.
- Security review result: import source only; exclude local config, `config/paths.json`, `config.json`, runtime artifacts, logs, test results, user data, and generated packages.
- Tests added or preserved: existing unit, integration, and UI test projects imported for canonical verification.
- Changes made during import: pending build verification and product naming cleanup.

## CardOps AI

- Original repository: `D:\WORK\GitRepos\PERSONAL\cardops`
- Original commit: `b0d1aa79de3a28955990da1d47162d7fc17626d7`
- Original branch: `codex/planned-updates`
- Original remote: `https://github.com/940smiley/cardops.git`
- Original license: unknown
- Destination paths: none imported directly in the initial source commit.
- Import classification: `REFERENCE_ONLY` initially, with targeted migration through `eBayHero.Infrastructure\Migration\CardOpsImportService.cs`.
- Reason for deferring direct import: repository contains `.ENV`, `data/ebay-oauth-token.json`, runtime SQLite data, logs, generated thumbnails, and other local artifacts. Full copy would violate the no-secret/no-runtime-data rule.
- Security review result: do not import CardOps runtime data or local configuration. Review individual algorithms before any future direct code import.
- Useful behavior to preserve: image roots, image ingestion, SHA/perceptual hash concepts, OCR fallback, demo mode, eBay-safe CSV exports, diagnostics, and safe launcher behavior.


