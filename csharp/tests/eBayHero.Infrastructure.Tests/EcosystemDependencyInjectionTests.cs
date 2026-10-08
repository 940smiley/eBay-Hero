using eBayHero.Core.Ebay;
using eBayHero.Core.Entitlements;
using eBayHero.Core.Plugins;
using eBayHero.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace eBayHero.Infrastructure.Tests;

public sealed class EcosystemDependencyInjectionTests
{
    [Fact]
    public void AddEcosystem_WiresEntitlementsRegistryAndHost()
    {
        var services = new ServiceCollection();
        services.AddEcosystem(new EntitlementOptions
        {
            Tier = EntitlementTier.Pro,
            Addons = [FeatureCatalog.CardOpsAddon]
        });

        using var provider = services.BuildServiceProvider();

        var entitlements = provider.GetRequiredService<IEntitlementService>();
        Assert.True(entitlements.HasFeature(FeatureKey.BulkApiPublishing));
        Assert.True(entitlements.HasFeature(FeatureKey.CardInventorySchema));

        // The runtime is wired but empty until a host registers its plugins, which keeps
        // Infrastructure free of any dependency on plugin projects.
        var registry = provider.GetRequiredService<IPluginRegistry>();
        Assert.Empty(registry.Plugins);
        Assert.NotNull(provider.GetRequiredService<IPluginHost>());
        Assert.NotNull(provider.GetRequiredService<BulkDraftGenerator>());
    }

    [Fact]
    public void AddEcosystem_DefaultsToFreeTier()
    {
        var services = new ServiceCollection();
        services.AddEcosystem();
        using var provider = services.BuildServiceProvider();

        var entitlements = provider.GetRequiredService<IEntitlementService>();

        Assert.Equal(EntitlementTier.Free, entitlements.Snapshot.Tier);
        Assert.False(entitlements.HasFeature(FeatureKey.BulkApiPublishing));
    }
}
