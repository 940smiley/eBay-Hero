using eBayHero.Core.Entitlements;

namespace eBayHero.Core.Plugins;

/// <summary>
/// Convenience base class for first- and third-party plugins. It implements the boring
/// parts of <see cref="IEcosystemPlugin"/> (default empty extension points, entitlement
/// helpers, payload parsing) so an addon author only overrides what they care about.
/// </summary>
public abstract class EcosystemPluginBase : IEcosystemPlugin
{
    public abstract string Id { get; }

    public abstract string Name { get; }

    public abstract string Version { get; }

    public abstract PluginTier Tier { get; }

    public virtual IReadOnlyList<PluginCapability> Capabilities => [];

    public virtual IReadOnlyList<PluginHook> Hooks => [];

    public virtual IReadOnlyList<PluginRoute> Routes => [];

    /// <summary>Populated by <see cref="InitializeAsync"/>; null until the host wires the plugin up.</summary>
    protected PluginContext? Context { get; private set; }

    protected IEntitlementService Entitlements =>
        Context?.Entitlements ?? throw new InvalidOperationException(
            $"Plugin '{Id}' was used before InitializeAsync was called.");

    public virtual ValueTask InitializeAsync(PluginContext context, CancellationToken cancellationToken = default)
    {
        Context = context;
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Default hook behavior: acknowledge hooks we declared, ignore everything else. This
    /// lets the host dispatch every hook to every plugin without plugins needing to know
    /// about hooks they don't care about.
    /// </summary>
    public virtual ValueTask<PluginHookResult> OnHookAsync(PluginHookContext context, CancellationToken cancellationToken = default)
    {
        var declared = Hooks.Any(hook => string.Equals(hook.Name, context.HookName, StringComparison.OrdinalIgnoreCase));
        return ValueTask.FromResult(declared
            ? PluginHookResult.Ok($"{Name} acknowledged {context.HookName}.")
            : PluginHookResult.Ignored);
    }

    public virtual ValueTask<PluginRouteResult> InvokeRouteAsync(PluginRouteContext context, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(PluginRouteResult.NotFound());

    protected bool HasFeature(FeatureKey key) => Entitlements.HasFeature(key);

    protected static string? GetString(IReadOnlyDictionary<string, object?> payload, string key) =>
        payload.TryGetValue(key, out var value) ? value?.ToString() : null;

    protected static int GetInt(IReadOnlyDictionary<string, object?> payload, string key, int fallback = 0) =>
        payload.TryGetValue(key, out var value) && int.TryParse(value?.ToString(), out var parsed) ? parsed : fallback;
}
