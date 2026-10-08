using System.Text.RegularExpressions;
using eBayHero.Core.Models;

namespace eBayHero.Core.Ebay;

/// <summary>A reusable listing template: title/description shape and default attributes.</summary>
public sealed record ListingTemplate(
    string Id,
    string Name,
    string TitleTemplate,
    string DescriptionTemplate,
    string CategoryId,
    string Condition,
    IReadOnlyDictionary<string, string> DefaultItemSpecifics);

/// <summary>Business policies and shipping defaults applied to generated offers.</summary>
public sealed record ShippingPolicyProfile(
    string Id,
    string Name,
    string FulfillmentPolicyId,
    decimal ShippingCost,
    string ServiceCode,
    int HandlingTimeDays);

public sealed record ReturnPolicyProfile(
    string Id,
    string Name,
    string ReturnPolicyId,
    bool ReturnsAccepted,
    int ReturnWindowDays,
    string ReturnShippingPayer);

public sealed record PaymentPolicyProfile(string Id, string Name, string PaymentPolicyId);

/// <summary>A node in an eBay category tree.</summary>
public sealed record CategoryNode(string CategoryId, string Name, IReadOnlyList<CategoryNode> Children)
{
    public CategoryNode? Find(string categoryId) =>
        CategoryId == categoryId ? this : Children.Select(child => child.Find(categoryId)).FirstOrDefault(node => node is not null);
}

/// <summary>
/// The complete field-mapping profile: which template and policies to use, plus a map of
/// internal field names to eBay aspect names so item specifics populate automatically.
/// </summary>
public sealed record EbayFieldMappingProfile(
    string Id,
    string Name,
    ListingTemplate Template,
    ShippingPolicyProfile Shipping,
    ReturnPolicyProfile Returns,
    PaymentPolicyProfile Payment,
    IReadOnlyDictionary<string, string> FieldMap);

/// <summary>eBay Inventory API inventory-item payload (subset used for draft generation).</summary>
public sealed record EbayInventoryItemPayload(
    string Sku,
    string Condition,
    IReadOnlyDictionary<string, string> Aspects,
    IReadOnlyList<string> ImageUrls);

/// <summary>eBay Inventory API offer payload (subset used for draft generation).</summary>
public sealed record EbayOfferPayload(
    string Sku,
    string MarketplaceId,
    string CategoryId,
    string Format,
    decimal Price,
    string Currency,
    string Title,
    string Description,
    int Quantity,
    string PaymentPolicyId,
    string FulfillmentPolicyId,
    string ReturnPolicyId,
    int HandlingTimeDays,
    IReadOnlyList<string> ImageUrls);

/// <summary>A draft ready to be pushed through the Inventory API.</summary>
public sealed record EbayDraftPayload(EbayInventoryItemPayload InventoryItem, EbayOfferPayload Offer);

/// <summary>
/// Renders <c>{Token}</c> placeholders from an inventory item. Deliberately separate from
/// the filename renderer because listing titles use display text, not sanitized names.
/// </summary>
public static class ListingTemplateRenderer
{
    public static string Render(string template, InventoryItem item)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Year"] = item.Year,
            ["Brand"] = item.Brand,
            ["Player"] = item.PlayerOrTitle,
            ["PlayerOrTitle"] = item.PlayerOrTitle,
            ["Set"] = item.SetName,
            ["CardNumber"] = item.CardNumber,
            ["Serial"] = item.SerialNumber,
            ["Team"] = item.Team,
            ["Grade"] = item.Grade,
            ["GradingCompany"] = item.GradingCompany,
            ["Category"] = item.Category
        };

        var rendered = Regex.Replace(template, @"\{(?<key>[A-Za-z0-9_]+)\}", match =>
        {
            var key = match.Groups["key"].Value;
            return values.TryGetValue(key, out var value) ? value : string.Empty;
        });

        return Regex.Replace(rendered, @"\s+", " ").Trim();
    }

    /// <summary>eBay titles are capped at 80 characters; trim on a word boundary when possible.</summary>
    public static string ClampTitle(string title, int limit = 80)
    {
        if (title.Length <= limit)
        {
            return title;
        }

        var truncated = title[..limit];
        var lastSpace = truncated.LastIndexOf(' ');
        return lastSpace > limit / 2 ? truncated[..lastSpace].TrimEnd() : truncated.TrimEnd();
    }
}
