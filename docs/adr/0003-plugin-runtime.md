# ADR 0003 — Plugin runtime

- **Status:** Accepted
- **Date:** 2026-10-08
- **Relates to:** ADR 0001

## Context

Vertical addons (CardOps, Stamplicity) and third-party plugins need to extend eBay Hero
without core depending on them, and without a misbehaving addon taking the app down.
Premium capabilities must be gated without embedding paywall checks in business code.

## Decision

Adopt an **in-process, declarative plugin runtime** in `eBayHero.Core/Plugins`:

1. **Contract** — `IEcosystemPlugin` exposes identity (`Id`, `Name`, `Version`, `Tier`),
   declared extension points (`Capabilities`, `Hooks`, `Routes`), and two async entry
   points (`InitializeAsync`, `OnHookAsync`, `InvokeRouteAsync`).
2. **Base class** — `EcosystemPluginBase` provides safe defaults, entitlement helpers, and
   payload parsing so addons stay small.
3. **Registry** — `PluginRegistry` tracks registration and enabled state, and resolves
   *active* plugins as `enabled && (Tier == Core || addon entitled)`.
4. **Host** — `PluginHost` initializes plugins and dispatches hooks/routes with per-plugin
   `try/catch`, reporting failures through an `onPluginError` callback. A failing plugin
   never aborts a workflow.
5. **Manifest** — `PluginManifest.json` (schema v1) is validated by `PluginManifestLoader`
   without loading code, so discovery is safe.
6. **Entitlement integration** — capabilities and routes declare an optional `FeatureKey`.
   The host returns 402 for an unentitled route and does not mount routes of inactive plugins.

## Consequences

**Positive**

- Crash isolation per plugin.
- Addon removal is a one-line registry change.
- Entitlement rules live in one place and are unit-tested.
- Manifests enable a future out-of-process or marketplace runtime.

**Negative**

- In-process plugins share the host's memory; a hostile plugin could still consume
  resources. Mitigated by the manifest allowlist and a future out-of-process option.
- Hooks are best-effort: a failing plugin silently drops out of a fan-out (logged).

## Alternatives considered

- **MEF / DI-discovered plugins** — more ceremony, weaker isolation guarantees, and no
  declarative manifest. Rejected.
- **Out-of-process plugins over JSON-RPC** — strongest isolation, but ~50ms IPC per call
  and significant packaging overhead. Deferred; the interface is designed to allow it.
- **Direct source integration per vertical** — creates a monolith and couples releases.
  Rejected.

## Compliance

- `tests/eBayHero.Plugins.Tests/PluginHostTests.cs` proves a throwing plugin is isolated.
- `tests/eBayHero.Plugins.Tests/PluginRegistryTests.cs` proves addon gating and safe removal.
- `tests/eBayHero.Plugins.Tests/PluginManifestTests.cs` proves manifest validation.
