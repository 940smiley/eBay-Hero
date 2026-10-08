namespace eBayHero.Core.Entitlements;

/// <summary>
/// Every gate-able capability in the eBay Hero ecosystem.
///
/// Feature keys are the single vocabulary shared by the core product, the plugin
/// registry, and the license/entitlement controller. Core services never branch on a
/// tier directly; they ask <see cref="IEntitlementService.HasFeature"/> (or the plugin
/// host) whether a key is granted. That keeps paywall logic out of business workflows.
/// </summary>
public enum FeatureKey
{
    // ---- Core eBay Hero: Free tier ------------------------------------------
    ManualListingGeneration,
    CsvImportExport,
    ManualInventorySync,
    StandardDraftCreation,
    SingleAccountConnection,

    // ---- Core eBay Hero: Pro tier -------------------------------------------
    ContinuousBackgroundSync,
    MultiAccountRouting,
    AutoRelisting,
    AutomatedRepricing,
    BulkApiPublishing,

    // ---- CardOps plugin: Free ----------------------------------------------
    CardInventorySchema,
    ManualCardEntry,
    CardDraftExport,

    // ---- CardOps plugin: Pro addon -----------------------------------------
    AiCardRecognition,
    CardGradingDetection,
    CardCompPricing,
    CardAttributeAutofill,

    // ---- Stamplicity plugin: Free ------------------------------------------
    StampCatalogSchema,
    ManualStampImageAttach,
    StampDraftStaging,

    // ---- Stamplicity plugin: Pro addon -------------------------------------
    AiPhilatelyVisualId,
    StampValuationComps,
    StampAutoListing,

    // ---- Cross-plugin AI addon ---------------------------------------------
    AiVisionSuite
}

/// <summary>
/// Static metadata describing each feature: which tier or addon grants it, and which
/// plugin (if any) owns it. Used by the entitlement controller, the demo, and docs so
/// the Free/Pro matrix has exactly one source of truth in code.
/// </summary>
public static class FeatureCatalog
{
    public const string CoreAddon = "ebayhero.core";
    public const string CardOpsAddon = "cardops";
    public const string StamplicityAddon = "stamplicity";
    public const string AiSuiteAddon = "ai-suite";

    public static readonly IReadOnlyDictionary<FeatureKey, FeatureDescriptor> All =
        new Dictionary<FeatureKey, FeatureDescriptor>
        {
            [FeatureKey.ManualListingGeneration] = new(FeatureKey.ManualListingGeneration, "Manual listing generation", EntitlementTier.Free, CoreAddon, "Build a single listing draft by hand."),
            [FeatureKey.CsvImportExport] = new(FeatureKey.CsvImportExport, "CSV import/export", EntitlementTier.Free, CoreAddon, "Import inventory and export eBay-safe CSVs."),
            [FeatureKey.ManualInventorySync] = new(FeatureKey.ManualInventorySync, "Manual inventory sync", EntitlementTier.Free, CoreAddon, "Push inventory to eBay on demand."),
            [FeatureKey.StandardDraftCreation] = new(FeatureKey.StandardDraftCreation, "Standard draft creation", EntitlementTier.Free, CoreAddon, "Create standard listing drafts."),
            [FeatureKey.SingleAccountConnection] = new(FeatureKey.SingleAccountConnection, "Single account connection", EntitlementTier.Free, CoreAddon, "Connect one eBay seller account."),

            [FeatureKey.ContinuousBackgroundSync] = new(FeatureKey.ContinuousBackgroundSync, "Continuous background sync", EntitlementTier.Pro, CoreAddon, "Keep inventory and orders in sync automatically."),
            [FeatureKey.MultiAccountRouting] = new(FeatureKey.MultiAccountRouting, "Multi-account routing", EntitlementTier.Pro, CoreAddon, "Route items to different seller accounts."),
            [FeatureKey.AutoRelisting] = new(FeatureKey.AutoRelisting, "Auto-relisting", EntitlementTier.Pro, CoreAddon, "Relist ended items on a schedule."),
            [FeatureKey.AutomatedRepricing] = new(FeatureKey.AutomatedRepricing, "Automated repricing rules", EntitlementTier.Pro, CoreAddon, "Apply repricing rules from comparable sales."),
            [FeatureKey.BulkApiPublishing] = new(FeatureKey.BulkApiPublishing, "Bulk API batch publishing", EntitlementTier.Pro, CoreAddon, "Publish many drafts in a single batch."),

            [FeatureKey.CardInventorySchema] = new(FeatureKey.CardInventorySchema, "Card inventory schema", EntitlementTier.Free, CardOpsAddon, "Sports/TCG card fields (year, brand, set, number, grade)."),
            [FeatureKey.ManualCardEntry] = new(FeatureKey.ManualCardEntry, "Manual card detail entry", EntitlementTier.Free, CardOpsAddon, "Type in card details by hand."),
            [FeatureKey.CardDraftExport] = new(FeatureKey.CardDraftExport, "Card export to eBay drafts", EntitlementTier.Free, CardOpsAddon, "Turn cards into standard eBay drafts."),
            [FeatureKey.AiCardRecognition] = new(FeatureKey.AiCardRecognition, "AI card recognition/OCR", EntitlementTier.Pro, CardOpsAddon, "Read card fronts/backs and fill fields automatically."),
            [FeatureKey.CardGradingDetection] = new(FeatureKey.CardGradingDetection, "Automated grading detection", EntitlementTier.Pro, CardOpsAddon, "Detect grading company and grade from labels."),
            [FeatureKey.CardCompPricing] = new(FeatureKey.CardCompPricing, "Automated comp pricing", EntitlementTier.Pro, CardOpsAddon, "Price from authorized sold-comparable data."),
            [FeatureKey.CardAttributeAutofill] = new(FeatureKey.CardAttributeAutofill, "Automated attribute population", EntitlementTier.Pro, CardOpsAddon, "Populate item specifics from recognition output."),

            [FeatureKey.StampCatalogSchema] = new(FeatureKey.StampCatalogSchema, "Stamp catalog schema", EntitlementTier.Free, StamplicityAddon, "Scott/Stanley Gibbons catalog fields."),
            [FeatureKey.ManualStampImageAttach] = new(FeatureKey.ManualStampImageAttach, "Manual image attachment", EntitlementTier.Free, StamplicityAddon, "Attach and order stamp photos."),
            [FeatureKey.StampDraftStaging] = new(FeatureKey.StampDraftStaging, "Standard eBay draft staging", EntitlementTier.Free, StamplicityAddon, "Stage stamp drafts for review."),
            [FeatureKey.AiPhilatelyVisualId] = new(FeatureKey.AiPhilatelyVisualId, "AI philately visual identification", EntitlementTier.Pro, StamplicityAddon, "Perforation counts, watermark tagging, centering estimation."),
            [FeatureKey.StampValuationComps] = new(FeatureKey.StampValuationComps, "Automated stamp valuation comps", EntitlementTier.Pro, StamplicityAddon, "Value stamps from comparable sales."),
            [FeatureKey.StampAutoListing] = new(FeatureKey.StampAutoListing, "Stamp auto-listing", EntitlementTier.Pro, StamplicityAddon, "Publish fully-mapped stamp listings."),

            [FeatureKey.AiVisionSuite] = new(FeatureKey.AiVisionSuite, "All-in-One AI Suite", EntitlementTier.Pro, AiSuiteAddon, "Unlocks AI vision, OCR, and automated valuation across every active plugin.")
        };


    /// <summary>
    /// Features unlocked purely by the All-in-One AI Suite addon. When that addon is
    /// active these keys are granted regardless of the individual plugin addon state.
    /// </summary>
    public static readonly IReadOnlySet<FeatureKey> AiSuiteUnlocked =
        new HashSet<FeatureKey>
        {
            FeatureKey.AiVisionSuite,
            FeatureKey.AiCardRecognition,
            FeatureKey.CardGradingDetection,
            FeatureKey.CardCompPricing,
            FeatureKey.CardAttributeAutofill,
            FeatureKey.AiPhilatelyVisualId,
            FeatureKey.StampValuationComps,
            FeatureKey.StampAutoListing
        };
}

/// <summary>Immutable description of a single feature gate.</summary>
public sealed record FeatureDescriptor(
    FeatureKey Key,
    string Name,
    EntitlementTier MinimumTier,
    string OwnerAddon,
    string Description);

