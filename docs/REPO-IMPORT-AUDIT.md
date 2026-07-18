# Repository Import Audit

Date: 2026-07-01

Source roots scanned:

- `D:\WORK\GitRepos\PERSONAL`
- `D:\WORK\GitRepos\CLONED`

## Candidates Reviewed

| Repository | Status | Useful material found | Import decision |
| --- | --- | --- | --- |
| `cardops` | Clean Git repo, branch `codex/planned-updates` | Sports-card OCR field heuristics, eBay title/recommendation logic, filename front/back pairing, job/root safety patterns | Imported native C# equivalents for card metadata analysis, listing recommendations, and front/back pairing. |
| `image-pro-cacaws_copy` | Clean Git repo, branch `main` | Image crop/rotate/enhance utilities, object crop detection, filename/export utilities | Imported native C# object-crop detection into OCR preprocessing. Existing filename/export services were already stronger in this app. |
| `EBAY_INVENTORY_PHOTO_ORGANIZER` | Dirty Git repo, detached `HEAD` | Python crop/rotation/dedup/background-removal modules and older UI experiments | Reference only. The repo includes generated environments and dirty worktree changes, so no direct source copy was made. |
| `ecommerce_autolister` | Dirty Git repo | Browser automation and scraper scripts | Skipped. Current app should use official eBay APIs/export workflows, not marketplace scraping or UI automation. |
| `Recoveredtreasures_ebay_manager` | Dirty Git repo | Early dashboard, draft, pricing, and settings concepts | Reference only. Current app already has native pricing/listing/eBay connection domain models. |
| `Collectease` | Clean Git repo | React collection UI ideas | Skipped for code import. It is a separate frontend stack with little direct .NET/WPF reuse. |
| `ebay_inventory_backup`, `COLLECTIBLE_AI_`, `Inventory_Photos_-_Documents`, `image_augmentor` | Mixed clean/dirty states | Backup/docs/older image and collectible experiments | No direct import after focused scan; no stronger reusable implementation than the selected sources was found. |
| `D:\WORK\GitRepos\CLONED` filtered matches | Read-only scan | Only `easylist-*` matched the filter | Skipped as unrelated. |

## Imported Into eBay Hero

- `CardMetadataAnalyzer` in `eBayHero.Core`:
  - Extracts trading-card fields from OCR text and filename fallback text.
  - Produces field-level evidence and confidence.
  - Builds marketplace-length card titles.
  - Produces deterministic listing recommendations without claiming sold-comparable access.

- `PhotoPairingAnalyzer` in `eBayHero.Core`:
  - Infers front/back/detail roles from filenames.
  - Proposes front/back pairs using filename base keys.

- OCR object crop detection in `eBayHero.Ocr`:
  - Adds connected-component style object crops alongside full-image, card-boundary, and custom crop variants.
  - Writes only derived OCR working images under the configured OCR temp root.
  - Does not overwrite originals.

- WPF inventory grid layout hardening:
  - Larger header and row minimum heights.
  - Explicit vertical and horizontal scrollbars.
  - Cell/row content centered to reduce clipped row text.

## Explicitly Excluded

- `cardops\data\ebay-oauth-token.json` and any token/credential material.
- Scraping/browser automation scripts from autolister repos.
- Electron, React, FastAPI, Supabase, or generated environment dependencies.
- Dirty worktree modifications in source repos.

## Follow-up Opportunities

- Add a review UI surface for `CardMetadataAnalysis.Evidence` so users can accept/reject individual OCR-derived metadata fields.
- Expand object crop detection with a real CV edge detector if OCR still struggles on cards with glare or dark backgrounds.
- Add a batch grouping command that applies `PhotoPairingAnalyzer.ProposeFrontBackPairs` to selected or newly imported folders.

