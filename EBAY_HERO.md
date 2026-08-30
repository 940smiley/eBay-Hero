# eBay Hero — product reference

> Compact reference for the target product. Generated 2026-08-12 from the canonical repo at `D:\WORK\GitRepos\PERSONAL\Commerce, eBay & Collectibles\ebay assistance\` (current VERSION: `eBay-Hero 1.3.0`).

## What it is

A local-first, AI-assisted eBay listing and inventory management platform. One app that:

- Ingests photos from one or more image roots.
- Identifies items (sports cards, collectibles, stamps).
- Reads text off labels and certs (OCR).
- Suggests prices from comparable sales.
- Builds lots and listing drafts.
- Validates drafts against eBay Sandbox (no live publishing by default).
- Exports eBay-safe CSVs and listing manifests.
- Keeps everything in a local SQLite database with diagnostics.

## Architecture (locked in `docs/adr/0001-canonical-architecture.md`)

```
eBayHero.Core           platform-neutral domain, enums, service contracts, business rules
eBayHero.Infrastructure EF Core SQLite persistence, migrations, legacy data imports
eBayHero.FileSystem     image root scanning, hashing, file safety, operation planning
eBayHero.Ocr            local OCR, image preprocessing, nondestructive edit ops
eBayHero.Export         eBay-safe CSV/export services
eBayHero.App            Windows WPF production shell
eBayHero.Web            ASP.NET companion surface
eBayHero.Bridge         eBay OAuth + Inventory API client (sandbox-gated)
eBayHero.Plugins.*      (planned, Phase C) host + abstractions for plugin ecosystem
```

## Status by phase (from ROADMAP.md)

| Phase | Target | Status |
|---|---|---|
| 0 — Source discovery + consolidation | 0.1 | Functional baseline |
| 1 — Windows production vertical slice | 0.2 | Functional beta |
| 2 — Full inventory + image workflow | 0.3 | In progress |
| 3 — OCR + identification completion | 0.4 | In progress |
| 4 — Pricing + lot workflows | 0.5 | In progress |
| 5 — eBay Sandbox integration | 0.6 | Not started |
| 6 — Windows public release | 1.0 | Blocked (release root / signing) |
| 7 — Linux release | 1.1 | Blocked (Linux UI shell) |
| 8 — iOS beta | 1.2 | Working scaffold only |
| 9 — Android beta | 1.3 | Working scaffold only |
| 10 — Cloud sync + team features | 2.0 | Not started |

Current verification: `dotnet test` → 37 passed, 0 warnings, 0 errors.

## Two new tracks added by the consolidation plan

- **Track P — Plugin ecosystem:** P1 runtime (Phase C of plan), P2 first-party plugins (Phase D), P3 third-party marketplace (C5), P4 capability audit automation.
- **Track R — Production readiness:** R1 eBay Sandbox, R2 release, R3 cross-platform, R4 cloud sync.

## Tiers (from MONETIZATION_PLAN.md)

| Tier | Price | Includes |
|---|---|---|
| Free / local | $0 | manual inventory, local image intake, basic OCR, listing drafts, CSV export, demo/safe mode, local SQLite, diagnostics, CardOps dry-run migration |
| Pro | $19–$39/mo or $199–$399/yr | higher OCR volume, advanced image processing, AI-assisted identification, pricing-provider connectors, bulk listing generation, listing audits, lot recommendations, advanced automation, priority updates |
| Business | TBD | multi-store, multi-user, shared inventory, cloud sync, scheduled automation, advanced analytics, higher provider limits, support SLAs |

Neither flavor enables live eBay publication. Set `EA_UPGRADE_URL` for the public build's upgrade gate.

## Safety defaults

- Live eBay publishing is disabled by default.
- CardOps runtime data, `.ENV`, OAuth tokens, logs, thumbnails, local SQLite data are not imported.
- Build outputs are ignored and can be redirected with `EA_BUILD_ROOT`.
- Public Windows packaging is blocked on available local disk space or a redirected release root.
- eBay-bridge paths require explicit `EBAY_SANDBOX_TOKEN` and are sandbox-only until Phase 5 acceptance.

## Naming convention

| Surface | Spelling |
|---|---|
| Product name (UI, README, marketing) | eBay Hero |
| Repo folder (post-Phase B) | `EBAY HERO/` |
| Internal namespaces, projects, .sln, settings keys | `eBayHero.*` (unchanged for migration safety) |
| VERSION tag | `eBay-Hero X.Y.Z` |
| Plugin runtime | `eBayHero.Plugins` |

## Build (verified on this machine)

```bash
cd "D:/WORK/GitRepos/PERSONAL/Commerce, eBay & Collectibles/ebay assistance"
$env:EA_BUILD_ROOT='D:\WORK\BuildArtifacts\ebay-hero'
$env:NUGET_PACKAGES='D:\WORK\.nuget-packages'
dotnet restore .\eBayHero.sln --packages $env:NUGET_PACKAGES
dotnet build .\eBayHero.sln -c Release --no-restore
dotnet test .\eBayHero.sln -c Release --no-build
```

Or:
```bash
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test.ps1
```

## Product build flavors

```bash
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build-flavors.ps1 -Flavor All
```
- `Development`: unlimited premium testing.
- `Public`: 14-day / 25-action premium trial with an upgrade gate.

## Repo posture vs siblings

Of the 16 repos inventoried under `Commerce, eBay & Collectibles\`:
- 1 KEEP (this one)
- 3 MERGE (already or will-be consolidated)
- 1 OPTIONAL MODULE pending diff (Collectease)
- 9 ARCHIVE
- 1 UNKNOWN pending license audit (COLLECTIBLE_AI_)
- 2 transitioning from MERGE → OPTIONAL MODULE once wrapped as plugins (stamplicity-V2.0, ecommerce_autolister)

See `PROJECT_INVENTORY.md` and `CLASSIFICATION.md` for the full breakdown.

## Key docs

- Architecture: `docs/adr/0001-canonical-architecture.md`
- Source discovery: `docs/SOURCE-DISCOVERY-REPORT.md`
- Capability matrix: `docs/SOURCE-CAPABILITY-MATRIX.md`, `docs/FEATURE-CAPABILITY-MATRIX.md`
- Import provenance: `docs/IMPORT-PROVENANCE.md`
- Plugin ecosystem design: `docs/PLUGIN-ECOSYSTEM.md`, `docs/adr/0003-plugin-runtime.md` (planned)
- Roadmap: `ROADMAP.md`
- Monetization plan: `MONETIZATION_PLAN.md`
- Consolidation plan: `.hermes/plans/2026-08-12_163904-ebay-hero-consolidation.md`

---

## One-paragraph version (for quick reference)

eBay Hero (current VERSION `eBay-Hero 1.3.0`) is the local-first .NET 8 / WPF consolidation of the user's eBay-related projects, packaged as one Windows desktop app with SQLite persistence, OCR, image scanning, pricing/lot/listing domain services, eBay-safe export, CardOps SQLite import, and a 10-phase roadmap toward sandbox integration, public release, Linux, iOS, Android, and cloud sync. The consolidation adds a plugin runtime so the 16 inventoried sibling repos become first-party plugins (`stamper`, `collectible-id`, `bg-remover`, `lister`, `listing-ai`) instead of code imports, with obsolete repos (notably the ToS-violating `eBay-View-Bot`) snapshotted into `archive/` rather than deleted.