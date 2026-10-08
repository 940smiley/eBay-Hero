using eBayHero.Core.Entitlements;
using eBayHero.Core.Plugins;
using eBayHero.Plugins.CardOps;
using eBayHero.Plugins.Stamplicity;

namespace eBayHero.Plugins.Tests;

public sealed class PluginRegistryTests
{
    private static EntitlementService Entitlements(EntitlementTier tier, params string[] addons) =>
        new(new EntitlementOptions { Tier = tier, Addons = addons });

    [Fact]
    public void Register_RejectsDuplicateIds()
    {
        var registry = new PluginRegistry(Entitlements(EntitlementTier.Free, FeatureCatalog.CardOpsAddon));

        Assert.True(registry.Register(new CardOpsPlugin()));
        Assert.False(registry.Register(new CardOpsPlugin()));
        Assert.Single(registry.Plugins);
    }

    [Fact]
    public void PluginWithoutAddonEntitlement_IsNotActive()
    {
        var registry = new PluginRegistry(Entitlements(EntitlementTier.Free));
        registry.Register(new CardOpsPlugin());

        Assert.False(registry.IsPluginActive(CardOpsPlugin.PluginId));
        Assert.Empty(registry.ActivePlugins);
        Assert.Contains(registry.Describe(), descriptor => descriptor.Reason.Contains("Requires addon"));
    }

    [Fact]
    public void PluginWithAddonEntitlement_IsActive()
    {
        var registry = new PluginRegistry(Entitlements(EntitlementTier.Free, FeatureCatalog.CardOpsAddon));
        registry.Register(new CardOpsPlugin());

        Assert.True(registry.IsPluginActive(CardOpsPlugin.PluginId));
        Assert.Single(registry.ActivePlugins);
    }

    [Fact]
    public void DisablingAPlugin_RemovesItsHooksAndRoutes_WithoutBreakingOthers()
    {
        var registry = new PluginRegistry(Entitlements(EntitlementTier.Pro, FeatureCatalog.CardOpsAddon, FeatureCatalog.StamplicityAddon));
        registry.Register(new CardOpsPlugin());
        registry.Register(new StamplicityPlugin());

        Assert.NotEmpty(registry.ResolveHooks(CardOpsPlugin.HookInventoryItemCreated));

        registry.SetEnabled(CardOpsPlugin.PluginId, false);

        // CardOps is gone, Stamplicity is untouched, and the registry still answers queries.
        Assert.DoesNotContain(registry.ActivePlugins, plugin => plugin.Id == CardOpsPlugin.PluginId);
        Assert.True(registry.IsPluginActive(StamplicityPlugin.PluginId));
        Assert.Single(registry.ResolveHooks(CardOpsPlugin.HookInventoryItemCreated));
    }

    [Fact]
    public void Unregister_RemovesPluginAndLeavesRegistryUsable()
    {
        var registry = new PluginRegistry(Entitlements(EntitlementTier.Free, FeatureCatalog.CardOpsAddon));
        registry.Register(new CardOpsPlugin());

        Assert.True(registry.Unregister(CardOpsPlugin.PluginId));
        Assert.Null(registry.Find(CardOpsPlugin.PluginId));
        Assert.Empty(registry.Describe());
        Assert.False(registry.Unregister("does-not-exist"));
    }

    [Fact]
    public void ResolveRoutes_ReturnsRoutesFromActivePlugins()
    {
        var registry = new PluginRegistry(Entitlements(EntitlementTier.Free, FeatureCatalog.CardOpsAddon));
        registry.Register(new CardOpsPlugin());

        // Route resolution is purely structural; the host applies the entitlement gate.
        Assert.Single(registry.ResolveRoutes("POST", "/plugins/cardops/draft"));
        Assert.Single(registry.ResolveRoutes("POST", "/plugins/cardops/recognize"));
        Assert.Empty(registry.ResolveRoutes("GET", "/plugins/cardops/draft"));
    }
}
