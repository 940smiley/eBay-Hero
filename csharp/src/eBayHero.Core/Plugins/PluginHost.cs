using eBayHero.Core.Entitlements;

namespace eBayHero.Core.Plugins;

/// <summary>Result of dispatching one hook to one plugin.</summary>
public sealed record PluginHookOutcome(
    string PluginId,
    bool Handled,
    string Message,
    bool Failed = false,
    IReadOnlyDictionary<string, object?>? Output = null);

/// <summary>
/// Executes plugins on behalf of the core. Every call is wrapped so that a misbehaving
/// addon is isolated: it can fail, time out, or be disabled without ever taking down an
/// eBay Hero workflow. This isolation guarantee is what makes the architecture safely
/// modular.
/// </summary>
public interface IPluginHost
{
    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PluginHookOutcome>> DispatchHookAsync(
        string hookName,
        IReadOnlyDictionary<string, object?> payload,
        CancellationToken cancellationToken = default);

    Task<PluginRouteResult> InvokeRouteAsync(
        string method,
        string path,
        IReadOnlyDictionary<string, string>? query = null,
        string? body = null,
        CancellationToken cancellationToken = default);

    IReadOnlyList<PluginDescriptor> Describe();
}

/// <summary>Default host that fans work out to the active plugins in the registry.</summary>
public sealed class PluginHost(
    IPluginRegistry registry,
    IEntitlementService entitlements,
    IReadOnlyDictionary<string, string>? settings = null,
    IServiceProvider? services = null,
    Action<string, Exception>? onPluginError = null) : IPluginHost
{
    private readonly Dictionary<string, string> _settings =
        settings is null ? new(StringComparer.OrdinalIgnoreCase) : new(settings, StringComparer.OrdinalIgnoreCase);

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var context = new PluginContext(entitlements, _settings, services);
        foreach (var plugin in registry.ActivePlugins)
        {
            try
            {
                await plugin.InitializeAsync(context, cancellationToken);
            }
            catch (Exception exception)
            {
                Report(plugin.Id, exception);
            }
        }
    }

    public async Task<IReadOnlyList<PluginHookOutcome>> DispatchHookAsync(
        string hookName,
        IReadOnlyDictionary<string, object?> payload,
        CancellationToken cancellationToken = default)
    {
        var outcomes = new List<PluginHookOutcome>();
        var context = new PluginHookContext(hookName, payload);

        foreach (var plugin in registry.ActivePlugins)
        {
            // Only dispatch to plugins that declared the hook; this keeps fan-out small
            // and lets plugins ignore hooks they don't understand.
            if (!plugin.Hooks.Any(hook => string.Equals(hook.Name, hookName, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            try
            {
                var result = await plugin.OnHookAsync(context, cancellationToken);
                outcomes.Add(new PluginHookOutcome(plugin.Id, result.Handled, result.Message, false, result.Output));
            }
            catch (Exception exception)
            {
                Report(plugin.Id, exception);
                outcomes.Add(new PluginHookOutcome(plugin.Id, false, exception.Message, true));
            }
        }

        return outcomes;
    }

    public async Task<PluginRouteResult> InvokeRouteAsync(
        string method,
        string path,
        IReadOnlyDictionary<string, string>? query = null,
        string? body = null,
        CancellationToken cancellationToken = default)
    {
        var matches = registry.ResolveRoutes(method, path);
        if (matches.Count == 0)
        {
            return PluginRouteResult.NotFound($"No active plugin serves {method} {path}.");
        }

        var (plugin, route) = matches[0];

        // Route-level entitlement gate. The plugin stays mounted, but a locked route
        // returns 402-style information rather than executing.
        if (route.RequiredFeature is { } feature && !entitlements.HasFeature(feature))
        {
            return PluginRouteResult.Error(402, $"Feature '{feature}' is not included in the current plan.");
        }

        try
        {
            return await plugin.InvokeRouteAsync(
                new PluginRouteContext(method, path, query ?? new Dictionary<string, string>(), body),
                cancellationToken);
        }
        catch (Exception exception)
        {
            Report(plugin.Id, exception);
            return PluginRouteResult.Error(500, $"Plugin '{plugin.Id}' failed: {exception.Message}");
        }
    }

    public IReadOnlyList<PluginDescriptor> Describe() => registry.Describe();

    private void Report(string pluginId, Exception exception) =>
        onPluginError?.Invoke(pluginId, exception);
}
