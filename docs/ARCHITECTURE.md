# Architecture

eBay Hero is a local-first .NET 8 application with a hub-and-spoke plugin ecosystem.
This document describes the modules, the runtime flows, and the boundaries that keep the
system modular.

## Hub and spokes

The **hub** is `eBayHero.Core` plus the infrastructure around it. It owns:

- the domain model and business rules (pricing, lots, listing audit),
- the entitlement controller,
- the plugin registry and host,
- the eBay automation engine (OAuth, REST + Trading routing, field mapping).

The **spokes** are plugins. A plugin never calls back into another plugin; it only receives
a `PluginContext` and responds to hooks and routes. Core never references a plugin project,
so the dependency graph is acyclic and removing a plugin is always safe.

```
Core ──(references)──> nothing plugin-related
Plugins.* ──(references)──> Core
App / Web ──(references)──> Core + Infrastructure + Plugins.*
```

## Projects

| Project | Responsibility |
|---|---|
| `eBayHero.Core` | domain entities, enums, service contracts, entitlements, plugin runtime, eBay engine |
| `eBayHero.Infrastructure` | EF Core SQLite context + migrations, legacy/CardOps importers, secret store, diagnostics |
| `eBayHero.FileSystem` | image root scanning, hashing, safe file operation planning |
| `eBayHero.Ocr` | local OCR, image preprocessing, non-destructive edit operations |
| `eBayHero.Export` | eBay-safe CSV + manifest export |
| `eBayHero.App` | Windows WPF production shell |
| `eBayHero.Web` | Vite/React companion surface |
| `eBayHero.Plugins.CardOps` | trading-card vertical |
| `eBayHero.Plugins.Stamplicity` | philately vertical |
| `eBayHero.Plugins.AiSuite` | master AI unlock addon |

## Entitlements

`IEntitlementService` is the single source of truth for "is this allowed?".

- `FeatureKey` enumerates every gate-able capability.
- `FeatureCatalog` maps each key to a minimum tier and an owning addon.
- `EntitlementEvaluator.Evaluate` is a pure function: `(tier, addons) -> granted features`.
- `EntitlementService` layers resolution order: development unlock → license key → trial →
  configured base tier.
- `HmacLicenseKeyValidator` verifies offline `EH1.<payload>.<signature>` keys.

Business code depends on `IEntitlementService.HasFeature`, never on a concrete tier. This
is what keeps paywalls out of workflows.

## Plugin runtime

- `IEcosystemPlugin` — identity, declared capabilities/hooks/routes, and two async entry points.
- `PluginRegistry` — registration, enabled state, and active resolution (enabled **and**
  addon entitlement granted).
- `PluginHost` — initializes plugins and dispatches hooks/routes with per-plugin isolation;
  a plugin that throws is recorded as failed and the pipeline continues.
- `PluginManifestLoader` — validates `PluginManifest.json` (schema v1) without loading code.

Route calls are entitlement-gated by the host: an inactive plugin's route is not mounted
(404), and a mounted route whose `RequiredFeature` is not granted returns 402.

## eBay engine

- `EbaySettings` — strongly-typed configuration with `FromEnvironment`.
- `EbayOAuthFlow` — pure builders/parsers for the OAuth 2.0 authorization-code flow.
- `HttpEbayTokenClient` — token exchange/refresh over HTTP.
- `EbayListingMapper` — maps inventory + pricing onto eBay Inventory/Offer payloads using
  an `EbayFieldMappingProfile` (template + shipping/return/payment policies + field map).
- `BulkDraftGenerator` — batch draft creation; reports whether bulk publishing is entitled.
- `EbayApiRouter` — REST-first routing with Trading API fallback, exponential backoff, and
  pre-flight entitlement checks.

## Workflows

### Intake
`FileScanner` walks configured roots → `HashService` dedupes → `FileOperationPlanner`
produces a reviewable plan → photos and items are persisted.

### Identify
`ImagePreprocessingService` produces normalized variants → `TesseractOcrService` reads text
→ `CardMetadataAnalyzer` extracts structured fields with evidence. Plugins can subscribe to
`inventory.item.created` to enrich vertical-specific fields.

### Price
`PricingService` uses only verified own sales, authorized sold comparables, and
user-imported sold comparables. Active asking prices are never treated as sold values.
Outliers are excluded and a trimmed average is used.

### Draft → audit → export
`ListingDraftService` builds a `MarketplaceListing`; `ListingAuditService` flags blockers
(e.g. missing front photo, weak pricing); `EbayExportService` writes eBay-safe CSVs. The
`BulkDraftGenerator` and `EbayListingMapper` produce API-ready payloads. Publishing stays
disabled unless explicitly enabled.

### Extend
`PluginHost.DispatchHookAsync("listing.draft.created", payload)` fans out to active plugins,
each enriching the draft within its own capability set.

## Data model

SQLite via EF Core (`InventoryDbContext`). DateTimeOffset values are persisted as UTC ticks
so ordering translates to SQL. Join tables use composite keys. Migrations live in
`eBayHero.Infrastructure/Data/Migrations`.

## Failure isolation

| Failure | Behaviour |
|---|---|
| Plugin throws on a hook | recorded as failed; other plugins still run |
| Plugin throws on a route | 500 with the plugin id; host stays up |
| Plugin missing its addon | plugin inactive; its hooks/routes are not mounted |
| License invalid/expired | silently ignored; tier falls back to base |
| eBay transient error | retried with backoff; Trading fallback when REST is unsupported |
