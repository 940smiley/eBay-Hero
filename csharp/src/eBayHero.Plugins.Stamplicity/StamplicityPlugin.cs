using eBayHero.Core.Entitlements;
using eBayHero.Core.Plugins;

namespace eBayHero.Plugins.Stamplicity;

/// <summary>
/// Stamplicity vertical addon: philately (stamp) inventory workflows.
///
/// Free: catalog entry with Scott / Stanley Gibbons fields, manual image attachment, and
/// standard eBay draft staging. Premium: AI visual identification (perforations,
/// watermarks, centering), valuation comps, and fully automated listing.
/// </summary>
public sealed class StamplicityPlugin : EcosystemPluginBase
{
    public const string PluginId = "stamplicity";
    public const string HookInventoryItemCreated = "inventory.item.created";
    public const string HookListingDraftCreated = "listing.draft.created";

    public override string Id => PluginId;

    public override string Name => "Stamplicity";

    public override string Version => "1.0.0";

    public override PluginTier Tier => PluginTier.Free;

    public override IReadOnlyList<PluginCapability> Capabilities =>
    [
        new("stamp.catalog.schema", "Stamp catalog schema", FeatureKey.StampCatalogSchema, "Scott and Stanley Gibbons catalog numbers plus country/denomination."),
        new("stamp.manual-image", "Manual image attachment", FeatureKey.ManualStampImageAttach, "Attach and order stamp photos by hand."),
        new("stamp.draft-staging", "Standard eBay draft staging", FeatureKey.StampDraftStaging, "Stage stamp drafts for review."),
        new("stamp.ai-visual-id", "AI philately visual identification", FeatureKey.AiPhilatelyVisualId, "Perforation counts, watermark tagging, centering estimation."),
        new("stamp.valuation-comps", "Automated stamp valuation comps", FeatureKey.StampValuationComps, "Value stamps from comparable sales."),
        new("stamp.auto-listing", "Stamp auto-listing", FeatureKey.StampAutoListing, "Publish fully mapped stamp listings.")
    ];

    public override IReadOnlyList<PluginHook> Hooks =>
    [
        new(HookInventoryItemCreated, "Apply stamp catalog defaults when an item is created.", Order: 11),
        new(HookListingDraftCreated, "Enrich stamp item specifics before publishing.", Order: 21)
    ];

    public override IReadOnlyList<PluginRoute> Routes =>
    [
        new("POST", "/plugins/stamplicity/identify", "IdentifyStamp", FeatureKey.AiPhilatelyVisualId),
        new("POST", "/plugins/stamplicity/valuation", "ValueStamp", FeatureKey.StampValuationComps)
    ];

    public override ValueTask<PluginHookResult> OnHookAsync(PluginHookContext context, CancellationToken cancellationToken = default)
    {
        if (string.Equals(context.HookName, HookInventoryItemCreated, StringComparison.OrdinalIgnoreCase))
        {
            return ValueTask.FromResult(PluginHookResult.Ok(
                "Stamplicity applied stamp catalog schema.",
                new Dictionary<string, object?> { ["schema"] = "stamplicity.v1", ["fields"] = StampCatalogFields.Names }));
        }

        if (string.Equals(context.HookName, HookListingDraftCreated, StringComparison.OrdinalIgnoreCase))
        {
            if (!HasFeature(FeatureKey.StampAutoListing))
            {
                return ValueTask.FromResult(PluginHookResult.Ok("Stamplicity staged the stamp draft for review (auto-listing not entitled)."));
            }

            return ValueTask.FromResult(PluginHookResult.Ok("Stamplicity prepared the stamp listing for automated publish."));
        }

        return ValueTask.FromResult(PluginHookResult.Ignored);
    }

    public override ValueTask<PluginRouteResult> InvokeRouteAsync(PluginRouteContext context, CancellationToken cancellationToken = default)
    {
        var body = PluginJson.ParseBody(context.Body);
        return ValueTask.FromResult(context.Path switch
        {
            "/plugins/stamplicity/identify" => PluginRouteResult.Json(PhilatelyRecognition.Simulate(GetString(body, "imagePath"))),
            "/plugins/stamplicity/valuation" => PluginRouteResult.Json(StampValuation.Simulate(GetString(body, "catalogNumber"))),
            _ => PluginRouteResult.NotFound()
        });
    }
}

/// <summary>The catalog fields Stamplicity stores for every stamp.</summary>
public static class StampCatalogFields
{
    public static readonly IReadOnlyList<string> Names =
    [
        "ScottNumber",
        "StanleyGibbonsNumber",
        "Country",
        "Year",
        "Denomination",
        "Perforations",
        "Watermark",
        "Centering",
        "Condition"
    ];
}
