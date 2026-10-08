# Changelog

## 1.4.0 - ecosystem, entitlements, and demo

### Added
- Entitlement controller (`eBayHero.Core/Entitlements`): `FeatureKey` catalog,
  Free/Pro/Business tiers, addon gating, trials, and offline HMAC-signed license keys.
- Plugin runtime (`eBayHero.Core/Plugins`): `IEcosystemPlugin`, `EcosystemPluginBase`,
  `PluginRegistry`, isolating `PluginHost`, and `PluginManifest.json` schema v1 validation.
- First-party plugins: `eBayHero.Plugins.CardOps`, `eBayHero.Plugins.Stamplicity`,
  and `eBayHero.Plugins.AiSuite` (master AI unlock).
- eBay engine (`eBayHero.Core/Ebay`): `EbaySettings`, OAuth 2.0 flow helpers and HTTP
  token client, listing field-mapping profiles, `EbayListingMapper`, `BulkDraftGenerator`,
  and `EbayApiRouter` (REST + Trading fallback, retry/backoff, entitlement gates).
- `eBayHero.Plugins.Tests` (41 tests) plus cross-platform CI job and GitHub Pages demo workflow.
- Interactive GitHub Pages demo in `docs/` with tier/plugin toggles, workflow simulations,
  a getting-started wizard, and a feature matrix.
- Enterprise README plus `docs/ARCHITECTURE.md`, `docs/QUICKSTART.md`,
  `docs/FEATURE-MATRIX.md`, `docs/PLUGIN-DEVELOPMENT.md`, `docs/API-REFERENCE.md`,
  `docs/adr/0001`, `docs/adr/0003`, and `.env.example`.

### Fixed
- Reconstructed the missing `InventoryDbContext` and EF Core migrations so
  `eBayHero.Infrastructure` builds and `MigrateAsync()` creates the schema.
- Renamed the migration-services namespace to `Migrations` to avoid an EF migration
  type/namespace collision.
- Persist `DateTimeOffset` as UTC ticks so SQLite can translate `ORDER BY` to SQL.
- Made `FilenameSanitizer` host-independent (Windows-safe character set).
- Canonicalized path comparison so duplicate-root detection works off-Windows.
- Guarded GDI+ integration tests so non-Windows runs skip instead of crashing.
- Corrected CI/release workflows to reference `csharp/eBayHero.sln` and `csharp/scripts`.

## 0.1.0-consolidation

- Initialized canonical eBay Assistance repository.
- Added source discovery tooling and reports.
- Imported eBayHero .NET 8 production baseline.
- Added ADR and import provenance.
- Added redirected build output support via `EA_BUILD_ROOT`.
- Fixed UI XAML test to work with redirected build outputs.
- Added migration and diagnostic wrapper scripts.


