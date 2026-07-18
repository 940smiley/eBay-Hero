using eBayHero.Core.Models;
using eBayHero.Core.Services;

namespace eBayHero.Core.Tests;

public sealed class ProductionDomainServiceTests
{
    [Fact]
    public void Pricing_UsesOnlySoldEvidenceForMarketValues()
    {
        var service = new PricingService();
        var result = service.Compute(new PriceComputationRequest(
            [
                new PriceEvidence { Kind = PricingEvidenceKind.AuthorizedSoldComparable, Amount = 10 },
                new PriceEvidence { Kind = PricingEvidenceKind.UserImportedSoldComparable, Amount = 20 },
                new PriceEvidence { Kind = PricingEvidenceKind.ActiveListing, Amount = 999 },
                new PriceEvidence { Kind = PricingEvidenceKind.VerifiedOwnSale, Amount = 30, IsOutlier = true }
            ],
            EstimatedShipping: 5,
            FeeRate: 0.13m));

        Assert.Equal(2, result.ComparableCount);
        Assert.Equal(15, result.Median);
        Assert.Equal(15, result.MarketPrice);
        Assert.Equal(PricingConfidence.Low, result.Confidence);
    }

    [Fact]
    public void LotBuilder_CalculatesLotTotalsAndWarnsAboutDuplicateItems()
    {
        var item = new InventoryItem { Id = "item-1", Name = "Card 1" };
        var prices = new Dictionary<string, PriceSnapshot>
        {
            ["item-1"] = new() { InventoryItemId = "item-1", MarketPrice = 20 }
        };

        var service = new LotBuilderService();
        var result = service.BuildLot(new LotBuildRequest("Test Lot", [item, item], prices));

        Assert.Single(result.Lot.Items);
        Assert.NotEmpty(result.Warnings);
        Assert.Equal(16, result.Lot.SuggestedLotValue);
    }

    [Fact]
    public void ListingAudit_BlocksPublishingWithoutFrontPhoto()
    {
        var service = new ListingAuditService();
        var listing = new MarketplaceListing { Title = "1989 Topps Test #1", Quantity = 1 };
        var item = new InventoryItem { Id = "item-1", CardNumber = "1" };

        var result = service.Audit(listing, item, [], null);

        Assert.True(result.BlocksPublishing);
        Assert.Contains(result.Findings, f => f.RuleId == "photo.front.missing");
        Assert.Contains(result.Findings, f => f.RuleId == "pricing.weak");
    }

    [Fact]
    public void ListingDraft_OrdersFrontBackPhotosAndKeepsPublishingDisabled()
    {
        var service = new ListingDraftService();
        var item = new InventoryItem { Id = "item-1", Year = "1990", Brand = "Topps", PlayerOrTitle = "Player", CardNumber = "5" };
        var photos = new[]
        {
            new Photo { Id = "back", FileName = "b.jpg", ViewType = PhotoViewType.Back },
            new Photo { Id = "front", FileName = "a.jpg", ViewType = PhotoViewType.Front }
        };

        var listing = service.CreateDraft(new ListingDraftRequest(item, null, photos, new PriceSnapshot { MarketPrice = 12 }));

        Assert.False(listing.PublishingEnabled);
        Assert.Equal("front", listing.Photos.OrderBy(p => p.SortOrder).First().PhotoId);
        Assert.Contains("#5", listing.Title);
    }

    [Fact]
    public void EbayConnection_BuildsSandboxAuthorizationUrlAndValidatesState()
    {
        var service = new EbayConnectionService();
        var start = service.StartAuthorization(
            new EbayConnectionSettings(MarketplaceEnvironment.Sandbox, "EBAY_US", "client-id", "secret", "runame", []),
            "http://127.0.0.1:49152/callback");

        Assert.StartsWith("https://auth.sandbox.ebay.com/oauth2/authorize?", start.AuthorizationUrl);
        Assert.Contains("sell.inventory", string.Join(' ', start.Scopes));
        Assert.True(service.ValidateOAuthState(start.State, start.State));
        Assert.False(service.ValidateOAuthState(start.State, start.State + "x"));
    }

    [Fact]
    public void SecretRedactor_RemovesKnownSecretValues()
    {
        var redactor = new SecretRedactor();
        var result = redactor.Redact("client_secret=abc123 Authorization: Bearer token-value");

        Assert.DoesNotContain("abc123", result);
        Assert.DoesNotContain("token-value", result);
        Assert.Contains("[REDACTED]", result);
    }

    [Fact]
    public void RootManagement_DetectsEquivalentDuplicateRoots()
    {
        var service = new RootManagementService(new WindowsPathService());
        var existing = new[] { new SourceRoot { Path = @"D:\Inventory\Cards" } };

        var result = service.ValidateRoot(@"D:\Inventory\Cards\", existing);

        Assert.True(result.IsDuplicateEquivalent);
        Assert.Equal(RootAvailability.DuplicateEquivalent, result.Availability);
    }
}

