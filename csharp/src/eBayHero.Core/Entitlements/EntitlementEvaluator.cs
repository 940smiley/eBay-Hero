namespace eBayHero.Core.Entitlements;

/// <summary>
/// Pure function that turns (tier, enabled addons) into the concrete set of granted
/// feature keys. Kept separate from <see cref="EntitlementService"/> so the gating rules
/// can be unit tested and reused by the demo simulator without any I/O.
/// </summary>
public static class EntitlementEvaluator
{
    public static EntitlementSnapshot Evaluate(
        EntitlementTier tier,
        IEnumerable<string> addons,
        string source,
        DateTimeOffset? expiresUtc = null,
        bool isTrial = false,
        DateTimeOffset? trialEndsUtc = null)
    {
        var addonList = addons
            .Where(addon => !string.IsNullOrWhiteSpace(addon))
            .Select(addon => addon.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var features = new HashSet<FeatureKey>();
        var aiSuiteActive = addonList.Contains(FeatureCatalog.AiSuiteAddon);

        foreach (var descriptor in FeatureCatalog.All.Values)
        {
            var ownsAddon = string.Equals(descriptor.OwnerAddon, FeatureCatalog.CoreAddon, StringComparison.OrdinalIgnoreCase);

            // Core features are always considered; plugin features require the plugin to
            // be enabled so removing an addon cleanly disables its capabilities.
            if (!ownsAddon && !addonList.Contains(descriptor.OwnerAddon))
            {
                continue;
            }

            var tierSatisfied = descriptor.MinimumTier == EntitlementTier.Free || tier >= descriptor.MinimumTier;
            if (tierSatisfied)
            {
                features.Add(descriptor.Key);
            }
        }

        // The All-in-One AI Suite is a master unlock: when active it grants every AI
        // capability across whichever plugins are installed.
        if (aiSuiteActive)
        {
            foreach (var key in FeatureCatalog.AiSuiteUnlocked)
            {
                features.Add(key);
            }
        }

        return new EntitlementSnapshot(tier, addonList, features, source, expiresUtc, isTrial, trialEndsUtc);
    }

    /// <summary>Everything unlocked - used by development builds and the demo's "Pro + All" preset.</summary>
    public static EntitlementSnapshot Everything(IEnumerable<string> addons) =>
        Evaluate(EntitlementTier.Pro, addons, "development");
}
