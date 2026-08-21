# eBay Hero Consolidation Implementation Plan

> **For Hermes:** This plan is intentionally phased so a fresh subagent can pick up any single phase with full context. Discovery and documentation precede any destructive action.

**Goal:** Consolidate the 158 eBay-related projects inventoried in `docs/SOURCE-DISCOVERY-REPORT.md` into the single canonical `eBay Hero` platform at `D:\WORK\GitRepos\PERSONAL\Commerce, eBay & Collectibles\ebay assistance\`, with a stable plugin ecosystem, safe archival of superseded siblings, and a roadmap to a production-ready AI-powered listing platform.

**Architecture:** Two-track strategy.
1. **In-place continuation** of the existing canonical repo (`ebay assistance/` → renamed `EBAY HERO/`), which already holds the eBayHero WPF/.NET 8 production baseline, CardOps SQLite importer, Phase 0 artifacts, and Phases 1–10 roadmap at v1.3.0.
2. **Plugin ecosystem** layered on top: a new `eBayHero.Plugins` runtime that loads capability plugins (pricing, OCR, identifier, image-preprocessor, eBay-bridge) discovered via manifest, so each inventoried project can contribute as a plugin instead of being source-imported.

**Tech Stack:** .NET 8 (WPF, ASP.NET, MAUI/iOS, Android), SQLite, Tesseract, OpenCV (via transparent-background), React/Vite (for future web), Python (plugin shims), MSBuild, NuGet, GitHub Actions, eBay REST + Inventory API, MEF/plugin host, PluginManifest.json schema v1.

---

## 0. Current Context and Assumptions

### 0.1 What already exists (verified by inspection 2026-08-12)

| Artifact | Location | State |
|---|---|---|
| Canonical repo | `D:\WORK\GitRepos\PERSONAL\Commerce, eBay & Collectibles\ebay assistance\` | v1.3.0, 5 commits on top of consolidation init |
| Solution | `eBayHero.sln` (8 src projects + tests) | `dotnet test`: 37 passed, 0 warnings, 0 errors |
| Discovery report | `docs/SOURCE-DISCOVERY-REPORT.md` | 158 projects inventoried 2026-07-18 |
| Capability matrix | `docs/FEATURE-CAPABILITY-MATRIX.md`, `SOURCE-CAPABILITY-MATRIX.md` | Exists |
| Import provenance | `docs/IMPORT-PROVENANCE.md` | Exists |
| Repo import audit | `docs/REPO-IMPORT-AUDIT.md` | Exists |
| Architecture ADR | `docs/adr/0001-canonical-architecture.md` | Exists |
| Roadmap | `ROADMAP.md` | Phases 0–10, 0.1→2.0 |
| Phase status | ROADMAP Phase 0/1 done; 2/3/4 in progress; 5–10 blocked | — |

### 0.2 Sibling candidate repos (all under `Commerce, eBay & Collectibles\`)

```
cardops/                                  ← superseded by CardOps importer in core
Collectease/                              ← to evaluate
COLLECTIBLE_AI_/                          ← to evaluate
ebay_inventory_backup/                    ← legacy data, archive candidate
EBAY_INVENTORY_PHOTO_ORGANIZER/           ← already partially imported
eBay-View-Bot/                            ← OBSOLETE — violates ToS, archive only
ecommerce_autolister/                     ← to evaluate
freedom-fleamarket-biz-production/        ← marketplace platform, evaluate
hydrogen/                                 ← Shopify OSS storefront, likely out of scope
Inventory_Photos_-_Documents/             ← data dump, archive candidate
recovered_treasures_app/                  ← superseded
Recoveredtreasures_ebay_manager/          ← superseded
RecoveredTreasuresTX.shop-Website-files/  ← static site archive
Stamplicity/                              ← stamper tool, possibly reusable
stamplicity-V2.0/                         ← newer stamper
```

### 0.3 Renaming directive

The user directive says: rename all references so the canonical repo reads as **eBay Hero** in user-facing docs, while `VERSION.txt` and migration paths retain `eBayHero`/`eBay-Hero` for backward compatibility with existing settings, scripts, and live data paths. The directive says **do not blindly delete code** — analyze, migrate, then archive.

### 0.4 Discovery + documentation first (user's correction)

This plan begins with **Phase A — Discovery & Documentation** that produces machine-checked artifacts before any destructive action. No `rm -rf`, no `git branch -D`, no renaming of source folders until the audit artifacts are committed.

### 0.5 Naming convention (locked)

| Surface | Canonical spelling |
|---|---|
| Product name (UI, README, marketing) | **eBay Hero** |
| Repo folder under PERSONAL | `EBAY HERO` |
| Internal namespaces, projects, .sln, settings keys | `eBayHero.*` (unchanged for migration safety) |
| VERSION tag | `eBay-Hero X.Y.Z` |
| Plugin runtime | `eBayHero.Plugins` |

---

## Phase A — Discovery & Documentation (no destructive changes)

> All tasks in Phase A produce committed documentation. Nothing under `Commerce, eBay & Collectibles\` is moved or deleted during this phase.

### Task A1: Build the consolidated inventory manifest

**Objective:** Produce a single machine-checked inventory of every candidate repo, its status, license, and migration recommendation.

**Files:**
- Create: `docs/CONSOLIDATION-INVENTORY.md`
- Create: `tools/audit/inventory.py` (regeneratable audit script)

**Step 1 — Write the inventory script**
- Walk `D:\WORK\GitRepos\PERSONAL\Commerce, eBay & Collectibles\`.
- For each child repo capture: path, remote, HEAD, last-commit date, primary language, license file present, total LOC (use `pygount` if available), presence of `eBayHero*` references.
- Output both Markdown table and `inventory.json`.

**Step 2 — Run it**
```bash
cd "D:/WORK/GitRepos/PERSONAL/Commerce, eBay & Collectibles/ebay assistance"
python tools/audit/inventory.py --root "D:/WORK/GitRepos/PERSONAL/Commerce, eBay & Collectibles" --out docs/CONSOLIDATION-INVENTORY.md
```
Expected: Markdown table listing all 18 sibling repos with columns noted above.

**Step 3 — Commit**
```bash
git add docs/CONSOLIDATION-INVENTORY.md tools/audit/inventory.py
git commit -m "docs: add consolidatable inventory manifest"
```

### Task A2: Classify each candidate repo

**Objective:** Assign one of six dispositions to every repo and record evidence.

**Files:**
- Modify: `docs/CONSOLIDATION-INVENTORY.md` (add disposition column)
- Create: `docs/CONSOLIDATION-DISPOSITIONS.md`

**Dispositions** (definitions locked in `docs/adr/0002-disposition-policy.md`):

| Code | Meaning | Allowed action |
|---|---|---|
| `IMPORTED` | Code already in core | None — already done |
| `IMPORT_AS_PLUGIN` | Reusable as plugin | Move to `plugins/` |
| `DATA_ONLY` | Photos/DBs, no live code | Move to `archive/data/<name>-<yyyy-mm>/` |
| `OBSOLETE_VIOLATES_TOS` | eBay-View-Bot class | Archive, log warning |
| `SUPERSEDED` | Replaced by core | Archive after migration |
| `OUT_OF_SCOPE` | Hydrogen, unrelated shop site | Document and leave |

**Step 1 — Write disposition ADR**
Create `docs/adr/0002-disposition-policy.md` defining each code and the verification checklist (must show: why deprecated, what was migrated, where the migration lives).

**Step 2 — Per-repo analysis record**
For each repo in the inventory, append a section to `docs/CONSOLIDATION-DISPOSITIONS.md`:
```
### <repo name>
- Path: ...
- LOC: ...
- Languages: ...
- Functionality: ...
- Dependencies: ...
- Overlap with eBayHero core: ...
- Disposition: IMPORT_AS_PLUGIN | SUPERSEDED | ...
- Evidence: <file:line snippets>
- Migration target: <path in core or plugin>
```
- `eBay-View-Bot/` → `OBSOLETE_VIOLATES_TOS` (eBay ToS §"Artificial means" — explicit ban on view bots)
- `EBAY_INVENTORY_PHOTO_ORGANIZER/` → `IMPORTED` (already in `eBayHero.Infrastructure` per IMPORT-PROVENANCE.md)
- `ebay_inventory_backup/`, `Inventory_Photos_-_Documents/` → `DATA_ONLY`
- `recovered_treasures_app/`, `Recoveredtreasures_ebay_manager/` → `SUPERSEDED`
- `hydrogen/`, `RecoveredTreasuresTX.shop-Website-files/` → `OUT_OF_SCOPE`
- `Stamplicity/`, `stamplicity-V2.0/` → evaluate; likely `IMPORT_AS_PLUGIN` (stamper)
- `freedom-fleamarket-biz-production/` → evaluate; likely `OUT_OF_SCOPE` (marketplace platform ≠ inventory tool)
- `COLLECTIBLE_AI_/`, `Collectease/`, `ecommerce_autolister/`, `cardops/` → individual evaluation

**Step 3 — Commit**
```bash
git add docs/CONSOLIDATION-DISPOSITIONS.md docs/adr/0002-disposition-policy.md
git commit -m "docs: classify candidate repos for consolidation"
```

### Task A3: Generate the cross-repo dependency map

**Objective:** Identify shared dependencies that would force import order or block a plugin split.

**Files:**
- Create: `docs/CONSOLIDATION-DEPENDENCY-MAP.md`
- Create: `tools/audit/dep-graph.py`

**Step 1 — Implement the dep graph**
Walk each candidate's `requirements.txt`, `*.csproj`, `package.json`, `Gemfile`, `go.mod` and produce a Mermaid diagram grouped by capability (OCR, image processing, eBay API, pricing, inventory).

**Step 2 — Embed in the doc**
Place the Mermaid block at the top of `CONSOLIDATION-DEPENDENCY-MAP.md` and a per-package table below (capability, package, version constraints, conflicts).

**Step 3 — Commit**
```bash
git add docs/CONSOLIDATION-DEPENDENCY-MAP.md tools/audit/dep-graph.py
git commit -m "docs: dependency map across candidate repos"
```

### Task A4: Document the rename plan

**Objective:** Lock down exactly what gets renamed and what must keep legacy identifiers for migration safety.

**Files:**
- Create: `docs/RENAME-PLAN.md`

**Step 1 — Define rename surface matrix**
Three columns: `Old`, `New`, `Reason`. Cover: repo folder, README header, package metadata, installer, but **not** `eBayHero.sln`, project assembly names, settings keys, SQLite migration table names, OAuth redirect URIs.

**Step 2 — Define the dual-name strategy**
User-visible "eBay Hero", internal "eBayHero" — both correct in their lane. `VERSION.txt` adds a `displayName: eBay Hero` field while keeping `id: eBay-Hero`.

**Step 3 — Commit**
```bash
git add docs/RENAME-PLAN.md
git commit -m "docs: lock the eBay Hero rename plan"
```

### Task A5: Author the plugin ecosystem design doc

**Objective:** Define the contract every future plugin must satisfy so each candidate repo can become a plugin instead of a code import.

**Files:**
- Create: `docs/PLUGIN-ECOSYSTEM.md`
- Create: `docs/adr/0003-plugin-runtime.md`

**Plugin contract (locked in ADR-0003):**
- Manifest: `plugin.json` with `id`, `version`, `capabilities[]`, `entryPoint`, `dependencies[]`, `permissions[]`.
- Capabilities: `pricing`, `ocr`, `identifier`, `image-preprocessor`, `ebay-bridge`, `storage`, `export`, `analytics`.
- Host: `eBayHero.Plugins` (.NET 8) with MEF (`System.Composition`) for in-process discovery and a `PluginLoader` that sandboxes failures.
- Out-of-process fallback: child process JSON-RPC for Python plugins (Stamplicity, COLLECTIBLE_AI_, transparent-background).

**Step 1 — Write the ADR with capability interfaces**
Define `IPricingProvider`, `IOcrEngine`, `IIdentifier`, `IImagePreprocessor`, `IEbayBridge` as `internal` interfaces in `eBayHero.Plugins.Abstractions`.

**Step 2 — Write the ecosystem doc**
Cover: discovery, lifecycle, manifest validation, capability registration, conflict resolution (highest `version`, then deterministic alphabetical `id`), dependency resolution, signing, marketplace distribution.

**Step 3 — Commit**
```bash
git add docs/PLUGIN-ECOSYSTEM.md docs/adr/0003-plugin-runtime.md
git commit -m "docs: design eBay Hero plugin ecosystem"
```

### Task A6: Author the production-readiness roadmap

**Objective:** Consolidate the existing 10-phase roadmap into a single forward-looking milestone document tied to plugin ecosystem work.

**Files:**
- Modify: `ROADMAP.md` (add Plugin track and Production track columns)
- Create: `docs/PRODUCTION-ROADMAP.md`

**Step 1 — Add two tracks to ROADMAP**
Keep existing Phases 0–10. Add:
- **Track P — Plugin ecosystem:** P1 runtime (this plan), P2 first-party plugins, P3 third-party marketplace, P4 capability audit automation.
- **Track R — Production readiness:** R1 eBay Sandbox (Phase 5), R2 release (Phase 6), R3 cross-platform (7/8/9), R4 cloud sync (Phase 10).

**Step 2 — Production roadmap doc**
Restate milestones with explicit acceptance gates: build green, tests green, ≥80% plugin coverage, docs current, provenance recorded.

**Step 3 — Commit**
```bash
git add ROADMAP.md docs/PRODUCTION-ROADMAP.md
git commit -m "docs: add plugin and production tracks to roadmap"
```

---

## Phase B — Rename to eBay Hero (safe, evidence-backed)

> Only after Phase A artifacts are committed. No source code changes; only folder rename and README/header updates.

### Task B1: Rename the canonical folder

**Objective:** Rename `ebay assistance/` → `EBAY HERO/` while keeping the working git repo intact.

**Files:**
- Move folder (filesystem)
- Modify: `README.md` (H1 line and any path references)
- Modify: `VERSION.txt` (add `displayName` field)
- Modify: `docs/IMPORT-PROVENANCE.md` (note rename)

**Step 1 — Verify no other path depends on the folder name**
```bash
cd "D:/WORK/GitRepos/PERSONAL/Commerce, eBay & Collectibles/ebay assistance"
git grep -l "ebay assistance" -- ':!docs/CONSOLIDATION-INVENTORY.md'
```
Expected: empty (the inventory references it intentionally as the source-of-truth record).

**Step 2 — Rename via `git mv`**
On Windows under bash:
```bash
cd "D:/WORK/GitRepos/PERSONAL"
# close any open shells/editors referencing the folder
mv "ebay assistance" "EBAY HERO"
cd "EBAY HERO"
git status  # expect: clean
```

**Step 3 — Update user-facing strings**
Edit `README.md` H1 → `# eBay Hero`. Edit `VERSION.txt` to add `displayName: eBay Hero`.

**Step 4 — Commit and tag**
```bash
git add -A
git commit -m "chore(rename): canonical folder now reads 'EBAY HERO' (product name: eBay Hero)"
git tag v1.3.1-rename
```

### Task B2: Update the solution display name and installer references

**Objective:** Update only the cosmetic project display names and installer strings; leave assembly names and GUIDs untouched for migration safety.

**Files:**
- Modify: `eBayHero.sln` — change only display names, not project type GUIDs
- Modify: `installer/Product.wxs` (or equivalent) — ProductName only

**Step 1 — Diff before edit**
```bash
git diff eBayHero.sln | head -50
```

**Step 2 — Edit display strings**
Update `eBayHero.App.csproj` `<AssemblyTitle>eBay Hero</AssemblyTitle>` and `<Product>eBay Hero</Product>`. Do **not** touch `<RootNamespace>eBayHero.App</RootNamespace>`, `<AssemblyName>eBayHero.App</AssemblyName>`, or project GUIDs.

**Step 3 — Build verification**
```bash
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
```
Expected: PASS, 0 warnings, 0 errors.

**Step 4 — Commit**
```bash
git add eBayHero.sln src/eBayHero.App/eBayHero.App.csproj installer/
git commit -m "chore(rename): cosmetic eBay Hero display strings (assembly identity preserved)"
```

---

## Phase C — Plugin Runtime (foundation for migration)

> Implements the ADR-0003 design from A5. After this phase, the next phases can convert candidate repos into plugins instead of forking code.

### Task C1: Create `eBayHero.Plugins` host project

**Objective:** Stand up the plugin host with manifest discovery and capability registration.

**Files:**
- Create: `src/eBayHero.Plugins/eBayHero.Plugins.csproj`
- Create: `src/eBayHero.Plugins.Abstractions/eBayHero.Plugins.Abstractions.csproj`
- Create: `tests/eBayHero.Plugins.Tests/`
- Modify: `eBayHero.sln`

**Step 1 — Failing test for manifest parsing**
```csharp
[Fact]
public void Manifest_Parse_RejectsMissingId() {
    var json = "{ \"version\": \"1.0.0\" }";
    Assert.Throws<ManifestValidationException>(() => PluginManifest.Parse(json));
}
```

**Step 2 — Implement `PluginManifest`**
- `id`, `version` (semver), `capabilities[]`, `entryPoint` (assembly path or `python:<module>:<entry>`), `dependencies[]` (capability ids), `permissions[]`.
- Validate: id regex `^[a-z][a-z0-9-]*$`, version semver, capabilities in enum, no self-dependency.

**Step 3 — Add to solution and run tests**
```bash
dotnet sln add src/eBayHero.Plugins/eBayHero.Plugins.csproj \
                src/eBayHero.Plugins.Abstractions/eBayHero.Plugins.Abstractions.csproj \
                tests/eBayHero.Plugins.Tests/eBayHero.Plugins.Tests.csproj
dotnet test tests/eBayHero.Plugins.Tests
```
Expected: tests pass.

**Step 4 — Commit**
```bash
git add eBayHero.sln src/eBayHero.Plugins* tests/eBayHero.Plugins.Tests
git commit -m "feat(plugins): host project with manifest validation"
```

### Task C2: Implement capability discovery and MEF composition

**Objective:** First-party .NET plugins can register capabilities via `[Export(typeof(IPricingProvider))]`.

**Files:**
- Modify: `src/eBayHero.Plugins/PluginHost.cs`
- Create: `src/eBayHero.Plugins/PluginCatalog.cs`

**Step 1 — Failing test for capability registration**
```csharp
[Fact]
public async Task Catalog_ResolvesHighestVersionOnConflict() {
    var dir = TestPluginsDir("pricing", ("a", "1.0.0"), ("b", "1.1.0"));
    var pricing = await catalog.ResolveAsync<IPricingProvider>();
    Assert.Equal("b", pricing.PluginId);
}
```

**Step 2 — Implement `PluginCatalog`**
- Scan `<AppData>/eBayHero/plugins/*/plugin.json` and in-tree `plugins/*/`.
- Build MEF `ContainerConfiguration` with `WithAssemblies(paths)`.
- On capability request, resolve all exports, pick by version then id.

**Step 3 — Tests pass; commit**
```bash
dotnet test tests/eBayHero.Plugins.Tests
git add src/eBayHero.Plugins
git commit -m "feat(plugins): MEF catalog with deterministic conflict resolution"
```

### Task C3: Python plugin out-of-process bridge

**Objective:** Python candidates (Stamplicity, COLLECTIBLE_AI_, transparent-background wrappers) load as child processes speaking JSON-RPC.

**Files:**
- Create: `src/eBayHero.Plugins/PythonPluginHost.cs`
- Create: `plugins/python-runtime/plugin_runtime.py` (host shim)

**Step 1 — Failing test for process spawn and call**
```csharp
[Fact]
public async Task PythonPlugin_RoundTripsMethod() {
    using var plugin = await PythonPluginHost.SpawnAsync("echo-plugin");
    var result = await plugin.InvokeAsync<string>("echo", new { msg = "hi" });
    Assert.Equal("hi", result);
}
```

**Step 2 — Implement**
- Spawn `python -m plugin_runtime <manifest>` with stdin/stdout JSON-RPC 2.0.
- Capability registration includes `python:` prefix entries.
- Timeout + cancellation token enforcement (default 30s).

**Step 3 — Commit**
```bash
git add src/eBayHero.Plugins/PythonPluginHost.cs plugins/python-runtime/
git commit -m "feat(plugins): out-of-process Python bridge with JSON-RPC"
```

### Task C4: Plugin sandbox and lifecycle

**Objective:** One plugin crash cannot take down the host.

**Files:**
- Modify: `src/eBayHero.Plugins/PluginHost.cs`
- Create: `tests/eBayHero.Plugins.Tests/SandboxTests.cs`

**Step 1 — Test for crash isolation**
```csharp
[Fact]
public async Task CrashingPlugin_DoesNotAffectSiblings() {
    await catalog.LoadAsync("crashy");
    var pricing = await catalog.ResolveAsync<IPricingProvider>("good");
    Assert.NotNull(pricing);
}
```

**Step 2 — Implement AppDomain + process isolation**
- .NET plugins → separate `AssemblyLoadContext` (collectible).
- Python plugins → child process with stdout reader thread that restarts on disconnect.

**Step 3 — Commit**
```bash
git add src/eBayHero.Plugins/PluginHost.cs tests/eBayHero.Plugins.Tests/SandboxTests.cs
git commit -m "feat(plugins): crash isolation via collectible ALC + child process"
```

### Task C5: Plugin marketplace skeleton

**Objective:** A versioned manifest of first-party plugins the host can install/upgrade.

**Files:**
- Create: `plugins/manifest.json`
- Create: `src/eBayHero.Plugins/MarketplaceClient.cs`
- Create: `tools/plugins/publish.py`

**Step 1 — Failing test for marketplace fetch**
```csharp
[Fact]
public async Task Marketplace_ReturnsSignedManifest() {
    var m = await MarketplaceClient.FetchAsync("https://plugins.ebayhero.local/manifest.json");
    Assert.True(m.HasSignature);
}
```

**Step 2 — Implement**
- Manifest with `signatures` (Ed25519), `artifacts[]` (sha256 + url).
- `MarketplaceClient` verifies signature against pinned public key.
- `tools/plugins/publish.py` produces signed entries for first-party releases.

**Step 3 — Commit**
```bash
git add plugins/manifest.json src/eBayHero.Plugins/MarketplaceClient.cs tools/plugins/publish.py tests/eBayHero.Plugins.Tests/MarketplaceTests.cs
git commit -m "feat(plugins): signed marketplace manifest"
```

---

## Phase D — Migrate Selected Repos as Plugins

> Only `IMPORT_AS_PLUGIN` repos from A2. Each task produces one plugin in `plugins/<id>/`.

### Task D1: Stamplicity → `stamper` plugin

**Objective:** Wrap Stamplicity V2.0 as a plugin providing `image-preprocessor` capability (front/back pairing, corner detection).

**Files:**
- Create: `plugins/stamper/plugin.json`
- Create: `plugins/stamper/src/StamperPlugin/` (.NET wrapper calling V2.0 CLI)
- Create: `plugins/stamper/tests/`
- Create: `docs/migration/STAMPLICITY.md`

**Step 1 — Verify V2.0 builds standalone**
```bash
cd "D:/WORK/GitRepos/PERSONAL/Commerce, eBay & Collectibles/stamplicity-V2.0"
# whatever the V2.0 build script is
./build.sh
```

**Step 2 — Wrapper plugin**
- Manifest `entryPoint`: `StamperPlugin.dll`.
- Wrapper exposes `IImagePreprocessor.PreprocessAsync(imagePath) → ProcessedSet`.
- Reuses V2.0 binaries; no source fork.

**Step 3 — Migration doc**
- Source repo path, LOC migrated, wrapper interface, conflict checks, rollout plan.

**Step 4 — Tests + commit**
```bash
dotnet test plugins/stamper/tests
git add plugins/stamper docs/migration/STAMPLICITY.md
git commit -m "feat(plugin-stamper): migrate Stamplicity V2.0 as image-preprocessor"
```

### Task D2: COLLECTIBLE_AI_ → `collectible-id` plugin

**Objective:** Wrap as `identifier` capability for sports-card / collectible recognition.

**Files:**
- Create: `plugins/collectible-id/plugin.json`
- Create: `plugins/collectible-id/src/CollectibleIdPlugin/`
- Create: `docs/migration/COLLECTIBLE_AI.md`

**Step 1 — Capability contract**
```csharp
public interface IIdentifier {
    Task<IdentifierResult> IdentifyAsync(string imagePath, IdentifyOptions opts, CancellationToken ct);
}
```

**Step 2 — Wrapper + tests**
Wrap the Python module via the C3 bridge; do not modify upstream.

**Step 3 — Commit**
```bash
git add plugins/collectible-id docs/migration/COLLECTIBLE_AI.md
git commit -m "feat(plugin-collectible-id): migrate COLLECTIBLE_AI_ as identifier"
```

### Task D3: transparent-background (Python) → `bg-remover` plugin

**Objective:** Out-of-process Python plugin for `image-preprocessor`.

**Files:**
- Create: `plugins/bg-remover/plugin.json`
- Create: `plugins/bg-remover/src/runtime.py`
- Create: `docs/migration/TRANSPARENT-BACKGROUND.md`

**Step 1 — JSON-RPC entry**
Methods: `remove_background(path, out_path) → {sha256, width, height}`.

**Step 2 — Tests**
Use a sample image; verify alpha channel added.

**Step 3 — Commit**
```bash
git add plugins/bg-remover docs/migration/TRANSPARENT-BACKGROUND.md
git commit -m "feat(plugin-bg-remover): wrap transparent-background as plugin"
```

### Task D4: EBAY_INVENTORY_PHOTO_ORGANIZER → already imported; mark `IMPORTED`

**Objective:** Confirm prior import; document residual evidence; archive upstream.

**Files:**
- Modify: `docs/CONSOLIDATION-DISPOSITIONS.md` (set row to `IMPORTED`)
- Modify: `docs/IMPORT-PROVENANCE.md` (note disposition)

**Step 1 — Verify**
```bash
git grep -l "PhotoOrganizer" src/ tests/
```
Expected: at least one match in `eBayHero.Infrastructure` per existing provenance.

**Step 2 — Commit**
```bash
git add docs/CONSOLIDATION-DISPOSITIONS.md docs/IMPORT-PROVENANCE.md
git commit -m "docs: mark EBAY_INVENTORY_PHOTO_ORGANIZER as IMPORTED"
```

### Task D5: cardops → archive upstream after CardOps importer landed

**Objective:** After verifying the CardOps SQLite importer handles all cardops data shapes, mark upstream archiveable.

**Files:**
- Create: `docs/migration/CARDOPS.md` (test matrix mapping cardops SQLite tables → importer behaviors)
- Create: `tests/eBayHero.Import.Tests/CardOpsCoverageTests.cs`

**Step 1 — Coverage test**
For every table in the cardops schema dump, assert the importer either imports it or records it under `import_skipped` with a reason.

**Step 2 — Tests pass**
```bash
dotnet test tests/eBayHero.Import.Tests/CardOpsCoverageTests
```

**Step 3 — Commit**
```bash
git add docs/migration/CARDOPS.md tests/eBayHero.Import.Tests/CardOpsCoverageTests.cs
git commit -m "docs+tests: cardops coverage matrix verified"
```

### Task D6: ecommerce_autolister → evaluate → likely `IMPORT_AS_PLUGIN` as `lister` capability

**Objective:** If it has eBay listing generation code we don't already have, wrap it as a plugin; otherwise `SUPERSEDED`.

**Files:**
- Modify: `docs/CONSOLIDATION-DISPOSITIONS.md`
- Possibly create: `plugins/lister/` and `docs/migration/ECOMMERCE_AUTOLISTER.md`

**Step 1 — Diff scan**
```bash
diff -rq "ebay assistance/src" "ecommerce_autolister/src" | head -50
```

**Step 2 — Decide and act**
If deltas are eBay-API-specific (taxonomy, shipping, returns), wrap as `lister` plugin. Otherwise document as `SUPERSEDED`.

**Step 3 — Commit**
```bash
git add docs/CONSOLIDATION-DISPOSITIONS.md plugins/lister/ docs/migration/ECOMMERCE_AUTOLISTER.md
git commit -m "feat(plugin-lister): or docs: mark ecommerce_autolister superseded"
```

---

## Phase E — Safe Archival of Superseded Repos

> Only after migration docs are committed. Use `git archive` + filesystem move; never `rm -rf`.

### Task E1: Build the archive script

**Objective:** A repeatable, auditable archive command — not a one-off `rm`.

**Files:**
- Create: `tools/audit/archive-repo.sh`
- Create: `tools/audit/archive-repo.ps1`

**Step 1 — Script behavior**
1. Verify repo has a `CONSOLIDATION-DISPOSITIONS.md` entry with disposition `SUPERSEDED`, `DATA_ONLY`, or `OBSOLETE_VIOLATES_TOS`.
2. Require `--reason`, `--evidence <file:line>`, `--migration-target <path>`.
3. `git archive` (if a git repo) into `archive/source-snapshots/<name>-<sha7>-<yyyy-mm-dd>.tar.gz`.
4. Move files into `archive/<category>/<name>-<yyyy-mm>/` (not delete).
5. Write `archive/<category>/<name>-<yyyy-mm>/MANIFEST.md` with disposition, evidence, target.
6. Append row to `docs/ARCHIVE-LOG.md`.

**Step 2 — Commit**
```bash
git add tools/audit/archive-repo.sh tools/audit/archive-repo.ps1
git commit -m "tools: auditable archive script (never deletes without evidence)"
```

### Task E2: Archive `eBay-View-Bot/`

**Objective:** The most clearly obsolete and ToS-violating repo, archive first as a precedent.

**Files:**
- Create: `archive/obsolete/eBay-View-Bot-2026-08/MANIFEST.md`
- Modify: `docs/ARCHIVE-LOG.md`

**Step 1 — Run**
```bash
bash tools/audit/archive-repo.sh \
  --path "D:/WORK/GitRepos/PERSONAL/Commerce, eBay & Collectibles/eBay-View-Bot" \
  --category obsolete \
  --disposition OBSOLETE_VIOLATES_TOS \
  --reason "Automates view counts in violation of eBay ToS §Artificial Means" \
  --evidence "docs/CONSOLIDATION-DISPOSITIONS.md#eBay-View-Bot" \
  --migration-target "n/a"
```
Expected: archive directory created, snapshot tarball saved, MANIFEST written, log appended.

**Step 2 — Commit**
```bash
git add archive/obsolete/eBay-View-Bot-2026-08 docs/ARCHIVE-LOG.md
git commit -m "archive: eBay-View-Bot (ToS-violating, obsolete)"
```

### Task E3: Archive `recovered_treasures_app/` and `Recoveredtreasures_ebay_manager/`

**Objective:** Two superseded repos, one script invocation each.

**Files:**
- Create: `archive/superseded/recovered_treasures_app-2026-08/MANIFEST.md`
- Create: `archive/superseded/Recoveredtreasures_ebay_manager-2026-08/MANIFEST.md`
- Modify: `docs/ARCHIVE-LOG.md`

**Step 1 — Run script twice** with appropriate evidence per repo.

**Step 2 — Commit**
```bash
git add archive/superseded docs/ARCHIVE-LOG.md
git commit -m "archive: superseded Recovered Treasures apps"
```

### Task E4: Archive `ebay_inventory_backup/` and `Inventory_Photos_-_Documents/`

**Objective:** Data-only dumps.

**Files:**
- Create: `archive/data/ebay_inventory_backup-2026-08/MANIFEST.md`
- Create: `archive/data/Inventory_Photos_-_Documents-2026-08/MANIFEST.md`
- Modify: `docs/ARCHIVE-LOG.md`

**Step 1 — Compute and record SHA-256 of each archive directory; commit manifest.**

**Step 2 — Commit**
```bash
git add archive/data docs/ARCHIVE-LOG.md
git commit -m "archive: data-only inventory dumps (with checksums)"
```

### Task E5: Re-run the inventory and verify

**Objective:** Confirm the consolidated repo still has 37 passing tests and the archive matches the manifest.

**Files:**
- Modify: `docs/CONSOLIDATION-INVENTORY.md` (regenerated)

**Step 1 — Re-run inventory script**
```bash
python tools/audit/inventory.py --root "D:/WORK/GitRepos/PERSONAL/Commerce, eBay & Collectibles" --out docs/CONSOLIDATION-INVENTORY.md
```
Expected: archive rows show `path: archive/...`.

**Step 2 — Test suite**
```bash
dotnet test -c Release
```
Expected: ≥37 tests pass.

**Step 3 — Commit**
```bash
git add docs/CONSOLIDATION-INVENTORY.md
git commit -m "docs: regenerate inventory post-archive"
```

---

## Phase F — Continue Existing Production Roadmap (1–10)

> Plugin-aware continuation. Each phase references the canonical ROADMAP.md and adds the plugin hook.

### Task F1: Phase 1 polish — UI renaming hooks

**Files:**
- Modify: `src/eBayHero.App/Views/AboutView.xaml` → title reads "eBay Hero"
- Modify: `src/eBayHero.App/Resources/Strings.resx`

**Step 1 — Update strings**
Only display strings; preserve all keys (so existing translations don't break).

**Step 2 — Verify**
```bash
dotnet build -c Release --no-restore
dotnet test -c Release --no-build
```

**Step 3 — Commit**
```bash
git add src/eBayHero.App
git commit -m "feat(ui): eBay Hero branding in About and shell"
```

### Task F2: Phase 2 inventory — plug-in image preprocessor

**Files:**
- Modify: `src/eBayHero.App/Services/InventoryScanner.cs`

**Step 1 — Use `IImagePreprocessor` capability** from `eBayHero.Plugins`; resolve via `PluginCatalog`.

**Step 2 — Tests pass**
```bash
dotnet test tests/eBayHero.App.Tests
```

**Step 3 — Commit**
```bash
git add src/eBayHero.App/Services/InventoryScanner.cs
git commit -m "feat(inventory): route preprocessing through plugin host"
```

### Task F3: Phase 3 OCR — plug-in OCR engine

**Files:**
- Modify: `src/eBayHero.Ocr/OcrService.cs`

**Step 1 — Resolve `IOcrEngine`** via `PluginCatalog`; fall back to Tesseract in-process.

**Step 2 — Tests + commit.**

### Task F4: Phase 4 pricing — plug-in pricing provider

**Files:**
- Modify: `src/eBayHero.Core/Services/PricingService.cs`

**Step 1 — Resolve `IPricingProvider`**.

**Step 2 — Tests + commit.**

### Task F5: Phase 5 eBay Sandbox — plug-in eBay bridge

**Files:**
- Modify: `src/eBayHero.Bridge/`

**Step 1 — Resolve `IEbayBridge`**.

**Step 2 — Sandbox tests (gated by `EBAY_SANDBOX_TOKEN` env); commit behind a feature flag.**

### Task F6: Phase 6 release

**Files:**
- Modify: `installer/`, `scripts/release.ps1`

**Step 1 — Use redirected release root (`EA_RELEASE_ROOT` already documented in README).**

**Step 2 — Produce ZIP + checksums + SBOM + manifest; commit `RELEASE-NOTES-1.0.0.md`.**

---

## Phase G — AI-Powered Listing Layer (production-ready target)

> New capability track beyond the existing 10-phase roadmap. Builds on the plugin runtime.

### Task G1: `listing-ai` plugin skeleton

**Objective:** A plugin that consumes `identifier` + `pricing` + `ocr` outputs and produces an eBay listing draft (title, description, category, aspects, images).

**Files:**
- Create: `plugins/listing-ai/plugin.json`
- Create: `plugins/listing-ai/src/` (Python or .NET per capability)

**Step 1 — Capability contract**
```csharp
public interface IListingDrafter {
    Task<ListingDraft> DraftAsync(DraftContext ctx, CancellationToken ct);
}
```

**Step 2 — Test**
Provide fake `identifier`, `pricing`, `ocr` exports; assert draft contains required eBay fields.

**Step 3 — Commit**

### Task G2: Hook into Phase 5 eBay Sandbox for draft validation

**Files:**
- Modify: `src/eBayHero.Bridge/` to validate drafts via `InventoryAPI`.

**Step 1 — Test with sandbox account.**

**Step 2 — Commit.**

### Task G3: Production hardening

- Add retry/backoff, structured logging, secret redaction, telemetry opt-in.
- Pen-test checklist per `docs/SECURITY-ARCHITECTURE.md`.
- Update `PRODUCTION-COMPLETION-PLAN.md` with closure notes.

---

## Files Likely to Change (consolidated)

### New
```
docs/CONSOLIDATION-INVENTORY.md
docs/CONSOLIDATION-DISPOSITIONS.md
docs/CONSOLIDATION-DEPENDENCY-MAP.md
docs/RENAME-PLAN.md
docs/PLUGIN-ECOSYSTEM.md
docs/PRODUCTION-ROADMAP.md
docs/ARCHIVE-LOG.md
docs/adr/0002-disposition-policy.md
docs/adr/0003-plugin-runtime.md
docs/migration/STAMPLICITY.md
docs/migration/COLLECTIBLE_AI.md
docs/migration/TRANSPARENT-BACKGROUND.md
docs/migration/CARDOPS.md
docs/migration/ECOMMERCE_AUTOLISTER.md
src/eBayHero.Plugins/                       (full project)
src/eBayHero.Plugins.Abstractions/          (interfaces)
tests/eBayHero.Plugins.Tests/               (host tests)
plugins/manifest.json
plugins/python-runtime/plugin_runtime.py
plugins/stamper/
plugins/collectible-id/
plugins/bg-remover/
plugins/listing-ai/
tools/audit/inventory.py
tools/audit/dep-graph.py
tools/audit/archive-repo.sh
tools/audit/archive-repo.ps1
tools/plugins/publish.py
archive/obsolete/<name>-<date>/             (created per-archive)
archive/superseded/<name>-<date>/
archive/data/<name>-<date>/
```

### Modified
```
README.md                                   (rename)
VERSION.txt                                 (displayName field)
eBayHero.sln                                (add 2 new projects)
ROADMAP.md                                  (add Plugin + Production tracks)
docs/IMPORT-PROVENANCE.md                   (rename + disposition notes)
src/eBayHero.App/                           (UI strings; plugin consumers)
src/eBayHero.Core/Services/PricingService.cs (plugin consumer)
src/eBayHero.Ocr/OcrService.cs              (plugin consumer)
src/eBayHero.Bridge/                        (plugin consumer; sandbox)
installer/                                  (ProductName)
scripts/release.ps1                         (release automation)
```

### Moved (preserved, not deleted)
```
Commerce, eBay & Collectibles/ebay assistance/  →  Commerce, eBay & Collectibles/EBAY HERO/
Commerce, eBay & Collectibles/<archived repos>  →  archive/<category>/<name>-<date>/
```

---

## Tests / Validation

Per-phase gates:

| Phase | Gate |
|---|---|
| A | Inventory script regenerates manifest deterministically; diff against committed version is empty. |
| B | `dotnet build -c Release` green; 37+ tests green; `git grep "ebay assistance"` only matches historical doc references. |
| C | `dotnet test tests/eBayHero.Plugins.Tests` ≥10 tests; manifest validation rejects bad inputs; sandbox test crashes a plugin and recovers. |
| D | Each migrated plugin has round-trip tests against the upstream repo's sample data; migration doc cites the upstream commit. |
| E | `tools/audit/archive-repo.sh --dry-run` lists intended moves; archive manifest checksum verified; inventory regenerated matches reality. |
| F | Existing roadmap phases update with plugin hooks without breaking the 37-test floor. |
| G | Sandbox listing draft validates via eBay Inventory API; security checklist signed off. |

Continuous checks:
- `dotnet test -c Release` — must remain green throughout.
- `python tools/audit/inventory.py` — must produce stable output (sorted, deterministic).
- `git grep -nE "TODO|FIXME|XXX"` — track but do not block.
- `pre-commit` (if added in C5) — manifest signature, license header, no secrets.

---

## Risks, Tradeoffs, Open Questions

### Risks

1. **Plugin ABI drift** — A Python plugin upgrade breaks the JSON-RPC contract. Mitigation: pin `python-runtime` version per plugin; CI matrix per plugin.
2. **eBay API rate limits** during sandbox integration (Phase F5 / G2). Mitigation: token bucket per session, exponential backoff, dry-run mode.
3. **WPF-only UI blocks Linux** (existing Phase 7 risk). Mitigation: plugin-host-first lets us deliver a Linux shell ahead of full UI parity.
4. **Archive snapshot size** for `Inventory_Photos_-_Documents/` may be large. Mitigation: store snapshot tarball outside the working repo; record only the manifest + checksum.
5. **Dual naming confusion** (eBay Hero vs eBayHero). Mitigation: ADR `docs/RENAME-PLAN.md` is the single source of truth; CI greps for accidental `eBay Hero` in internal namespaces.

### Tradeoffs

- **In-place rename (B1)** vs **fresh repo** — chose in-place to keep the working `.git` history and all 37 tests green. Tradeoff: history shows the old name in early commits.
- **Plugin-first (Phase C) vs code-import** — chose plugin-first to avoid creating a 150k-LOC monolith. Tradeoff: some duplication of glue code per plugin.
- **Out-of-process Python (C3) vs in-process** — chose OOP for crash isolation. Tradeoff: ~50ms IPC overhead per call.

### Open Questions

- Does the user want a public `plugins.ebayhero.local` registry, or a single-user local marketplace only? (Affects C5 scope.)
- Is `freedom-fleamarket-biz-production/` a feature to bring in or genuinely out-of-scope? (Affects D7.)
- Should `recovered_treasures_*` photo libraries become a seed dataset, or stay archived-only? (Affects E3/E4.)
- Sign-in / billing / subscription platform for Phase 10 / Track R? (Out of scope for this plan, call out in PRODUCTION-ROADMAP.md.)

---

## Execution Handoff

Plan complete and saved. Ready to execute using `subagent-driven-development` — a fresh subagent per task with two-stage review (spec compliance then code quality). I will not begin any phase until you approve. Suggested first dispatch: **Task A1** (consolidated inventory manifest) — produces the evidence that gates every later phase.