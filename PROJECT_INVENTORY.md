# PROJECT_INVENTORY.md

> **eBay Hero consolidation — repo-by-repo inventory.**
> Generated 2026-08-12 from `D:\WORK\GitRepos\PERSONAL\Commerce, eBay & Collectibles\`.
> Each row records the canonical name, git state, stack, capabilities observed, classification, and recommended disposition.
>
> **Dispositions** (locked in `docs/adr/0002-disposition-policy.md`):
> - `CANONICAL` — this is the consolidation target; do not move or rename without a plan.
> - `IMPORTED` — code already lives in the canonical repo via prior import; no further action.
> - `IMPORT_AS_PLUGIN` — wrap as a plugin under `plugins/<id>/` in Phase D.
> - `DATA_ONLY` — photos / DB dumps; snapshot, checksum, archive.
> - `OBSOLETE_VIOLATES_TOS` — violates eBay ToS or otherwise unsafe; archive with notice.
> - `SUPERSEDED` — functionality is replaced by the canonical repo; archive after migration evidence is recorded.
> - `OUT_OF_SCOPE` — unrelated to eBay inventory/listing; document and leave.
> - `REJECT` — license or quality concerns; do not import.

## Summary

| Category | Count |
|---|---|
| CANONICAL | 1 |
| IMPORTED | 1 |
| IMPORT_AS_PLUGIN (planned) | 4 |
| DATA_ONLY | 2 |
| OBSOLETE_VIOLATES_TOS | 1 |
| SUPERSEDED | 4 |
| OUT_OF_SCOPE | 2 |
| REJECT | 1 |
| **Total** | **16** |

## Inventory table

| # | Repo | Path | Stack | LOC (code) | Git HEAD | Last commit | License | Remote | Capabilities observed | Disposition | Notes |
|---|---|---|---|---:|---|---|---|---|---|---|---|
| 1 | **ebay assistance** (canonical → rename to `EBAY HERO`) | `Commerce, eBay & Collectibles/ebay assistance/` | C# / .NET 8 / WPF / SQLite | n/a (soln count) | `5cc910b` | 2026-07-18 | none | https://github.com/940smiley/ebay-assistance.git | background jobs, CSV import/export, desktop UI, eBay listing generation, front/back image pairing, image ingestion, image root mgmt, installer, inventory mgmt, listing audits, logging/diagnostics, lot recommendations, metadata normalization, OCR, pricing, tests | **CANONICAL** | Production baseline at v1.3.0; 37 tests passing; rename per `docs/RENAME-PLAN.md`. |
| 2 | cardops | `Commerce, eBay & Collectibles/cardops/` | Python, TypeScript (FastAPI, OCR, React, Vite) | 31,878 | `6590e54` | recent | none | https://github.com/940smiley/cardops.git | background jobs, CSV import/export, duplicate detection, eBay listing generation, file organization, file renaming, front/back image pairing, image ingestion, image root mgmt, installer, inventory mgmt, listing audits, logging/diagnostics, lot recommendations, OCR, OpenCV preprocessing, pricing, sports-card identification, tests | **IMPORTED** | CardOps SQLite importer already in `eBayHero.Infrastructure`; archive upstream after coverage test (Task D5). |
| 3 | EBAY_INVENTORY_PHOTO_ORGANIZER | `Commerce, eBay & Collectibles/EBAY_INVENTORY_PHOTO_ORGANIZER/` | Python, TypeScript (OpenCV, React, Vite) | 56,291 | n/a (no HEAD recorded) | — | none | https://github.com/940smiley/EBAY_INVENTORY_PHOTO_ORGANIZER.git | background jobs, duplicate detection, eBay listing generation, file organization, front/back image pairing, image ingestion, inventory mgmt, OCR, OpenCV preprocessing | **IMPORTED** | Per `docs/IMPORT-PROVENANCE.md` — production baseline already in core; mark `IMPORTED` (Task D4). |
| 4 | Stamplicity | `Commerce, eBay & Collectibles/Stamplicity/` | TypeScript / React / Vite (AI Studio app) | 4,494 | `ca17983` | recent | none | https://github.com/940smiley/Stamplicity.git | stamp identification, web UI | **IMPORT_AS_PLUGIN** | Older stamp tool; superseded by V2.0; keep only as reference for V2.0 wrapper. |
| 5 | **stamplicity-V2.0** | `Commerce, eBay & Collectibles/stamplicity-V2.0/` | TypeScript / React / Vite (Next.js-ish) | 12,628 | `b46337b` | recent | none | https://github.com/940smiley/stamplicity-V2.0.git | stamp recognition, cataloging, collection mgmt, appraisal workflows, marketplace listing support, image preprocessing | **IMPORT_AS_PLUGIN** | Wrap as `plugins/stamper/` providing `image-preprocessor` capability (Task D1). |
| 6 | COLLECTIBLE_AI_ | `Commerce, eBay & Collectibles/COLLECTIBLE_AI_/` | TypeScript (Next.js, React, Prisma) | 379,333 (incl. `node_modules`) | n/a | — | none | none | web app shell; pre-existing `findings.md`, `REPO_TODO.md`, prompts/, lib/ | **IMPORT_AS_PLUGIN** | Wrap as `plugins/collectible-id/` (Task D2). Mark `REJECT` per source report was overly conservative — verify license terms before import. |
| 7 | ecommerce_autolister | `Commerce, eBay & Collectibles/ecommerce_autolister/` | Python | 4,306 | `ec59866` | recent | none | https://github.com/940smiley/ecommerce_autolister.git | CSV import/export, desktop UI, eBay listing generation (WIP), front/back image pairing, installer, lot recommendations, metadata normalization | **IMPORT_AS_PLUGIN** | Wrap as `plugins/lister/` capability or mark `SUPERSEDED` after diff scan (Task D6). |
| 8 | hydrogen | `Commerce, eBay & Collectibles/hydrogen/` | TypeScript / React / Vite | 1,705 (likely larger w/ vendor) | `2dd1502` (grafted) | recent | none | https://github.com/940smiley/hydrogen | CSV import/export, eBay listing generation, front/back image pairing, tests | **OUT_OF_SCOPE** | Shopify Hydrogen storefront framework; not eBay inventory tooling. Leave in place. |
| 9 | freedom-fleamarket-biz-production | `Commerce, eBay & Collectibles/freedom-fleamarket-biz-production/` | WordPress / WooCommerce / GCP deployment | 0 (config-only) | `ef70a4e` | recent | none | https://github.com/940smiley/freedom-fleamarket-biz-production.git | marketplace hosting deployment | **OUT_OF_SCOPE** | Multi-vendor marketplace platform, not an inventory tool. Open question: seed dataset? |
| 10 | Collectease | `Commerce, eBay & Collectibles/Collectease/` | TypeScript (React, Material-UI, Vite, GitHub Pages) | 76,038 | `49b1c0d` | recent | none | https://github.com/940smiley/Collectease.git | comparable sales, CSV import/export, eBay listing generation, file organization, lot recommendations, pricing | **SUPERSEDED** | Web collectibles manager; functionality overlaps with core. Defer until capability diff complete. |
| 11 | recovered_treasures_app | `Commerce, eBay & Collectibles/recovered_treasures_app/` | mixed | 592 | `b6e9ee7` | recent | none | https://github.com/940smiley/recovered_treasures_app.git | generic web app shell | **SUPERSEDED** | Archive in Phase E3. |
| 12 | Recoveredtreasures_ebay_manager | `Commerce, eBay & Collectibles/Recoveredtreasures_ebay_manager/` | mixed | 583 | `af9bf5e` | recent | none | https://github.com/940smiley/Recoveredtreasures_ebay_manager.git | generic web app shell | **SUPERSEDED** | Archive in Phase E3. |
| 13 | RecoveredTreasuresTX.shop-Website-files | `Commerce, eBay & Collectibles/RecoveredTreasuresTX.shop-Website-files/` | static site | 1,230 | `193abd0` | recent | none | https://github.com/940smiley/RecoveredTreasuresTX.shop-Website-files.git | static storefront files | **OUT_OF_SCOPE** | Shopify theme files for an external shop; not core tooling. Leave or DATA_ONLY if photos included. |
| 14 | ebay_inventory_backup | `Commerce, eBay & Collectibles/ebay_inventory_backup/` | data dump | 0 | `3afc3dc` | recent | none | https://github.com/940smiley/ebay_inventory_backup.git | (no code, just data) | **DATA_ONLY** | Snapshot + checksum in Phase E4. |
| 15 | Inventory_Photos_-_Documents | `Commerce, eBay & Collectibles/Inventory_Photos_-_Documents/` | data dump | 334 | `64ece3b` | recent | none | https://github.com/940smiley/Inventory_Photos_-_Documents.git | photos + docs | **DATA_ONLY** | Snapshot + checksum in Phase E4. |
| 16 | eBay-View-Bot | `Commerce, eBay & Collectibles/eBay-View-Bot/` | Python | 782 | `f86749d` | recent | none | https://github.com/940smiley/eBay-View-Bot.git | automated view inflation | **OBSOLETE_VIOLATES_TOS** | Violates eBay ToS §"Artificial means"; archive first as precedent in Phase E2. |

## Disposition evidence map

| Repo | Evidence file:line | Migration target |
|---|---|---|
| cardops | `docs/IMPORT-PROVENANCE.md` (CardOps importer section) | `eBayHero.Infrastructure` (already imported) |
| EBAY_INVENTORY_PHOTO_ORGANIZER | `docs/IMPORT-PROVENANCE.md` (PhotoOps baseline) | `eBayHero.Infrastructure` (already imported) |
| Stamplicity | older stamp tool, V2.0 supersedes | reference only |
| stamplicity-V2.0 | `docs/migration/STAMPLICITY.md` (to be written, Task D1) | `plugins/stamper/` |
| COLLECTIBLE_AI_ | `docs/migration/COLLECTIBLE_AI.md` (to be written, Task D2) | `plugins/collectible-id/` |
| ecommerce_autolister | `docs/migration/ECOMMERCE_AUTOLISTER.md` (to be written, Task D6) | `plugins/lister/` or `archive/superseded/` |
| eBay-View-Bot | eBay ToS §Artificial Means | `archive/obsolete/eBay-View-Bot-2026-08/` |
| recovered_treasures_app | `docs/migration/CARDOPS.md` capability diff | `archive/superseded/recovered_treasures_app-2026-08/` |
| Recoveredtreasures_ebay_manager | capability diff (no unique functionality) | `archive/superseded/Recoveredtreasures_ebay_manager-2026-08/` |
| ebay_inventory_backup | photos + DB dump, no live code | `archive/data/ebay_inventory_backup-2026-08/` |
| Inventory_Photos_-_Documents | photos + docs dump | `archive/data/Inventory_Photos_-_Documents-2026-08/` |
| Collectease | capability diff pending (web app overlaps core) | TBD |
| hydrogen | Shopify Hydrogen storefront framework | none (out of scope) |
| freedom-fleamarket-biz-production | WooCommerce deployment | none (out of scope) |
| RecoveredTreasuresTX.shop-Website-files | static storefront files | none (out of scope) |

## Open questions requiring user decision

1. **Collectease** — capability diff not yet run; is it `SUPERSEDED` or `IMPORT_AS_PLUGIN`?
2. **COLLECTIBLE_AI_** — license terms in `node_modules` were never audited; the source-discovery report marks it `REJECT` on license grounds. Do you want me to (a) audit the licenses, (b) keep it as `OUT_OF_SCOPE`, or (c) proceed to wrap as a plugin only on the source code (not vendored deps)?
3. **freedom-fleamarket-biz-production** — should the photo inventory become a seed dataset for the AI listing layer, or remain purely out of scope?
4. **RecoveredTreasuresTX.shop-Website-files** — DATA_ONLY archive, or leave in place?

## Verification commands

To regenerate the inventory from a fresh checkout:
```bash
cd "D:/WORK/GitRepos/PERSONAL/Commerce, eBay & Collectibles"
for d in */; do
  name="${d%/}"
  ( cd "$d" && \
    printf "%s,%s,%s,%s\n" \
      "$name" \
      "$(git rev-parse --short HEAD 2>/dev/null || echo '-')" \
      "$(git config --get remote.origin.url 2>/dev/null || echo '-')" \
      "$(find . -type f \( -name '*.py' -o -name '*.js' -o -name '*.ts' -o -name '*.cs' -o -name '*.swift' -o -name '*.kt' -o -name '*.go' \) -not -path './.git/*' -not -path './*/node_modules/*' 2>/dev/null | xargs wc -l 2>/dev/null | tail -1 | awk '{print $1}')" \
  )
done
```

To cross-check totals: `python tools/audit/inventory.py` (to be added in Task A1) will produce the equivalent machine-checked artifact.

---

*Next: Task A2 — write `docs/CONSOLIDATION-DISPOSITIONS.md` with per-repo evidence and link from this table.*