# Roadmap

| Phase | Status | Dependencies | Acceptance criteria | Risk | Target | Credentials or approval |
| --- | --- | --- | --- | --- | --- | --- |
| Phase 0 - Source discovery and consolidation | Functional baseline | Local roots under `D:\WORK` | Discovery reports, ADR, import provenance, source import, build, tests | Automatic scoring is noisy | 0.1 | No |
| Phase 1 - Windows production vertical slice | Functional beta | .NET SDK, writable build root | Launch, scan image roots, ingest images, edit metadata, price, build lot/listing draft, audit, export, diagnostics | UI still carries InventoryPhotoOps naming | 0.2 | No |
| Phase 2 - Full inventory and image workflow | In progress | Storage rules, demo data | Root reconnect/remove, duplicate handling, thumbnails, missing-file detection, bulk actions | Large photo libraries need performance testing | 0.3 | No |
| Phase 3 - OCR and identification completion | In progress | Tesseract optional | OCR profiles, preprocessing, confidence, correction provenance, rerun OCR | Tesseract path varies by machine | 0.4 | No |
| Phase 4 - Pricing and lot workflows | In progress | Pricing provider policy | Manual/verified comps, outlier filtering, lot constraints, export manifests | Sold-comparable access may require provider approval | 0.5 | Maybe |
| Phase 5 - eBay Sandbox integration | Not started | eBay developer app | OAuth sandbox connection, policy retrieval, listing import, draft validation | OAuth and API scope errors | 0.6 | Yes |
| Phase 6 - Windows public release | Blocked | Disk space or `EA_RELEASE_ROOT`, installer tech, signing | Portable ZIP, installer, checksums, release manifest, SBOM, verification | C: has too little free space for local release output | 1.0 | Code signing recommended |
| Phase 7 - Linux release | Blocked | Linux UI shell | Buildable Linux UI, SQLite, diagnostics, AppImage or .deb | Current UI is WPF-only | 1.1 | No |
| Phase 8 - iOS beta | Working scaffold only | macOS, Xcode, Apple developer account | Valid iOS project, signing docs, simulator CI | Cannot verify iOS build on Windows | 1.2 | Apple Team ID/signing |
| Phase 9 - Android beta | Working scaffold only | Android signing, shared contracts | Valid Android scaffold, inventory list/detail, diagnostics export | Mobile UX and camera import not implemented | 1.3 | Android signing key for release |
| Phase 10 - Cloud sync and team features | Not started | Cloud backend, billing, privacy review | Multi-user sync, teams, analytics, subscriptions | Recurring infrastructure and support load | 2.0 | Payment/cloud accounts |

