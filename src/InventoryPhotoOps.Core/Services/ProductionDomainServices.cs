using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using InventoryPhotoOps.Core.Models;

namespace InventoryPhotoOps.Core.Services;

public sealed class RootManagementService(IPathService pathService) : IRootManagementService
{
    public RootValidationResult ValidateRoot(string path, IEnumerable<SourceRoot> existingRoots)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return new RootValidationResult(string.Empty, RootAvailability.Unknown, false, "Root path is required.");
        }

        var normalized = pathService.NormalizePath(path);
        var duplicate = existingRoots.Any(root => pathService.IsSamePath(root.Path, normalized));
        if (duplicate)
        {
            return new RootValidationResult(normalized, RootAvailability.DuplicateEquivalent, true, "An equivalent source root is already configured.");
        }

        var availability = Directory.Exists(normalized) ? RootAvailability.Available : RootAvailability.Unavailable;
        return new RootValidationResult(normalized, availability, false, availability == RootAvailability.Available ? "Root is available." : "Root path is not currently available.");
    }
}

public sealed class PricingService : IPricingService
{
    public PriceComputationResult Compute(PriceComputationRequest request)
    {
        var soldKinds = new[]
        {
            PricingEvidenceKind.VerifiedOwnSale,
            PricingEvidenceKind.AuthorizedSoldComparable,
            PricingEvidenceKind.UserImportedSoldComparable
        };

        var values = request.Evidence
            .Where(e => e.Included && !e.IsOutlier && soldKinds.Contains(e.Kind))
            .Select(e => e.Amount)
            .Where(v => v > 0)
            .OrderBy(v => v)
            .ToList();

        if (values.Count == 0)
        {
            return new PriceComputationResult(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, request.EstimatedShipping, -request.EstimatedShipping, 0, PricingConfidence.Unknown);
        }

        var average = DecimalRound(values.Average());
        var median = DecimalRound(Median(values));
        var trimmed = DecimalRound(Trim(values).DefaultIfEmpty(median).Average());
        var low = values.First();
        var high = values.Last();
        var market = request.PriceLocked ? median : trimmed;
        var quick = DecimalRound(market * 0.85m);
        var premium = DecimalRound(market * 1.15m);
        var auction = DecimalRound(Math.Max(0.99m, quick * 0.60m));
        var fees = DecimalRound(market * request.FeeRate);
        var net = DecimalRound(market - fees - request.EstimatedShipping);
        var confidence = values.Count switch
        {
            >= 7 => PricingConfidence.High,
            >= 3 => PricingConfidence.Medium,
            _ => PricingConfidence.Low
        };

        if (request.PriceLocked)
        {
            confidence = PricingConfidence.Locked;
        }

        return new PriceComputationResult(low, median, average, trimmed, high, quick, market, premium, auction, fees, request.EstimatedShipping, net, values.Count, confidence);
    }

    private static IEnumerable<decimal> Trim(IReadOnlyList<decimal> values)
    {
        if (values.Count < 5)
        {
            return values;
        }

        var trim = Math.Max(1, (int)Math.Floor(values.Count * 0.10));
        return values.Skip(trim).Take(values.Count - trim * 2);
    }

    private static decimal Median(IReadOnlyList<decimal> values)
    {
        var middle = values.Count / 2;
        return values.Count % 2 == 1 ? values[middle] : (values[middle - 1] + values[middle]) / 2m;
    }

    private static decimal DecimalRound(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

public sealed class LotBuilderService : ILotBuilderService
{
    public LotBuildResult BuildLot(LotBuildRequest request)
    {
        var warnings = new List<string>();
        var duplicateItems = request.Items.GroupBy(i => i.Id, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicateItems.Count > 0 && !request.AllowMultipleActiveLots)
        {
            warnings.Add("Duplicate item IDs were supplied. Each physical item can appear once in a lot unless explicitly allowed.");
        }

        var lot = new SaleLot
        {
            Name = string.IsNullOrWhiteSpace(request.Name) ? $"Lot - {DateTimeOffset.Now:yyyyMMdd-HHmm}" : request.Name,
            Status = LotStatus.Draft
        };

        var sequence = 0;
        foreach (var item in request.Items.DistinctBy(i => i.Id, StringComparer.OrdinalIgnoreCase))
        {
            lot.Items.Add(new SaleLotItem
            {
                InventoryItemId = item.Id,
                InventoryItem = item,
                SortOrder = sequence++
            });
        }

        var values = lot.Items
            .Select(i => request.PricesByItemId.TryGetValue(i.InventoryItemId, out var price) ? price.MarketPrice : 0m)
            .ToList();
        lot.EstimatedIndividualValue = DecimalRound(values.Sum());
        lot.SuggestedLotValue = DecimalRound(lot.EstimatedIndividualValue * (values.Count >= 10 ? 0.70m : 0.80m));
        lot.SuggestedAuctionStart = DecimalRound(Math.Max(0.99m, lot.SuggestedLotValue * 0.50m));
        lot.SuggestedBuyItNow = DecimalRound(lot.SuggestedLotValue * 1.10m);
        lot.WeightOunces = DecimalRound(2m + values.Count * 0.25m);
        lot.Shipping = DecimalRound(lot.WeightOunces <= 4 ? 4.95m : 6.95m);
        lot.EstimatedFees = DecimalRound(lot.SuggestedLotValue * 0.1325m);
        lot.EstimatedNet = DecimalRound(lot.SuggestedLotValue - lot.EstimatedFees - lot.Shipping);
        lot.Rationale = $"Built from {values.Count} selected item(s); lot value discounts individual value for batch sale.";
        return new LotBuildResult(lot, warnings);
    }

    private static decimal DecimalRound(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

public sealed class ListingDraftService : IListingDraftService
{
    public MarketplaceListing CreateDraft(ListingDraftRequest request)
    {
        if (request.Item is null && request.Lot is null)
        {
            throw new ArgumentException("A listing draft requires either an item or a lot.");
        }

        var title = request.Lot is not null ? request.Lot.Name : BuildItemTitle(request.Item!);
        var listing = new MarketplaceListing
        {
            Kind = request.Lot is null ? MarketplaceListingKind.SingleCard : MarketplaceListingKind.Lot,
            Status = MarketplaceListingStatus.Draft,
            InventoryItemId = request.Item?.Id ?? string.Empty,
            InventoryItem = request.Item,
            SaleLotId = request.Lot?.Id ?? string.Empty,
            SaleLot = request.Lot,
            Title = FilenameSanitizer.Sanitize(title, 80, "Inventory item").Replace('_', ' '),
            Description = BuildDescription(request),
            Price = request.Price?.MarketPrice ?? request.Lot?.SuggestedBuyItNow ?? 0m,
            Sku = request.Item is null ? request.Lot!.Id : request.Item.Id,
            PublishingEnabled = false
        };

        var order = 0;
        foreach (var photo in request.Photos.OrderBy(p => ExportPhotoOrderComparer.GetOrder(p.ViewType)).ThenBy(p => p.FileName, StringComparer.OrdinalIgnoreCase))
        {
            listing.Photos.Add(new MarketplaceListingPhoto
            {
                PhotoId = photo.Id,
                Photo = photo,
                SortOrder = order++,
                ViewType = photo.ViewType
            });
        }

        return listing;
    }

    private static string BuildItemTitle(InventoryItem item)
    {
        var parts = new[] { item.Year, item.Brand, item.SetName, item.PlayerOrTitle, item.CardNumber.Length > 0 ? "#" + item.CardNumber : string.Empty, item.Grade }.Where(p => !string.IsNullOrWhiteSpace(p));
        return string.Join(' ', parts);
    }

    private static string BuildDescription(ListingDraftRequest request)
    {
        if (request.Lot is not null)
        {
            return $"Lot of {request.Lot.Items.Count} item(s). Review photos and condition notes before publishing.";
        }

        var item = request.Item!;
        return string.Join(Environment.NewLine, new[]
        {
            BuildItemTitle(item),
            string.IsNullOrWhiteSpace(item.Condition) ? "Condition: review required." : $"Condition: {item.Condition}",
            "Photos show the actual item."
        });
    }
}

public sealed class ListingAuditService : IListingAuditService
{
    public ListingAuditResult Audit(MarketplaceListing listing, InventoryItem? item, IReadOnlyList<Photo> photos, PriceSnapshot? price)
    {
        var findings = new List<ListingAuditFinding>();

        AddIf(findings, string.IsNullOrWhiteSpace(listing.Title), ListingAuditSeverity.Blocker, "listing.title.missing", "Listing title is missing.");
        AddIf(findings, listing.Title.Length > 80, ListingAuditSeverity.Warning, "listing.title.length", "Listing title is longer than the usual eBay title limit.");
        AddIf(findings, item is not null && string.IsNullOrWhiteSpace(item.CardNumber), ListingAuditSeverity.Warning, "identity.card-number.missing", "Card number is missing.");
        AddIf(findings, item is not null && item.Rookie && !listing.Title.Contains("rookie", StringComparison.OrdinalIgnoreCase), ListingAuditSeverity.Info, "identity.rookie.title", "Item is marked rookie but title does not mention rookie.");
        AddIf(findings, photos.All(p => p.ViewType != PhotoViewType.Front), ListingAuditSeverity.Blocker, "photo.front.missing", "Front photo is missing.");
        AddIf(findings, photos.All(p => p.ViewType != PhotoViewType.Back), ListingAuditSeverity.Warning, "photo.back.missing", "Back photo is missing.");
        AddIf(findings, photos.Any(p => p.IsMissing), ListingAuditSeverity.Blocker, "photo.file.missing", "One or more listing photos are missing from disk.");
        AddIf(findings, price is null || price.ComparableCount == 0, ListingAuditSeverity.Warning, "pricing.weak", "Pricing has no included sold comparable evidence.");
        AddIf(findings, listing.Quantity < 1, ListingAuditSeverity.Blocker, "listing.quantity.invalid", "Quantity must be at least 1.");
        AddIf(findings, listing.PublishingEnabled && listing.Status == MarketplaceListingStatus.Draft, ListingAuditSeverity.Error, "listing.publish.draft", "Publishing is enabled while listing is still draft.");

        return new ListingAuditResult(findings, findings.Any(f => f.Severity == ListingAuditSeverity.Blocker));
    }

    private static void AddIf(List<ListingAuditFinding> findings, bool condition, ListingAuditSeverity severity, string ruleId, string message)
    {
        if (!condition)
        {
            return;
        }

        findings.Add(new ListingAuditFinding
        {
            Severity = severity,
            FixSafety = AuditFixSafety.ManualOnly,
            RuleId = ruleId,
            Message = message
        });
    }
}

public sealed class SecretRedactor : ISecretRedactor
{
    public string Redact(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var redacted = text;
        var patterns = new[]
        {
            @"(?i)(client[_ -]?secret|cert[_ -]?id|access[_ -]?token|refresh[_ -]?token|password)\s*[:=]\s*[""']?[^""'\s]+",
            @"(?i)(Authorization:\s*Bearer\s+)[A-Za-z0-9._~+/=-]+",
            @"(?i)(Authorization:\s*Basic\s+)[A-Za-z0-9+/=]+"
        };

        foreach (var pattern in patterns)
        {
            redacted = System.Text.RegularExpressions.Regex.Replace(redacted, pattern, match =>
            {
                var value = match.Value;
                var separatorIndex = Math.Max(value.LastIndexOf(':'), value.LastIndexOf('='));
                return separatorIndex >= 0 ? value[..(separatorIndex + 1)] + " [REDACTED]" : "[REDACTED]";
            });
        }

        return redacted;
    }
}

public sealed class EbayConnectionService : IEbayConnectionService
{
    private static readonly string[] MinimalSellerScopes =
    [
        "https://api.ebay.com/oauth/api_scope",
        "https://api.ebay.com/oauth/api_scope/sell.account.readonly",
        "https://api.ebay.com/oauth/api_scope/sell.inventory.readonly",
        "https://api.ebay.com/oauth/api_scope/sell.inventory",
        "https://api.ebay.com/oauth/api_scope/sell.fulfillment.readonly"
    ];

    public IReadOnlyList<EbayCapability> GetCapabilities(EbayConnectionProfile profile)
    {
        var scopes = (profile.ScopeList ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var connected = profile.Status == EbayConnectionStatus.Connected;
        var publishing = connected && profile.PublishingEnabled && scopes.Contains("https://api.ebay.com/oauth/api_scope/sell.inventory");

        return
        [
            new("oauth", "OAuth user consent", connected, true, false, connected ? "Connected." : "Configure credentials and complete eBay consent."),
            new("account-policies", "Account business policies", connected && scopes.Contains("https://api.ebay.com/oauth/api_scope/sell.account.readonly"), true, false, "Requires sell.account.readonly or sell.account scope."),
            new("inventory-read", "Inventory read", connected && scopes.Contains("https://api.ebay.com/oauth/api_scope/sell.inventory.readonly"), true, false, "Requires sell.inventory.readonly or sell.inventory scope."),
            new("inventory-write", "Inventory and offer write", publishing, true, false, "Requires sell.inventory scope and local publishing enablement."),
            new("orders", "Seller order read", connected && scopes.Contains("https://api.ebay.com/oauth/api_scope/sell.fulfillment.readonly"), true, false, "Requires sell.fulfillment.readonly scope."),
            new("sold-comps", "Sold comparable access", false, true, true, "Only seller-owned sales or approved data access can be used; active asking prices are not sold values.")
        ];
    }

    public EbayAuthorizationStart StartAuthorization(EbayConnectionSettings settings, string callbackUri)
    {
        if (string.IsNullOrWhiteSpace(settings.ClientId))
        {
            throw new ArgumentException("Client ID/App ID is required.", nameof(settings));
        }

        if (string.IsNullOrWhiteSpace(settings.RuName))
        {
            throw new ArgumentException("RuName/redirect is required.", nameof(settings));
        }

        var state = CreateState();
        var scopes = settings.Scopes.Count == 0 ? MinimalSellerScopes : settings.Scopes;
        var baseUrl = settings.Environment == MarketplaceEnvironment.Sandbox
            ? "https://auth.sandbox.ebay.com/oauth2/authorize"
            : "https://auth.ebay.com/oauth2/authorize";
        var query = new Dictionary<string, string>
        {
            ["client_id"] = settings.ClientId,
            ["redirect_uri"] = settings.RuName,
            ["response_type"] = "code",
            ["scope"] = string.Join(' ', scopes),
            ["state"] = state
        };

        var url = baseUrl + "?" + string.Join('&', query.Select(kvp => Uri.EscapeDataString(kvp.Key) + "=" + Uri.EscapeDataString(kvp.Value)));
        return new EbayAuthorizationStart(url, state, DateTimeOffset.UtcNow.AddMinutes(15), scopes);
    }

    public bool ValidateOAuthState(string expectedState, string receivedState) =>
        !string.IsNullOrWhiteSpace(expectedState) &&
        !string.IsNullOrWhiteSpace(receivedState) &&
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expectedState), Encoding.UTF8.GetBytes(receivedState));

    private static string CreateState()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}

public static class ProductionJson
{
    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = false });
}
