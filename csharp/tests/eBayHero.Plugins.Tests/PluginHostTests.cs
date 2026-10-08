using eBayHero.Core.Entitlements;
using eBayHero.Core.Plugins;
using eBayHero.Plugins.AiSuite;
using eBayHero.Plugins.CardOps;

namespace eBayHero.Plugins.Tests;

public sealed class PluginHostTests
{
    /// <summary>A deliberately broken plugin used to prove crash isolation.</summary>
    private sealed class FaultyPlugin : EcosystemPluginBase
    {
        public override string Id => "faulty";
        public override string Name => "Faulty Plugin";
        public override string Version => "0.0.1";
        public override PluginTier Tier => PluginTier.Core;
        public override IReadOnlyList<PluginHook> Hooks => [new("inventory.item.created", "Always throws.")];

        public override ValueTask InitializeAsync(PluginContext context, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("faulty initialize");

        public override ValueTask<PluginHookResult> OnHookAsync(PluginHookContext context, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("faulty hook");
    }

    private static (PluginRegistry Registry, PluginHost Host) Build(params string[] addons)
    {
        var entitlements = new EntitlementService(new EntitlementOptions { Tier = EntitlementTier.Free, Addons = addons });
        var registry = new PluginRegistry(entitlements);
        registry.Register(new CardOpsPlugin());
        var host = new PluginHost(registry, entitlements);
        return (registry, host);
    }

    [Fact]
    public async Task DispatchHook_ReachesActivePlugins()
    {
        var (_, host) = Build(FeatureCatalog.CardOpsAddon);

        var outcomes = await host.DispatchHookAsync("inventory.item.created", new Dictionary<string, object?> { ["sportOrGame"] = "Baseball" });

        var outcome = Assert.Single(outcomes);
        Assert.Equal(CardOpsPlugin.PluginId, outcome.PluginId);
        Assert.True(outcome.Handled);
        Assert.False(outcome.Failed);
    }

    [Fact]
    public async Task DispatchHook_IsolatesAFaultyPlugin()
    {
        var entitlements = new EntitlementService(new EntitlementOptions { Addons = [FeatureCatalog.CardOpsAddon] });
        var registry = new PluginRegistry(entitlements);
        registry.Register(new FaultyPlugin());
        registry.Register(new CardOpsPlugin());
        var errors = new List<string>();
        var host = new PluginHost(registry, entitlements, onPluginError: (id, _) => errors.Add(id));

        var outcomes = await host.DispatchHookAsync("inventory.item.created", new Dictionary<string, object?>());

        // The faulty plugin failed but the healthy plugin still ran, and the host did not throw.
        Assert.Contains(outcomes, outcome => outcome.PluginId == "faulty" && outcome.Failed);
        Assert.Contains(outcomes, outcome => outcome.PluginId == CardOpsPlugin.PluginId && outcome.Handled);
        Assert.Contains("faulty", errors);
    }

    [Fact]
    public async Task Initialize_IsolatesFaultyPlugin()
    {
        var entitlements = new EntitlementService(new EntitlementOptions { Addons = [FeatureCatalog.CardOpsAddon] });
        var registry = new PluginRegistry(entitlements);
        registry.Register(new FaultyPlugin());
        registry.Register(new CardOpsPlugin());
        var host = new PluginHost(registry, entitlements, onPluginError: (_, _) => { });

        await host.InitializeAsync();

        Assert.True(registry.IsPluginActive(CardOpsPlugin.PluginId));
    }

    [Fact]
    public async Task InvokeRoute_ReturnsPluginResponse()
    {
        var (_, host) = Build(FeatureCatalog.CardOpsAddon);

        var result = await host.InvokeRouteAsync("POST", "/plugins/cardops/draft", body: "{}");

        Assert.Equal(200, result.StatusCode);
        Assert.NotNull(result.Body);
    }

    [Fact]
    public async Task InvokeRoute_IsBlockedWhenFeatureIsNotEntitled()
    {
        // CardOps enabled but AI recognition is not part of the free plan.
        var (_, host) = Build(FeatureCatalog.CardOpsAddon);

        var result = await host.InvokeRouteAsync("POST", "/plugins/cardops/recognize", body: "{}");

        Assert.Equal(402, result.StatusCode);
    }

    [Fact]
    public async Task InvokeRoute_ReturnsAiResultWhenSuiteIsActive()
    {
        var (_, host) = Build(FeatureCatalog.CardOpsAddon, FeatureCatalog.AiSuiteAddon);

        var result = await host.InvokeRouteAsync("POST", "/plugins/cardops/recognize", body: "{\"imagePath\":\"front.jpg\"}");

        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task InvokeRoute_UnknownPathReturnsNotFound()
    {
        var (_, host) = Build(FeatureCatalog.CardOpsAddon);

        var result = await host.InvokeRouteAsync("POST", "/plugins/cardops/does-not-exist");

        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task AiSuitePlugin_IsActiveOnlyWithItsAddon()
    {
        var entitlements = new EntitlementService(new EntitlementOptions { Tier = EntitlementTier.Pro });
        var registry = new PluginRegistry(entitlements);
        registry.Register(new AiSuitePlugin());
        var host = new PluginHost(registry, entitlements);

        // Without the ai-suite addon the plugin is inactive, so its route is not mounted.
        Assert.Equal(404, (await host.InvokeRouteAsync("POST", "/plugins/ai-suite/analyze")).StatusCode);

        entitlements.SetTier(EntitlementTier.Pro, [FeatureCatalog.AiSuiteAddon]);
        Assert.Equal(200, (await host.InvokeRouteAsync("POST", "/plugins/ai-suite/analyze")).StatusCode);
    }
}
