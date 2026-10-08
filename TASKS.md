# Tasks

## Completed

- Polyglot monorepo restructure and root launchers created.
- Documentation updated with beginner instructions and launch/scaling plans.
- Baseline repository commit.
- Full source discovery across `D:\WORK\GitRepos` and `D:\WORK\Projects`.
- Architecture decision recorded.
- eBayHero production baseline imported.
- Release build verified; cross-platform test suite verified.
- Repaired the missing `InventoryDbContext` + EF Core migrations (Infrastructure now builds).
- Freemium entitlement controller (`FeatureKey`, tiers, addons, license keys, trials).
- Modular plugin runtime (`IEcosystemPlugin`, registry, isolating host, manifest schema v1).
- First-party plugins: CardOps, Stamplicity, All-in-One AI Suite.
- eBay automation engine: OAuth 2.0 flow, listing field mapping, bulk drafts, REST/Trading router.
- Plugin test suite (41 tests) plus cross-platform CI and GitHub Pages demo workflow.
- Interactive GitHub Pages demo in `docs/` (tiers, plugins, simulations, onboarding wizard).
- Enterprise README and developer docs (architecture, quickstart, plugin guide, API reference, ADRs).

## Next

- Wire the entitlement service and plugin host into the WPF shell and web companion.
- Replace plugin AI stubs with real OCR/vision/valuation providers behind the same routes.
- Persist plugin enablement and license state in the local database.
- Complete eBay Sandbox OAuth acceptance and add live-publish guardrails.
- Add installer technology beyond PowerShell ZIP packaging.
- Add Linux UI shell plan and implementation.
- Add iOS/Android scaffold projects when macOS/mobile signing inputs are available.
