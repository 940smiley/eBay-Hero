# Classification Report

> **eBay Hero consolidation — per-repo classification.**
> Generated 2026-08-12. Scope: every repo under `D:\WORK\GitRepos\PERSONAL\Commerce, eBay & Collectibles\`.
> Classification uses the user's vocabulary: `KEEP` / `MERGE` / `OPTIONAL MODULE` / `ARCHIVE` / `UNKNOWN`.
>
> Sources cross-referenced:
> - `PROJECT_INVENTORY.md` (folder, git, stack)
> - `docs/SOURCE-CAPABILITY-MATRIX.md` (auto-detected capability signals)
> - `docs/SOURCE-DISCOVERY-REPORT.md` (auto-disposition annotations)
> - Per-repo README headers and remote URLs

## How to read this

Each row answers the eight required questions, then assigns a classification:

- **KEEP** — canonical or already part of the canonical repo; do not move.
- **MERGE** — actively consolidate by importing its functionality (code, data, or both) into the canonical repo.
- **OPTIONAL MODULE** — keep as a separately installable plugin/module that complements eBay Hero; not merged into core.
- **ARCHIVE** — preserve snapshot in repo's `archive/` tree with manifest + checksum; do not delete.
- **UNKNOWN** — insufficient evidence to classify; needs review before any action.

## 1. ebay assistance (canonical)

- **Project name:** eBay Assistance (canonical) — to be renamed `eBay Hero` per `docs/RENAME-PLAN.md`.
- **Purpose:** Canonical consolidation repo. Production baseline for eBay-related listing, inventory, OCR, pricing, and eBay-bridge workflows.
- **Tech stack:** .NET 8, C#, WPF, ASP.NET, SQLite, Tesseract (optional), xUnit. 8 src projects + tests.
- **Status:** Active. v1.3.0. `dotnet test` green: 37 passed, 0 warnings, 0 errors.
- **Dependencies:** Internal — `eBayHero.{App,Core,Export,FileSystem,Infrastructure,Ocr,Bridge,Web}`. NuGet packages via redirected packages root.
- **APIs used:** None live. eBay Inventory + OAuth wired but gated; sandbox path only when `EBAY_SANDBOX_TOKEN` set.
- **Active/inactive:** **Active.**
- **Relevance:** **This is the target.** Phases 0/1 complete; 2/3/4 in progress.
- **Classification:** `KEEP`.

## 2. cardops

- **Project name:** CardOps AI.
- **Purpose:** Local-first sports & trading card ops app: inventory, image ingestion, listing review, lot planning, eBay-safe draft workflows.
- **Tech stack:** Python + TypeScript (FastAPI, OCR, React, Vite), monorepo layout. ~31,878 LOC code.
- **Status:** Active upstream; functionality already merged into `eBayHero.Infrastructure` via CardOps SQLite importer.
- **Dependencies:** FastAPI, React, Vite, OCR libs. No live eBay API.
- **APIs used:** None live (eBay-safe drafts only).
- **Active/inactive:** **Active upstream, but logically superseded by the canonical importer.**
- **Relevance:** **High.** Already partially imported; remaining artifacts (UI shell, FastAPI service) overlap core.
- **Classification:** `MERGE` — coverage test in Plan Task D5; archive upstream after.

## 3. EBAY_INVENTORY_PHOTO_ORGANIZER

- **Project name:** EBay Inventory Photo Organizer.
- **Purpose:** Inventory + photo organization + OCR + listing helper. The original "inventory photo ops" production baseline.
- **Tech stack:** Python + TypeScript (OpenCV, React, Vite) per matrix. ~56,291 LOC code.
- **Status:** Active upstream; baseline already imported into `eBayHero.Infrastructure` per `docs/IMPORT-PROVENANCE.md`.
- **Dependencies:** OpenCV, React, Vite, OCR tooling.
- **APIs used:** None live.
- **Active/inactive:** **Active upstream, superseded in our pipeline.**
- **Relevance:** **High.** Foundation of canonical inventory + photo ops.
- **Classification:** `MERGE` — already partially done; mark complete in Plan Task D4.

## 4. Stamplicity

- **Project name:** Stamplicity (v1).
- **Purpose:** AI Studio-style stamp image recognition / cataloging app (web).
- **Tech stack:** TypeScript, React, Vite. ~4,494 LOC.
- **Status:** Active upstream, but superseded by `stamplicity-V2.0`. No unique capability matrix signals beyond `REFERENCE_ONLY`.
- **Dependencies:** React, Vite.
- **APIs used:** None.
- **Active/inactive:** **Active but legacy.**
- **Relevance:** **Low.** V2.0 subsumes it.
- **Classification:** `ARCHIVE` — superseded by V2.0.

## 5. stamplicity-V2.0

- **Project name:** Stamplicity V2.0.
- **Purpose:** AI-powered philately platform — stamp image recognition, cataloging, collection management, appraisal workflows, marketplace listing support.
- **Tech stack:** TypeScript, React, Vite (Next.js-flavored). ~12,628 LOC.
- **Status:** Active. Strong capability signals (sports-card ID, file organization, desktop UI, listing support, Linux compat, tests, installer).
- **Dependencies:** React, Vite, image-recognition libs.
- **APIs used:** None live; marketplace support is local only.
- **Active/inactive:** **Active.**
- **Relevance:** **High.** Provides stamp-philately specialty on top of general eBay Hero. Front/back pairing, file organization, lot recommendations, metadata normalization all align with eBay Hero.
- **Classification:** `MERGE` (as a plugin) → `OPTIONAL MODULE` after Phase D1 wraps it as `plugins/stamper/` providing the `image-preprocessor` capability.

## 6. COLLECTIBLE_AI_

- **Project name:** COLLECTIBLE_AI_.
- **Purpose:** Web app shell for collectible identification; `findings.md`, `REPO_TODO.md`, prompts/, lib/, Prisma schema, Next.js pages.
- **Tech stack:** TypeScript, Next.js, React, Prisma. ~379,333 LOC including `node_modules`; far less of authored code.
- **Status:** Unknown license terms (matrix marks `REJECT` on license grounds). No git HEAD recorded, suggesting uninitialized state.
- **Dependencies:** Next.js, Prisma, large vendored set in `node_modules`.
- **APIs used:** None live (matrix shows only `iOS compatibility` signal).
- **Active/inactive:** **UNKNOWN.** No commit history; cannot confirm whether maintained.
- **Relevance:** **Medium.** Collectible identification is a target capability for the `listing-ai` plugin.
- **Classification:** `UNKNOWN` — needs license audit + authored-LOC diff (vs `node_modules`) before any action. If license cleared: `OPTIONAL MODULE` (`plugins/collectible-id/`, Plan Task D2).

## 7. ecommerce_autolister

- **Project name:** eCommerce Autolister.
- **Purpose:** "Autolisting bot" for FBMP and Etsy (eBay in progress). CSV I/O, desktop UI, lot recommendations, metadata normalization, eBay listing scaffolding.
- **Tech stack:** Python. ~4,306 LOC.
- **Status:** Active upstream; eBay integration explicitly WIP.
- **Dependencies:** Python stdlib + minimal libs per matrix.
- **APIs used:** FBMP, Etsy APIs (matrix signals); eBay is WIP.
- **Active/inactive:** **Active.**
- **Relevance:** **Medium.** Listing automation capability maps directly onto `plugins/lister/` in Plan Task D6.
- **Classification:** `MERGE` (as a plugin) → `OPTIONAL MODULE` after diff scan confirms capability delta vs core.

## 8. hydrogen

- **Project name:** Hydrogen (Shopify).
- **Purpose:** Shopify OSS React-based storefront framework.
- **Tech stack:** TypeScript, React, Vite. ~1,705 LOC counted (likely more with vendor files). Grafted commit history.
- **Status:** Active upstream as a Shopify framework.
- **Dependencies:** Shopify Hydrogen core.
- **APIs used:** Shopify Storefront API.
- **Active/inactive:** **Active but unrelated to eBay inventory.**
- **Relevance:** **None.** Not eBay tooling; capability matrix flags overlap (file pairing, listing generation, CSV) but those are eBay-irrelevant storefront features.
- **Classification:** `ARCHIVE` — out of scope; preserve snapshot, do not import.

## 9. freedom-fleamarket-biz-production

- **Project name:** Freedom Flea Market (production deployment).
- **Purpose:** Deployment files for `freedomfleamarket.biz`, a multi-vendor marketplace on WordPress/WooCommerce + GCP.
- **Tech stack:** WordPress, WooCommerce, GCP deployment configs.
- **Status:** Active.
- **Dependencies:** WordPress, WooCommerce, GCP.
- **APIs used:** WooCommerce REST, GCP services.
- **Active/inactive:** **Active.**
- **Relevance:** **None** as tooling. **Open question:** photo library could seed the AI listing layer's training/eval data.
- **Classification:** `ARCHIVE` — out of scope for core. Hold photos aside for potential seed dataset.

## 10. Collectease

- **Project name:** Collectease.
- **Purpose:** Collectibles management web app: organize, value, share collections. React + Material-UI + Vite, GitHub Pages deploy.
- **Tech stack:** TypeScript, React, Material-UI, Vite. ~76,038 LOC.
- **Status:** Active upstream.
- **Dependencies:** React, Material-UI.
- **APIs used:** None live (matrix: comparable sales, CSV I/O, listing gen, lot recommendations, pricing).
- **Active/inactive:** **Active.**
- **Relevance:** **Medium.** Overlaps core on listing gen, lot recommendations, pricing, CSV I/O.
- **Classification:** `OPTIONAL MODULE` — capability diff vs core is still pending; treat as reference until diff confirms overlap is complete.

## 11. recovered_treasures_app

- **Project name:** Recovered Treasures (app).
- **Purpose:** Generic web app shell for the Recovered Treasures storefront (matrix shows inventory mgmt, file renaming, lot recommendations, file organization, duplicate detection).
- **Tech stack:** mixed; ~592 LOC.
- **Status:** Active upstream.
- **Dependencies:** small.
- **APIs used:** none.
- **Active/inactive:** **Active but small/legacy.**
- **Relevance:** **Low.** Capability overlap with core; nothing unique.
- **Classification:** `ARCHIVE` — superseded by core; safe to snapshot and remove from active scope.

## 12. Recoveredtreasures_ebay_manager

- **Project name:** Recovered Treasures eBay Manager.
- **Purpose:** Web app shell for eBay-side operations on the Recovered Treasures account. Matrix signals inventory mgmt, file renaming, lot recommendations, file organization.
- **Tech stack:** mixed; ~583 LOC.
- **Status:** Active upstream.
- **Dependencies:** small.
- **APIs used:** none.
- **Active/inactive:** **Active but small/legacy.**
- **Relevance:** **Low.** Functionality subsumed by `ebay assistance` core.
- **Classification:** `ARCHIVE` — superseded.

## 13. RecoveredTreasuresTX.shop-Website-files

- **Project name:** RecoveredTreasuresTX.shop Website Files.
- **Purpose:** Static storefront site files for an external Shopify-style shop.
- **Tech stack:** static site; ~1,230 LOC.
- **Status:** Active.
- **Dependencies:** none (static).
- **APIs used:** none.
- **Active/inactive:** **Active.**
- **Relevance:** **None** for inventory/listing tooling.
- **Classification:** `ARCHIVE` — out of scope.

## 14. ebay_inventory_backup

- **Project name:** eBay Inventory Backup.
- **Purpose:** Data dump (photos / DBs) — no live code.
- **Tech stack:** n/a.
- **Status:** Archived dump.
- **Dependencies:** none.
- **APIs used:** none.
- **Active/inactive:** **Inactive data set.**
- **Relevance:** **Low.** Useful only as reference data; could feed `Inventory_Photos_-_Documents` seed.
- **Classification:** `ARCHIVE` — data-only; preserve with checksum in `archive/data/`.

## 15. Inventory_Photos_-_Documents

- **Project name:** Inventory Photos & Documents.
- **Purpose:** Photo/document dump for inventory records.
- **Tech stack:** n/a; ~334 LOC text/metadata.
- **Status:** Archived dump.
- **Dependencies:** none.
- **APIs used:** none.
- **Active/inactive:** **Inactive data set.**
- **Relevance:** **Low-medium.** Could become a seed/eval set for the AI listing layer.
- **Classification:** `ARCHIVE` — data-only.

## 16. eBay-View-Bot

- **Project name:** eBay View Bot.
- **Purpose:** Automated view-count inflation for eBay listings.
- **Tech stack:** Python; ~782 LOC.
- **Status:** Active upstream.
- **Dependencies:** small Python libs.
- **APIs used:** eBay listing pages (read-only, but with automation).
- **Active/inactive:** **Active but prohibited.**
- **Relevance:** **None** — violates eBay ToS §"Artificial means" / §"Manipulation".
- **Classification:** `ARCHIVE` — `OBSOLETE_VIOLATES_TOS`. Archive first as precedent (Plan Phase E2).

## Summary table

| # | Repo | Project name | Active | Classification | Reason |
|---|---|---|---|---|---|
| 1 | ebay assistance | eBay Assistance / eBay Hero | Active | `KEEP` | canonical target |
| 2 | cardops | CardOps AI | Active | `MERGE` | already partially imported |
| 3 | EBAY_INVENTORY_PHOTO_ORGANIZER | EBay Inventory Photo Organizer | Active | `MERGE` | baseline imported |
| 4 | Stamplicity | Stamplicity v1 | Active (legacy) | `ARCHIVE` | superseded by V2.0 |
| 5 | stamplicity-V2.0 | Stamplicity V2.0 | Active | `MERGE` → `OPTIONAL MODULE` | plugin `image-preprocessor` |
| 6 | COLLECTIBLE_AI_ | COLLECTIBLE_AI_ | UNKNOWN | `UNKNOWN` | license audit + LOC diff needed |
| 7 | ecommerce_autolister | eCommerce Autolister | Active | `MERGE` → `OPTIONAL MODULE` | plugin `lister` candidate |
| 8 | hydrogen | Hydrogen (Shopify) | Active | `ARCHIVE` | out of scope |
| 9 | freedom-fleamarket-biz-production | Freedom Flea Market | Active | `ARCHIVE` | out of scope |
| 10 | Collectease | Collectease | Active | `OPTIONAL MODULE` | capability diff pending |
| 11 | recovered_treasures_app | Recovered Treasures app | Active (small) | `ARCHIVE` | superseded |
| 12 | Recoveredtreasures_ebay_manager | Recovered Treasures eBay Manager | Active (small) | `ARCHIVE` | superseded |
| 13 | RecoveredTreasuresTX.shop-Website-files | RT.TX Shop website | Active | `ARCHIVE` | out of scope |
| 14 | ebay_inventory_backup | eBay Inventory Backup | Inactive data | `ARCHIVE` | data-only |
| 15 | Inventory_Photos_-_Documents | Inventory Photos & Documents | Inactive data | `ARCHIVE` | data-only |
| 16 | eBay-View-Bot | eBay View Bot | Active (prohibited) | `ARCHIVE` | ToS violation |

## Counts

| Classification | Count |
|---|---|
| `KEEP` | 1 |
| `MERGE` | 3 (+ 1 transitioning to OPTIONAL MODULE after plugin wrap) |
| `OPTIONAL MODULE` | 1 (+ 2 transitioning from MERGE) |
| `ARCHIVE` | 9 |
| `UNKNOWN` | 1 |
| **Total** | **16** |

## Phase 2 targets (from directive)

The directive's Phase 2 listed `EBAY_INVENTORY_PHOTO_ORGANIZER` as the import-and-consolidate target. Verdict:
- **`EBAY_INVENTORY_PHOTO_ORGANIZER`** — already imported into `eBayHero.Infrastructure`; Plan Task D4 closes it (`MERGE` complete).
- The plan extends Phase 2 to also fold in `cardops` (Plan Task D5) and convert `stamplicity-V2.0` + `ecommerce_autolister` into optional modules via the plugin runtime (Plan Tasks D1, D6). `Collectease` becomes `OPTIONAL MODULE` pending diff.

## Open questions

1. `COLLECTIBLE_AI_` — license audit required before classification can move out of `UNKNOWN`.
2. `Collectease` — capability diff vs core needs to run; could end up `ARCHIVE` if everything overlaps.
3. `freedom-fleamarket-biz-production` photos — seed dataset for AI listing layer, or strictly out of scope?
4. `Inventory_Photos_-_Documents` — same seed-dataset question.

## Verification commands

```bash
cd "D:/WORK/GitRepos/PERSONAL/Commerce, eBay & Collectibles"
# Per-repo HEAD + remote + LOC
for d in */; do
  ( cd "$d" && \
    printf "%-45s %s\n  %s\n  LOC=%s\n" \
      "${d%/}" \
      "$(git rev-parse --short HEAD 2>/dev/null || echo '-')" \
      "$(git config --get remote.origin.url 2>/dev/null || echo '-')" \
      "$(find . -type f \( -name '*.py' -o -name '*.js' -o -name '*.ts' -o -name '*.cs' \) -not -path './.git/*' -not -path './*/node_modules/*' 2>/dev/null | xargs wc -l 2>/dev/null | tail -1 | awk '{print $1}')" \
  )
done
```