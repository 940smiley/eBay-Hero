using eBayHero.Core.Entitlements;
using eBayHero.Core.Plugins;

namespace eBayHero.Plugins.AiSuite;

/// <summary>
/// All-in-One AI Suite addon.
///
/// This is the master entitlement: while it is active, the entitlement controller grants
/// every AI capability across all installed verticals (see
/// <see cref="FeatureCatalog.AiSuiteUnlocked"/>). The plugin itself exposes a single
/// analysis route that fan-out-capable plugins can delegate to, but its main job is to be
/// the switch that unlocks AI vision, OCR, and automated valuation everywhere at once.
/// </summary>
public sealed class AiSuitePlugin : EcosystemPluginBase
{
    public const string PluginId = "ai-suite";
    public const string HookListingDraftCreated = "listing.draft.created";

    public override string Id => PluginId;

    public override string Name => "All-in-One AI Suite";

    public override string Version => "1.0.0";

    public override PluginTier Tier => PluginTier.Pro;

    public override IReadOnlyList<PluginCapability> Capabilities =>
    [
        new("ai.vision", "AI vision", FeatureKey.AiVisionSuite, "Vision analysis across cards and stamps."),
        new("ai.ocr", "AI OCR", FeatureKey.AiCardRecognition, "Text extraction from labels, slabs, and certs."),
        new("ai.valuation", "AI valuation", FeatureKey.CardCompPricing, "Automated valuation from sold comparables.")
    ];

    public override IReadOnlyList<PluginHook> Hooks =>
    [
        new(HookListingDraftCreated, "Attach AI confidence metadata to drafts.", Order: 5)
    ];

    public override IReadOnlyList<PluginRoute> Routes =>
    [
        new("POST", "/plugins/ai-suite/analyze", "Analyze", FeatureKey.AiVisionSuite)
    ];

    public override ValueTask<PluginHookResult> OnHookAsync(PluginHookContext context, CancellationToken cancellationToken = default)
    {
        if (string.Equals(context.HookName, HookListingDraftCreated, StringComparison.OrdinalIgnoreCase))
        {
            return ValueTask.FromResult(PluginHookResult.Ok(
                "AI Suite annotated the draft with vision/OCR confidence.",
                new Dictionary<string, object?> { ["suite"] = "all-in-one", ["confidence"] = 0.9 }));
        }

        return ValueTask.FromResult(PluginHookResult.Ignored);
    }

    public override ValueTask<PluginRouteResult> InvokeRouteAsync(PluginRouteContext context, CancellationToken cancellationToken = default)
    {
        if (context.Path == "/plugins/ai-suite/analyze")
        {
            return ValueTask.FromResult(PluginRouteResult.Json(new
            {
                plugin = PluginId,
                engine = "local-stub",
                vision = new { labels = new[] { "trading-card", "slab", "autograph" }, confidence = 0.94 },
                ocr = new { text = "2023 TOPPS CHROME RUBEN AMARO #12 23/99 PSA 9", confidence = 0.91 },
                valuation = new { median = 15.00m, confidence = "medium", evidenceSource = "authorized-sold-comparables" }
            }));
        }

        return ValueTask.FromResult(PluginRouteResult.NotFound());
    }
}
