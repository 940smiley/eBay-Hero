using eBayHero.Core.Entitlements;

namespace eBayHero.Core.Plugins;

/// <summary>Snapshot describing one registered plugin and why it is (or is not) active.</summary>
public sealed record PluginDescriptor(
    string Id,
    string Name,
    string Version,
    PluginTier Tier,
    bool Enabled,
    bool Active,
    string Reason,
    IReadOnlyList<PluginCapability> Capabilities);

/// <summary>
/// Central catalog of installed plugins. The registry is deliberately independent of any
/// UI or transport: it only knows how to register plugins, track their enabled state, and
/// resolve hooks/routes. Removing a plugin from the registry can never break core code
/// because core never calls a plugin directly.
/// </summary>
public interface IPluginRegistry
{
    IReadOnlyList<IEcosystemPlugin> Plugins { get; }

    /// <summary>Plugins that are enabled and whose addon entitlement is granted.</summary>
    IReadOnlyList<IEcosystemPlugin> ActivePlugins { get; }

    bool Register(IEcosystemPlugin plugin, bool enabled = true);

    bool Unregister(string pluginId);

    bool SetEnabled(string pluginId, bool enabled);

    bool IsEnabled(string pluginId);

    bool IsPluginActive(string pluginId);

    IEcosystemPlugin? Find(string pluginId);

    IReadOnlyList<PluginHook> ResolveHooks(string hookName);

    IReadOnlyList<(IEcosystemPlugin Plugin, PluginRoute Route)> ResolveRoutes(string method, string path);

    IReadOnlyList<PluginDescriptor> Describe();
}

/// <summary>Default in-memory registry implementation.</summary>
public sealed class PluginRegistry(IEntitlementService entitlements) : IPluginRegistry
{
    private readonly List<IEcosystemPlugin> _plugins = [];
    private readonly Dictionary<string, bool> _enabled = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<IEcosystemPlugin> Plugins => _plugins;

    public IReadOnlyList<IEcosystemPlugin> ActivePlugins =>
        _plugins.Where(plugin => IsPluginActive(plugin.Id)).ToList();

    public bool Register(IEcosystemPlugin plugin, bool enabled = true)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        if (_plugins.Any(existing => string.Equals(existing.Id, plugin.Id, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        _plugins.Add(plugin);
        _enabled[plugin.Id] = enabled;
        return true;
    }

    public bool Unregister(string pluginId)
    {
        var plugin = Find(pluginId);
        if (plugin is null)
        {
            return false;
        }

        _plugins.Remove(plugin);
        _enabled.Remove(plugin.Id);
        return true;
    }

    public bool SetEnabled(string pluginId, bool enabled)
    {
        var plugin = Find(pluginId);
        if (plugin is null)
        {
            return false;
        }

        _enabled[plugin.Id] = enabled;
        return true;
    }

    public bool IsEnabled(string pluginId) =>
        _enabled.TryGetValue(pluginId, out var enabled) && enabled;

    public bool IsPluginActive(string pluginId)
    {
        var plugin = Find(pluginId);
        if (plugin is null || !IsEnabled(pluginId))
        {
            return false;
        }

        // Core plugins are always available; vertical addons additionally require their
        // addon entitlement so disabling an addon disables its capabilities cleanly.
        return plugin.Tier == PluginTier.Core || entitlements.IsAddonActive(plugin.Id);
    }

    public IEcosystemPlugin? Find(string pluginId) =>
        _plugins.FirstOrDefault(plugin => string.Equals(plugin.Id, pluginId, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<PluginHook> ResolveHooks(string hookName) =>
        ActivePlugins
            .SelectMany(plugin => plugin.Hooks)
            .Where(hook => string.Equals(hook.Name, hookName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(hook => hook.Order)
            .ToList();

    public IReadOnlyList<(IEcosystemPlugin Plugin, PluginRoute Route)> ResolveRoutes(string method, string path) =>
        ActivePlugins
            .SelectMany(plugin => plugin.Routes.Select(route => (Plugin: plugin, Route: route)))
            .Where(entry =>
                string.Equals(entry.Route.Method, method, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(entry.Route.Path, path, StringComparison.OrdinalIgnoreCase))
            .ToList();

    public IReadOnlyList<PluginDescriptor> Describe() =>
        _plugins
            .Select(plugin =>
            {
                var active = IsPluginActive(plugin.Id);
                var reason = !IsEnabled(plugin.Id)
                    ? "Disabled"
                    : active
                        ? "Active"
                        : $"Requires addon '{plugin.Id}'";
                return new PluginDescriptor(plugin.Id, plugin.Name, plugin.Version, plugin.Tier, IsEnabled(plugin.Id), active, reason, plugin.Capabilities);
            })
            .ToList();
}
