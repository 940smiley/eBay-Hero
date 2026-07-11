# Feature Capability Matrix

Status values: Complete, Partial, Mock only, Credential required, External approval required, Missing, Deferred with documented reason.

| Feature | Status | Current capability | Required completion |
| --- | --- | --- | --- |
| WPF inventory grid | Complete | Virtualized grid, preview, search/filter, refresh. | Keep responsive with larger datasets. |
| JSON to SQLite migration | Complete | Dry-run/apply service, idempotent tests, live safety. | Continue testing against real 826-photo catalog before live apply. |
| SQLite migrations | Partial | Initial migration exists. | Add production-foundation migration and backup/restore tests. |
| Directory scan | Partial | Indexes images, avoids duplicates by path. | Hash repeated scans, root management UI, missing-drive handling. |
| Source-root management | Missing | SourceRoot table exists. | Add, edit, disable, reconnect, remove index records, re-index. |
| File import planning | Partial | Dry-run plan, duplicate/collision counts. | Execute with journal, rollback, database transaction, undo manifest. |
| OCR Tesseract invocation | Complete | Multiple PSM passes, TSV parsing, candidate scoring. | Use derived images from preprocessing pipeline. |
| OCR preprocessing | Complete | Generates derived orientation/crop/grayscale/threshold/sharpen/denoise images and candidate metadata. | Add richer deskew/perspective algorithms over time. |
| OCR review UI | Partial | OCR Review tab shows candidates, editable text, save, save+learn, and rerun. | Add drawn custom region overlay and side-by-side image compare. |
| OCR correction learning | Complete | Correction map and user-words export. | Add formal sample export metadata. |
| Image editor | Partial | Nondestructive rotate/crop service, derived copies, edit history tab. | Add full drawn crop/pan/zoom/undo-redo UI controls. |
| Front/back/detail grouping | Partial | PhotoItemLink sort/view type exists, export orders front/back, listing drafts preserve order. | Manual/auto split/merge UI, similarity grouping. |
| Card metadata workflow | Partial | Basic fields and metadata extractors exist. | Identification review and metadata confidence/provenance. |
| Pricing workspace | Partial | Price evidence/snapshot tables, calculations, Pricing tab. | Evidence import UI and comparable include/exclude controls. |
| Lot Builder | Partial | Lot tables, lot calculation service, create lot from selection. | Full merge/split/archive/checklist UI. |
| Listings domain | Partial | Marketplace listing tables, draft creation, photos, local export. | Full policies/item specifics editor and eBay adapter calls. |
| Listing audits | Partial | Audit rules persisted and shown in Audits tab. | Add glare/blur/duplicate-image analysis and approved fixes. |
| eBay local export | Complete | Copy-only batch folders and CSV/JSON manifests. | Keep as safe fallback. |
| eBay OAuth connection | Credential required | Auth URL/state/capability service, secure DPAPI secret store, docs. | Callback listener/token exchange once credentials are supplied. |
| eBay Sandbox provider | Mock only | Capability/status service and tests. | HTTP adapter against Sandbox credentials. |
| eBay production publishing | External approval required | Intentionally disabled. | Enable only after credentials, scopes, validation, final confirmation. |
| Secret storage | Partial | DPAPI-protected file store and redaction service. | Optional Windows Credential Manager backend. |
| Background jobs | Partial | Job table exists. | Cancellable job runner, attempts, progress, retry, failure records. |
| Control Center | Complete | WinForms GUI loads manifest, runs/cancels actions, logs output, creates failure bundles. | Add richer progress parsing per script. |
| First-run pipeline | Partial | Safe scripted pipeline with failure bundles. | Add interactive live approval prompt inside GUI. |
| Dependency path config | Complete | `config\paths.json`, configure/verify scripts, process-level cache env. | Optional user-level env opt-in documented. |
| KnowledgeBase discovery | Complete | Read-only discovery and dry-run secret import. | Add curated import mappings as user identifies files. |
| Storage audit | Partial | Dry-run audit, protected boundaries, manifest verification. | Item-level apply/rollback workflow. |
| Installer | Partial | Script installer package. | MSI/MSIX/WiX or maintained installer, shortcuts, upgrades preserve data. |
| Portable release | Complete | Self-contained win-x64 ZIP. | Add checksums and release manifest. |
| CI | Partial | Build/test/release artifact workflows. | Add script parse/analyze, release gates, checksums, installer artifact. |
| Documentation | Partial | Core docs exist. | Add first-run, control center, paths, storage, eBay setup, pricing/lots/listings. |
