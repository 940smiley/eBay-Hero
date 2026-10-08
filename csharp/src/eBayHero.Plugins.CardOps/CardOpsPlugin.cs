using eBayHero.Core.Entitlements;
using eBayHero.Core.Plugins;

namespace eBayHero.Plugins.CardOps;

/// <summary>
/// CardOps vertical addon: trading-card inventory routines on top of eBay Hero.
///
/// Free capabilities (schema, manual entry, draft export) are always available while the
/// plugin is enabled. AI capabilities (recognition, grading detection, comp pricing,
/// attribute autofill) declare a <see cref="FeatureKey"/> so the entitlement controller -
/// not this plugin - decides whether they are offered.
/// </summary>
public sealed class CardOpsPlugin : EcosystemPluginBase
{
    public const string PluginId = "cardops";
    public const string HookInventoryItemCreated = "inventory.item.created";
    public const string HookListingDraftCreated = "listing.draft.created";

    public override string Id => PluginId;

    public override string Name => "CardOps";

    public override string Version => "1.0.0";

    public override PluginTier Tier => PluginTier.Free;

    public override IReadOnlyList<PluginCapability> Capabilities =>
    [
        new("card.inventory.schema", "Card inventory schema", FeatureKey.CardInventorySchema, "Year, brand, set, card number, serial, grade fields."),
        new("card.manual-entry", "Manual card detail entry", FeatureKey.ManualCardEntry, "Hand-enter card details."),
        new("card.draft-export", "Card export to eBay drafts", FeatureKey.CardDraftExport, "Convert a card into a standard eBay draft."),
        new("card.ai-recognition", "AI card recognition/OCR", FeatureKey.AiCardRecognition, "Read front/back text and fill card fields."),
        new("card.grading-detection", "Automated grading detection", FeatureKey.CardGradingDetection, "Detect grading company and numeric grade."),
        new("card.comp-pricing", "Automated comp pricing", FeatureKey.CardCompPricing, "Value a card from authorized sold comps."),
        new("card.attribute-autofill", "Automated attribute population", FeatureKey.CardAttributeAutofill, "Populate eBay item specifics automatically.")
    ];

    public override IReadOnlyList<PluginHook> Hooks =>
    [
        new(HookInventoryItemCreated, "Normalize card-specific fields when an item is created.", Order: 10),
        new(HookListingDraftCreated, "Enrich card item specifics before a draft is published.", Order: 20)
    ];

    public override IReadOnlyList<PluginRoute> Routes =>
    [
        new("POST", "/plugins/cardops/recognize", "RecognizeCard", FeatureKey.AiCardRecognition),
        new("POST", "/plugins/cardops/price", "PriceCard", FeatureKey.CardCompPricing),
        new("POST", "/plugins/cardops/draft", "ExportDraft", FeatureKey.CardDraftExport)
    ];

    public override ValueTask<PluginHookResult> OnHookAsync(PluginHookContext context, CancellationToken cancellationToken = default)
    {
        if (string.Equals(context.HookName, HookInventoryItemCreated, StringComparison.OrdinalIgnoreCase))
        {
            var sport = GetString(context.Payload, "sportOrGame") ?? "Unknown";
            return ValueTask.FromResult(PluginHookResult.Ok(
                $"CardOps applied card schema for sport '{sport}'.",
                new Dictionary<string, object?> { ["schema"] = "cardops.v1", ["sportOrGame"] = sport }));
        }

        if (string.Equals(context.HookName, HookListingDraftCreated, StringComparison.OrdinalIgnoreCase))
        {
            // Attribute autofill is premium; when not entitled we simply skip enrichment
            // instead of throwing, so the core draft pipeline is unaffected.
            if (!HasFeature(FeatureKey.CardAttributeAutofill))
            {
                return ValueTask.FromResult(PluginHookResult.Ok("CardOps left item specifics unchanged (attribute autofill not entitled)."));
            }

            return ValueTask.FromResult(PluginHookResult.Ok(
                "CardOps populated card item specifics.",
                new Dictionary<string, object?> { ["aspects"] = new[] { "Sport", "Manufacturer", "Set", "Card Number" } }));
        }

        return ValueTask.FromResult(PluginHookResult.Ignored);
    }

    public override ValueTask<PluginRouteResult> InvokeRouteAsync(PluginRouteContext context, CancellationToken cancellationToken = default)
    {
        var body = PluginJson.ParseBody(context.Body);
        return ValueTask.FromResult(context.Path switch
        {
            "/plugins/cardops/recognize" => PluginRouteResult.Json(CardRecognition.Simulate(GetString(body, "imagePath"))),
            "/plugins/cardops/price" => PluginRouteResult.Json(CardPricing.Simulate(GetString(body, "player"), GetString(body, "year"))),
            "/plugins/cardops/draft" => PluginRouteResult.Json(new { status = "draft-created", plugin = Id, schema = "cardops.v1" }),
            _ => PluginRouteResult.NotFound()
        });
    }
}
