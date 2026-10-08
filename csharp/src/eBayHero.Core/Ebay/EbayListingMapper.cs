using eBayHero.Core.Models;

namespace eBayHero.Core.Ebay;

/// <summary>
/// Maps internal inventory + pricing data onto eBay Inventory/Offer payloads using a
/// field-mapping profile. This is the seam that keeps eBay-specific field names out of
/// the domain model: change the profile, not the domain.
/// </summary>
public sealed class EbayListingMapper
{
    public EbayDraftPayload Map(
        InventoryItem item,
        IReadOnlyList<Photo> photos,
        PriceSnapshot? price,
        EbayFieldMappingProfile profile,
        string marketplaceId = "EBAY_US",
        string currency = "USD")
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(profile);

        var sku = BuildSku(item);
        var title = ListingTemplateRenderer.ClampTitle(
            ListingTemplateRenderer.Render(profile.Template.TitleTemplate, item));
        if (string.IsNullOrWhiteSpace(title))
        {
            title = ListingTemplateRenderer.ClampTitle(item.Name);
        }

        var description = ListingTemplateRenderer.Render(profile.Template.DescriptionTemplate, item);
        var aspects = BuildAspects(item, profile);
        var imagePaths = OrderPhotos(photos).Select(photo => photo.FullPath).Where(path => !string.IsNullOrWhiteSpace(path)).ToList();

        // Never invent a market price: fall back to a conservative $0.99 auction start so
        // the draft is publishable but obviously needs review.
        var priceValue = price is { MarketPrice: > 0 } ? price.MarketPrice : 0.99m;

        var inventoryItem = new EbayInventoryItemPayload(
            sku,
            string.IsNullOrWhiteSpace(profile.Template.Condition) ? "USED_GOOD" : profile.Template.Condition,
            aspects,
            imagePaths);

        var offer = new EbayOfferPayload(
            sku,
            marketplaceId,
            profile.Template.CategoryId,
            "FIXED_PRICE",
            priceValue,
            currency,
            title,
            description,
            1,
            profile.Payment.PaymentPolicyId,
            profile.Shipping.FulfillmentPolicyId,
            profile.Returns.ReturnPolicyId,
            profile.Shipping.HandlingTimeDays,
            imagePaths);

        return new EbayDraftPayload(inventoryItem, offer);
    }

    /// <summary>
    /// Front photos first, then back, then detail shots - the order eBay expects and the
    /// order the export service already uses.
    /// </summary>
    private static IEnumerable<Photo> OrderPhotos(IEnumerable<Photo> photos) =>
        photos.OrderBy(photo => photo.ViewType switch
        {
            PhotoViewType.Front => 0,
            PhotoViewType.Back => 1,
            PhotoViewType.ConditionCloseup => 3,
            _ => 2
        });

    private static string BuildSku(InventoryItem item)
    {
        var raw = string.IsNullOrWhiteSpace(item.CardNumber)
            ? item.Id
            : $"{item.Year}-{item.Brand}-{item.CardNumber}-{item.Id}";
        return new string(raw.Where(character => !char.IsWhiteSpace(character)).ToArray());
    }

    private static IReadOnlyDictionary<string, string> BuildAspects(InventoryItem item, EbayFieldMappingProfile profile)
    {
        var aspects = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Template defaults first, then explicit field mappings so mappings win.
        foreach (var (key, value) in profile.Template.DefaultItemSpecifics)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                aspects[key] = value;
            }
        }

        foreach (var (internalField, ebayAspect) in profile.FieldMap)
        {
            var value = internalField switch
            {
                nameof(InventoryItem.Year) => item.Year,
                nameof(InventoryItem.Brand) => item.Brand,
                nameof(InventoryItem.SetName) => item.SetName,
                nameof(InventoryItem.CardNumber) => item.CardNumber,
                nameof(InventoryItem.SerialNumber) => item.SerialNumber,
                nameof(InventoryItem.PlayerOrTitle) => item.PlayerOrTitle,
                nameof(InventoryItem.Team) => item.Team,
                nameof(InventoryItem.GradingCompany) => item.GradingCompany,
                nameof(InventoryItem.Grade) => item.Grade,
                nameof(InventoryItem.SportOrGame) => item.SportOrGame,
                nameof(InventoryItem.Manufacturer) => item.Manufacturer,
                nameof(InventoryItem.Condition) => item.Condition,
                _ => string.Empty
            };

            if (!string.IsNullOrWhiteSpace(value))
            {
                aspects[ebayAspect] = value;
            }
        }

        return aspects;
    }
}
