# Plugin development guide

Build a vertical addon for eBay Hero without touching core code.

## The interface

```csharp
public interface IEcosystemPlugin
{
    string Id { get; }
    string Name { get; }
    string Version { get; }
    PluginTier Tier { get; }                         // Core | Free | Pro
    IReadOnlyList<PluginCapability> Capabilities { get; }
    IReadOnlyList<PluginHook> Hooks { get; }
    IReadOnlyList<PluginRoute> Routes { get; }
    ValueTask InitializeAsync(PluginContext context, CancellationToken cancellationToken = default);
    ValueTask<PluginHookResult> OnHookAsync(PluginHookContext context, CancellationToken cancellationToken = default);
    ValueTask<PluginRouteResult> InvokeRouteAsync(PluginRouteContext context, CancellationToken cancellationToken = default);
}
```

Most plugins derive from `EcosystemPluginBase`, which supplies empty defaults, entitlement
helpers, and payload parsing.

## 1. Create the project

```
src/eBayHero.Plugins.MyVertical/
  eBayHero.Plugins.MyVertical.csproj   -> references eBayHero.Core only
  MyVerticalPlugin.cs
  MyVerticalServices.cs
  PluginManifest.json
```

Reference **only** `eBayHero.Core`. Never reference another plugin.

## 2. Declare capabilities

A capability with a `FeatureKey` is only offered when the entitlement controller grants it:

```csharp
public override IReadOnlyList<PluginCapability> Capabilities =>
[
    new("my.schema", "My inventory schema", FeatureKey.CardInventorySchema, "Base fields."),
    new("my.ai", "My AI feature", FeatureKey.AiCardRecognition, "Premium capability.")
];
```

## 3. Subscribe to hooks

Hooks let the core notify plugins at well-defined moments. Declare the ones you handle and
return `Ignored` for anything else.

| Hook | When |
|---|---|
| `inventory.item.created` | a new inventory item is persisted |
| `listing.draft.created` | a listing draft is built, before validation |

```csharp
public override ValueTask<PluginHookResult> OnHookAsync(PluginHookContext context, CancellationToken ct = default)
{
    if (context.HookName == "listing.draft.created" && HasFeature(FeatureKey.AiCardRecognition))
    {
        return ValueTask.FromResult(PluginHookResult.Ok("enriched", output));
    }
    return ValueTask.FromResult(PluginHookResult.Ignored);
}
```

## 4. Mount routes

Routes are plugin-scoped and entitlement-gated by the host.

```csharp
public override IReadOnlyList<PluginRoute> Routes =>
[
    new("POST", "/plugins/myvertical/analyze", "Analyze", FeatureKey.AiCardRecognition)
];
```

Status semantics the host enforces for you:

- **404** — plugin inactive (disabled or addon not entitled).
- **402** — route mounted but its `RequiredFeature` is not granted.
- **500** — your handler threw; the host isolates it.

## 5. Write the manifest

`PluginManifest.json` (schema v1) lets the host discover and validate your plugin without
loading code:

```json
{
  "schemaVersion": 1,
  "id": "myvertical",
  "name": "My Vertical",
  "version": "1.0.0",
  "tier": "free",
  "entryPoint": "eBayHero.Plugins.MyVertical.dll",
  "capabilities": [
    { "id": "my.ai", "name": "My AI feature", "requiredFeature": "AiCardRecognition" }
  ],
  "hooks": [ { "name": "listing.draft.created", "order": 30 } ],
  "routes": [
    { "method": "POST", "path": "/plugins/myvertical/analyze", "handler": "Analyze", "requiredFeature": "AiCardRecognition" }
  ]
}
```

Validate it in a test:

```csharp
var result = PluginManifestLoader.Parse(json);
Assert.True(result.IsValid, string.Join("; ", result.Errors));
```

## 6. Register with the host

```csharp
var entitlements = new EntitlementService(new EntitlementOptions
{
    Tier = EntitlementTier.Pro,
    Addons = ["myvertical"]
});

var registry = new PluginRegistry(entitlements);
registry.Register(new MyVerticalPlugin());

var host = new PluginHost(registry, entitlements, onPluginError: (id, ex) => Log(id, ex));
await host.InitializeAsync();

var outcomes = await host.DispatchHookAsync("listing.draft.created", payload);
var result = await host.InvokeRouteAsync("POST", "/plugins/myvertical/analyze", body: json);
```

## Rules of the road

1. **Never hardcode a paywall.** Declare a `FeatureKey`; let the entitlement controller decide.
2. **Never throw from `InitializeAsync`/`OnHookAsync` intentionally.** The host isolates
   failures, but a failing plugin is invisible to users.
3. **Don't reference other plugins.** Coordinate through hooks.
4. **Treat payloads defensively.** Use `PluginJson.ParseBody`; validate required fields.
5. **Version your manifest.** Bump `schemaVersion` only with a new loader.
6. **Add tests.** Cover entitlement on/off, route gating, and hook behaviour.

## Reference implementations

- `src/eBayHero.Plugins.CardOps` — free schema + premium AI.
- `src/eBayHero.Plugins.Stamplicity` — catalog fields + AI philately.
- `src/eBayHero.Plugins.AiSuite` — a master-unlock addon with a single route.

Tests: `tests/eBayHero.Plugins.Tests`.
