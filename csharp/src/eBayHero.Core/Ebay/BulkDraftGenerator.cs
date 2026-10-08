using eBayHero.Core.Entitlements;
using eBayHero.Core.Models;

namespace eBayHero.Core.Ebay;

/// <summary>Input for a bulk draft run.</summary>
public sealed record BulkDraftRequest(
    IReadOnlyList<InventoryItem> Items,
    IReadOnlyDictionary<string, IReadOnlyList<Photo>> PhotosByItemId,
    IReadOnlyDictionary<string, PriceSnapshot> PricesByItemId,
    EbayFieldMappingProfile Profile,
    string MarketplaceId = "EBAY_US",
    int MaxBatchSize = 50);

/// <summary>Output of a bulk draft run.</summary>
public sealed record BulkDraftResult(
    IReadOnlyList<EbayDraftPayload> Drafts,
    IReadOnlyList<string> Warnings,
    bool BulkPublishingEntitled)
{
    public int DraftCount => Drafts.Count;
}

/// <summary>
/// Generates many eBay drafts from a batch of inventory items.
///
/// Note the deliberate separation: draft *generation* is always allowed, but the result
/// reports whether bulk *publishing* is entitled. The caller (UI/API) decides what to do
/// with that flag. That keeps the freemium gate out of the generation logic itself.
/// </summary>
public sealed class BulkDraftGenerator(EbayListingMapper mapper, IEntitlementService entitlements)
{
    public BulkDraftResult Generate(BulkDraftRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var warnings = new List<string>();
        var drafts = new List<EbayDraftPayload>();
        var batchSize = Math.Max(1, request.MaxBatchSize);

        foreach (var item in request.Items.Take(batchSize))
        {
            request.PhotosByItemId.TryGetValue(item.Id, out var photos);
            request.PricesByItemId.TryGetValue(item.Id, out var price);

            if (photos is null || photos.Count == 0)
            {
                warnings.Add($"Item '{item.Id}' has no photos; the draft will publish without images.");
            }

            if (price is null || price.MarketPrice <= 0)
            {
                warnings.Add($"Item '{item.Id}' has no verified price; a placeholder price was used.");
            }

            drafts.Add(mapper.Map(item, photos ?? [], price, request.Profile, request.MarketplaceId));
        }

        if (request.Items.Count > batchSize)
        {
            warnings.Add($"Batch limited to {batchSize} drafts; {request.Items.Count - batchSize} item(s) deferred.");
        }

        var entitled = entitlements.HasFeature(FeatureKey.BulkApiPublishing);
        if (!entitled)
        {
            warnings.Add("Bulk API batch publishing requires the Pro plan; drafts were generated for manual review.");
        }

        return new BulkDraftResult(drafts, warnings, entitled);
    }
}
