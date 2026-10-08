# Feature matrix

The authoritative definition of every gate lives in
`csharp/src/eBayHero.Core/Entitlements/FeatureKey.cs` (`FeatureCatalog`). The demo mirrors
it in `docs/demo/data.js`. If those ever disagree, the C# catalog wins.

## Core eBay Hero

| Feature key | Capability | Free | Pro |
|---|---|:--:|:--:|
| `ManualListingGeneration` | Manual listing generation | ✅ | ✅ |
| `CsvImportExport` | CSV import / export | ✅ | ✅ |
| `ManualInventorySync` | Manual inventory sync | ✅ | ✅ |
| `StandardDraftCreation` | Standard draft creation | ✅ | ✅ |
| `SingleAccountConnection` | Single account connection | ✅ | ✅ |
| `ContinuousBackgroundSync` | Continuous background sync | — | ✅ |
| `MultiAccountRouting` | Multi-account routing | — | ✅ |
| `AutoRelisting` | Auto-relisting | — | ✅ |
| `AutomatedRepricing` | Automated repricing rules | — | ✅ |
| `BulkApiPublishing` | Bulk API batch publishing | — | ✅ |

## CardOps plugin (`cardops`)

| Feature key | Capability | Free plugin | Pro addon |
|---|---|:--:|:--:|
| `CardInventorySchema` | Card inventory schema | ✅ | ✅ |
| `ManualCardEntry` | Manual card detail entry | ✅ | ✅ |
| `CardDraftExport` | Card export to eBay drafts | ✅ | ✅ |
| `AiCardRecognition` | AI card recognition / OCR | — | ✅ |
| `CardGradingDetection` | Automated grading detection | — | ✅ |
| `CardCompPricing` | Automated comp pricing | — | ✅ |
| `CardAttributeAutofill` | Automated attribute population | — | ✅ |

## Stamplicity plugin (`stamplicity`)

| Feature key | Capability | Free plugin | Pro addon |
|---|---|:--:|:--:|
| `StampCatalogSchema` | Scott / Stanley Gibbons catalog fields | ✅ | ✅ |
| `ManualStampImageAttach` | Manual image attachment | ✅ | ✅ |
| `StampDraftStaging` | Standard eBay draft staging | ✅ | ✅ |
| `AiPhilatelyVisualId` | AI philately visual identification | — | ✅ |
| `StampValuationComps` | Automated stamp valuation comps | — | ✅ |
| `StampAutoListing` | Stamp auto-listing | — | ✅ |

## All-in-One AI Suite (`ai-suite`)

The AI Suite is a master unlock. While it is active it grants every AI capability across
all installed plugins, regardless of the core tier:

`AiVisionSuite`, `AiCardRecognition`, `CardGradingDetection`, `CardCompPricing`,
`CardAttributeAutofill`, `AiPhilatelyVisualId`, `StampValuationComps`, `StampAutoListing`.

## How gating resolves

1. A feature is considered only if its owning addon is enabled (core features are always
   considered).
2. The feature is granted when the tier meets its minimum (`free` or `pro`).
3. The AI Suite addon then adds all AI features on top.

This is implemented by `EntitlementEvaluator.Evaluate` and covered by
`eBayHero.Plugins.Tests/EntitlementServiceTests.cs`.
