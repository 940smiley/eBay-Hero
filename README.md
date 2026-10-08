# eBay Hero

> Local-first, AI-assisted eBay listing and inventory platform with a modular plugin ecosystem.

[![CI](https://github.com/940smiley/eBay-Hero/actions/workflows/ci.yml/badge.svg)](https://github.com/940smiley/eBay-Hero/actions/workflows/ci.yml)
[![Interactive demo](https://img.shields.io/badge/demo-GitHub%20Pages-5b8cff)](https://940smiley.github.io/eBay-Hero/)
[![Version](https://img.shields.io/badge/version-1.4.0-22d3a7)](VERSION.txt)

eBay Hero ingests photos, identifies items (trading cards, stamps, collectibles), reads
text with OCR, prices from authorized sold comparables, builds listing drafts, validates
them against eBay (sandbox by default), and exports eBay-safe CSVs — all against a local
SQLite database.

It is also an **ecosystem hub**: vertical capabilities ship as isolated plugins (CardOps
for trading cards, Stamplicity for philately) that mount onto the core through a small,
entitlement-aware interface. Disabling or removing a plugin can never break the core.

---

## Table of contents

- [Project overview](#project-overview)
- [Architecture](#architecture)
- [Feature matrix](#feature-matrix)
- [Quickstart](#quickstart)
- [Configuration](#configuration)
- [Repository layout](#repository-layout)
- [Plugin development](#plugin-development)
- [API & workflow reference](#api--workflow-reference)
- [Interactive demo](#interactive-demo)
- [Testing & CI](#testing--ci)
- [Safety defaults](#safety-defaults)
- [Documentation index](#documentation-index)

---

## Project overview

eBay Hero solves the whole "one item at a time" problem for high-volume resellers:

1. **Intake** — scan one or more photo roots, hash, deduplicate, and index images.
2. **Identify** — OCR labels, certs, and cards; extract structured metadata.
3. **Price** — compute market value from verified and authorized sold evidence only.
4. **Draft** — map inventory onto eBay Inventory/Offer payloads with reusable templates.
5. **Validate** — audit drafts against listing rules before anything is published.
6. **Export / publish** — produce eBay-safe CSVs, or publish via the REST API (opt-in).
7. **Extend** — plug in vertical addons without touching core code.

The product is **local-first**: inventory, images, and the database stay on your machine.
Live eBay publishing is disabled by default and must be explicitly enabled per account.

---

## Architecture

eBay Hero uses a **hub-and-spoke** model. The hub owns the eBay engine, persistence, and
the entitlement controller. Spokes are plugins that declare capabilities, hooks, and
routes — and are executed by an isolating host.

```
+---------------------------- eBay Hero (hub) -----------------------------+
|  eBayHero.Core          domain, entitlements, plugin runtime, eBay engine |
|  eBayHero.Infrastructure EF Core + SQLite, migrations, legacy importers   |
|  eBayHero.FileSystem    image scanning, hashing, safe file operations     |
|  eBayHero.Ocr           local OCR, preprocessing, non-destructive edits   |
|  eBayHero.Export        eBay-safe CSV/manifest export                     |
|  eBayHero.App (WPF)     Windows production shell                          |
|  eBayHero.Web (Vite)    companion surface                                 |
|                                                                           |
|  [ Entitlements ]   [ Plugin registry ]   [ Plugin host (isolated) ]      |
|  [ eBay engine: OAuth 2.0 / Inventory / Fulfillment / Trading ]           |
+------------^----------------------------^----------------------------^----+
             | IEcosystemPlugin           |                            |
   +---------+---------+   +--------------+--------+   +--------+------+
   | Plugins.CardOps   |   | Plugins.Stamplicity   |   | Plugins.AiSuite|
   | cards + AI        |   | stamps + AI           |   | master AI unlock|
   +-------------------+   +-----------------------+   +----------------+
```

**Design rules**

- Core never references a plugin project. Plugins reference Core.
- All gating flows through `IEntitlementService`; business logic never checks a tier.
- The plugin host wraps every plugin call, so a failing addon is isolated, not fatal.
- eBay field names live in mapping profiles, not in the domain model.

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) and
[docs/adr/0003-plugin-runtime.md](docs/adr/0003-plugin-runtime.md).

---

## Feature matrix

Legend: ✅ included · 🔒 requires the tier/addon shown · — not applicable.

| Capability | Owner | Free | Pro | Plugin addon |
|---|---|:--:|:--:|:--:|
| Manual listing generation | Core | ✅ | ✅ | — |
| CSV import / export | Core | ✅ | ✅ | — |
| Manual inventory sync | Core | ✅ | ✅ | — |
| Standard draft creation | Core | ✅ | ✅ | — |
| Single account connection | Core | ✅ | ✅ | — |
| Continuous background sync | Core | — | 🔒 Pro | — |
| Multi-account routing | Core | — | 🔒 Pro | — |
| Auto-relisting | Core | — | 🔒 Pro | — |
| Automated repricing rules | Core | — | 🔒 Pro | — |
| Bulk API batch publishing | Core | — | 🔒 Pro | — |
| Card inventory schema | CardOps | ✅ | ✅ | ✅ CardOps |
| Manual card detail entry | CardOps | ✅ | ✅ | ✅ CardOps |
| Card export to eBay drafts | CardOps | ✅ | ✅ | ✅ CardOps |
| AI card recognition / OCR | CardOps | — | 🔒 Pro | 🔒 CardOps Pro |
| Automated grading detection | CardOps | — | 🔒 Pro | 🔒 CardOps Pro |
| Automated comp pricing | CardOps | — | 🔒 Pro | 🔒 CardOps Pro |
| Automated attribute population | CardOps | — | 🔒 Pro | 🔒 CardOps Pro |
| Stamp catalog schema (Scott / SG) | Stamplicity | ✅ | ✅ | ✅ Stamplicity |
| Manual image attachment | Stamplicity | ✅ | ✅ | ✅ Stamplicity |
| Standard eBay draft staging | Stamplicity | ✅ | ✅ | ✅ Stamplicity |
| AI philately visual identification | Stamplicity | — | 🔒 Pro | 🔒 Stamplicity Pro |
| Automated stamp valuation comps | Stamplicity | — | 🔒 Pro | 🔒 Stamplicity Pro |
| Stamp auto-listing | Stamplicity | — | 🔒 Pro | 🔒 Stamplicity Pro |
| AI vision / OCR / valuation everywhere | AI Suite | — | — | 🔒 All-in-One AI Suite |

The authoritative definition of every gate lives in code:
`csharp/src/eBayHero.Core/Entitlements/FeatureKey.cs` (`FeatureCatalog`). The demo mirrors
it in `docs/demo/data.js`, and [docs/FEATURE-MATRIX.md](docs/FEATURE-MATRIX.md) expands it.

### Plans

| Plan | Price (suggested) | Highlights |
|---|---|---|
| Free / local | $0 | manual listing, image intake, basic OCR, drafts, CSV export, one account, one plugin at a time |
| Pro | $19–$39 / mo | background sync, multi-account routing, auto-relisting, repricing, bulk API publishing, plugin AI |
| Business | $79–$199 / mo | multi-store, multi-user, cloud sync, scheduled automation, higher provider limits |
| Addons | per plugin | CardOps Pro, Stamplicity Pro, All-in-One AI Suite (unlocks AI everywhere) |

See [MONETIZATION_PLAN.md](MONETIZATION_PLAN.md).

---

## Quickstart

### Prerequisites

- .NET SDK 8.0+
- Node.js 20+ (web companion and demo only)
- Optional: Tesseract OCR, an eBay developer keyset

### Build & test (any OS — non-WPF projects)

```bash
cd csharp
dotnet restore eBayHero.sln
dotnet build eBayHero.sln -c Release --no-restore
dotnet test  eBayHero.sln -c Release --no-build
```

The WPF app (`eBayHero.App`) and its UI tests target `net8.0-windows` and only build on
Windows. On Linux/macOS, build the cross-platform projects:

```bash
dotnet build src/eBayHero.Core/eBayHero.Core.csproj -c Release
dotnet test  tests/eBayHero.Plugins.Tests/eBayHero.Plugins.Tests.csproj -c Release
```

### Low-disk machines

Redirect build output and the NuGet cache (recommended when `C:` is tight):

```powershell
$env:EA_BUILD_ROOT  = 'D:\WORK\BuildArtifacts\ebay-hero'
$env:NUGET_PACKAGES = 'D:\WORK\.nuget-packages'
dotnet restore .\csharp\eBayHero.sln --packages $env:NUGET_PACKAGES
```

### Run the demo locally

The interactive demo is static — open `docs/index.html`, or serve it:

```bash
npx serve docs          # then open the printed URL
```

### Windows launchers

| Action | Double-click |
|---|---|
| Launch the app | `Run-Demo.cmd` |
| Run tests | `Run-Tests.cmd` |
| Build | `Build-Project.cmd` |
| Publish | `Deploy-Windows.cmd` |
| Control center | `Open-ControlCenter.cmd` |

Full walkthrough: [docs/QUICKSTART.md](docs/QUICKSTART.md).


---

## Configuration

Copy [`.env.example`](.env.example) and fill in your eBay developer values. eBay Hero reads
these from the environment (or from the in-app settings panel, which stores secrets through
the platform secret store).

| Variable | Purpose | Example |
|---|---|---|
| `EBAY_ENVIRONMENT` | `sandbox` or `production` | `sandbox` |
| `EBAY_MARKETPLACE_ID` | eBay marketplace | `EBAY_US` |
| `EBAY_CLIENT_ID` | App ID | `MyApp-xxxx-...` |
| `EBAY_CLIENT_SECRET` | Cert ID | `SBX-xxxx...` |
| `EBAY_REFRESH_TOKEN` | Long-lived user token | `v^1.1#...` |
| `EBAY_RUNAME` | Redirect name (redirect_uri) | `My_Name-MyApp-SBX-abc` |
| `EBAY_REDIRECT_URI` | Local loopback callback | `http://127.0.0.1:49152/callback` |
| `EH_LICENSE_KEY` | Optional signed license key | `EH1....` |
| `EH_LICENSE_SECRET` | Secret used to verify license keys | *(server-side only)* |
| `EH_ADDONS` | Comma-separated enabled addons | `cardops,stamplicity` |

> Never commit `.env`, refresh tokens, or license secrets. They are gitignored.

---

## Repository layout

```
csharp/
  src/eBayHero.Core            domain, entitlements, plugins, eBay engine
  src/eBayHero.Infrastructure  EF Core + SQLite, migrations, importers
  src/eBayHero.FileSystem      scanning, hashing, file ops
  src/eBayHero.Ocr             OCR + image preprocessing
  src/eBayHero.Export          eBay-safe export
  src/eBayHero.App             Windows WPF shell
  src/eBayHero.Web             Vite companion surface
  src/eBayHero.Plugins.CardOps       first-party plugin
  src/eBayHero.Plugins.Stamplicity   first-party plugin
  src/eBayHero.Plugins.AiSuite       first-party plugin
  tests/                       xUnit test projects (Core, Infrastructure, Integration, Plugins, UI)
docs/                          documentation + GitHub Pages demo
  demo/                        demo assets (data.js, app.js, styles.css)
```

Other language folders (`python/`, `nodejs/`, `java/`, `go/`) are reserved build-outs.

---

## Plugin development

A plugin implements one small interface:

```csharp
public interface IEcosystemPlugin
{
    string Id { get; }
    string Name { get; }
    string Version { get; }
    PluginTier Tier { get; }
    IReadOnlyList<PluginCapability> Capabilities { get; }
    IReadOnlyList<PluginHook> Hooks { get; }
    IReadOnlyList<PluginRoute> Routes { get; }
    ValueTask InitializeAsync(PluginContext context, CancellationToken cancellationToken = default);
    ValueTask<PluginHookResult> OnHookAsync(PluginHookContext context, CancellationToken cancellationToken = default);
    ValueTask<PluginRouteResult> InvokeRouteAsync(PluginRouteContext context, CancellationToken cancellationToken = default);
}
```

Derive from `EcosystemPluginBase`, declare capabilities with an optional `FeatureKey`, and
register with the host:

```csharp
var registry = new PluginRegistry(entitlements);
registry.Register(new CardOpsPlugin());
var host = new PluginHost(registry, entitlements);
await host.InitializeAsync();
var outcomes = await host.DispatchHookAsync("inventory.item.created", payload);
```

Rules of the road:

- Declare a `FeatureKey` on any premium capability; never hardcode a paywall.
- Return `PluginHookResult.Ignored` for hooks you don't handle.
- Treat the manifest (`PluginManifest.json`, schema v1) as the discovery contract.

Full guide: [docs/PLUGIN-DEVELOPMENT.md](docs/PLUGIN-DEVELOPMENT.md).

---

## API & workflow reference

- Supported eBay endpoints, scopes, and REST/Trading routing rules:
  [docs/API-REFERENCE.md](docs/API-REFERENCE.md)
- Configuration parameters and field mapping:
  [docs/API-REFERENCE.md](docs/API-REFERENCE.md#configuration-parameters)
- Core workflows (intake → identify → price → draft → audit → export):
  [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md#workflows)

---

## Interactive demo

A dependency-free demo of the entitlement + plugin model lives in [`docs/`](docs/index.html)
and deploys to GitHub Pages:

**https://940smiley.github.io/eBay-Hero/**

It includes mock tier toggles, plugin toggles, an eBay connection test, bulk draft
creation, card OCR and stamp cataloging previews, and a getting-started wizard.


---

## Testing & CI

```bash
cd csharp
dotnet test tests/eBayHero.Core.Tests/eBayHero.Core.Tests.csproj -c Release
dotnet test tests/eBayHero.Infrastructure.Tests/eBayHero.Infrastructure.Tests.csproj -c Release
dotnet test tests/eBayHero.IntegrationTests/eBayHero.IntegrationTests.csproj -c Release
dotnet test tests/eBayHero.Plugins.Tests/eBayHero.Plugins.Tests.csproj -c Release
```

| Suite | Covers |
|---|---|
| Core.Tests | domain services, pricing, listing audit |
| Infrastructure.Tests | SQLite migrations, legacy + CardOps importers, DateTimeOffset queries |
| IntegrationTests | export and image-processing end-to-end |
| Plugins.Tests | entitlements, plugin registry, host isolation, manifests, eBay engine |

CI runs on Windows (full solution) and Linux (cross-platform projects); see
[.github/workflows/ci.yml](.github/workflows/ci.yml).

---

## Safety defaults

- Live eBay publishing is **disabled by default**; drafts are validated in sandbox first.
- CardOps runtime data, `.ENV`, OAuth tokens, logs, thumbnails, and local SQLite data are
  never committed.
- Build outputs are gitignored and can be redirected with `EA_BUILD_ROOT`.
- Secrets are redacted in diagnostics bundles.

See [SECURITY.md](SECURITY.md).

---

## Documentation index

| Document | Contents |
|---|---|
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | system design, workflows, module responsibilities |
| [docs/QUICKSTART.md](docs/QUICKSTART.md) | install, configure, first publish |
| [docs/FEATURE-MATRIX.md](docs/FEATURE-MATRIX.md) | full Free/Pro/addon comparison |
| [docs/PLUGIN-DEVELOPMENT.md](docs/PLUGIN-DEVELOPMENT.md) | build an addon end to end |
| [docs/API-REFERENCE.md](docs/API-REFERENCE.md) | eBay endpoints, scopes, config, mapping |
| [docs/adr/0001-canonical-architecture.md](docs/adr/0001-canonical-architecture.md) | canonical architecture decision |
| [docs/adr/0003-plugin-runtime.md](docs/adr/0003-plugin-runtime.md) | plugin runtime decision |
| [ROADMAP.md](ROADMAP.md) | phased delivery plan |
| [MONETIZATION_PLAN.md](MONETIZATION_PLAN.md) | pricing model |
| [CHANGELOG.md](CHANGELOG.md) | release history |

---

## License

See [LICENSE](LICENSE).

