using eBayHero.Core.Entitlements;

namespace eBayHero.Core.Plugins;

/// <summary>Commercial tier a plugin belongs to.</summary>
public enum PluginTier
{
    /// <summary>Shipped with eBay Hero itself.</summary>
    Core = 0,

    /// <summary>Free vertical addon (e.g. CardOps basics).</summary>
    Free = 1,

    /// <summary>Paid addon / AI suite.</summary>
    Pro = 2
}

/// <summary>
/// A discrete capability a plugin exposes. When <see cref="RequiredFeature"/> is set the
/// capability is only offered while the entitlement controller grants that feature, so
/// addon authors never hardcode paywalls.
/// </summary>
public sealed record PluginCapability(
    string Id,
    string Name,
    FeatureKey? RequiredFeature,
    string Description,
    bool IsEnabledByDefault = true);

/// <summary>
/// A named extension point a plugin subscribes to (e.g. <c>listing.draft.created</c>).
/// Hooks are dispatched best-effort; a failing plugin must never break the pipeline.
/// </summary>
public sealed record PluginHook(string Name, string Description, int Order = 0);

/// <summary>
/// A route a plugin mounts onto the host surface. Paths are plugin-scoped by convention
/// (<c>/plugins/{pluginId}/...</c>) and may require an entitlement feature.
/// </summary>
public sealed record PluginRoute(
    string Method,
    string Path,
    string HandlerName,
    FeatureKey? RequiredFeature = null);

/// <summary>Services handed to a plugin when it is initialized.</summary>
public sealed record PluginContext(
    IEntitlementService Entitlements,
    IReadOnlyDictionary<string, string> Settings,
    IServiceProvider? Services = null);

/// <summary>Inbound hook payload plus the hook name being dispatched.</summary>
public sealed record PluginHookContext(
    string HookName,
    IReadOnlyDictionary<string, object?> Payload);

/// <summary>Outcome of a single plugin handling a hook.</summary>
public sealed record PluginHookResult(
    bool Handled,
    string Message,
    IReadOnlyDictionary<string, object?>? Output = null)
{
    public static PluginHookResult Ignored { get; } = new(false, "Hook not handled by this plugin.");

    public static PluginHookResult Ok(string message = "ok", IReadOnlyDictionary<string, object?>? output = null) =>
        new(true, message, output);
}

/// <summary>Inbound route invocation.</summary>
public sealed record PluginRouteContext(
    string Method,
    string Path,
    IReadOnlyDictionary<string, string> Query,
    string? Body);

/// <summary>Route response returned by a plugin.</summary>
public sealed record PluginRouteResult(int StatusCode, object? Body, string ContentType = "application/json")
{
    public static PluginRouteResult Json(object? body, int statusCode = 200) => new(statusCode, body);

    public static PluginRouteResult NotFound(string message = "Route not found.") => new(404, new { error = message });

    public static PluginRouteResult Error(int statusCode, string message) => new(statusCode, new { error = message });
}

/// <summary>
/// The single interface every vertical addon implements. Intentionally small: identity,
/// declared extension points, and two async entry points.
/// </summary>
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
