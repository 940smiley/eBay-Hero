using eBayHero.Core.Ebay;
using eBayHero.Core.Entitlements;
using eBayHero.Core.Models;

namespace eBayHero.Plugins.Tests;

public sealed class EbayEngineTests
{
    private sealed class FakeTransport(EbayApiSurface surface, params EbayApiResponse[] responses) : IEbayTransport
    {
        public EbayApiSurface Surface => surface;
        public int Calls { get; private set; }
        public EbayApiCall? LastCall { get; private set; }

        public Task<EbayApiResponse> SendAsync(EbayApiCall call, CancellationToken cancellationToken = default)
        {
            LastCall = call;
            var response = responses[Math.Min(Calls, responses.Length - 1)];
            Calls++;
            return Task.FromResult(response with { Surface = surface });
        }
    }

    private static EbayFieldMappingProfile Profile() => new(
        "default",
        "Default profile",
        new ListingTemplate("tpl", "Default", "{Year} {Brand} {Player} #{CardNumber}", "Card: {Player}", "261328", "USED_GOOD", new Dictionary<string, string> { ["Sport"] = "Baseball" }),
        new ShippingPolicyProfile("ship", "Standard", "FULFILL-1", 4.99m, "USPSFirstClass", 3),
        new ReturnPolicyProfile("ret", "30 day", "RETURN-1", true, 30, "BUYER"),
        new PaymentPolicyProfile("pay", "Default", "PAY-1"),
        new Dictionary<string, string>
        {
            [nameof(InventoryItem.CardNumber)] = "Card Number",
            [nameof(InventoryItem.Year)] = "Year"
        });

    [Fact]
    public void AuthorizationUrl_UsesSandboxHostForSandbox()
    {
        var settings = new EbaySettings { Environment = MarketplaceEnvironment.Sandbox, ClientId = "cid", RuName = "runame" };

        var url = EbayOAuthFlow.BuildAuthorizationUrl(settings, "state-1");

        Assert.StartsWith("https://auth.sandbox.ebay.com/oauth2/authorize?", url);
        Assert.Contains("client_id=cid", url);
        Assert.Contains("state=state-1", url);
    }

    [Fact]
    public void AuthorizationUrl_UsesProductionHostForProduction()
    {
        var settings = new EbaySettings { Environment = MarketplaceEnvironment.Production, ClientId = "cid", RuName = "runame" };

        Assert.StartsWith("https://auth.ebay.com/oauth2/authorize?", EbayOAuthFlow.BuildAuthorizationUrl(settings, "s"));
    }

    [Fact]
    public void TokenResponse_ParsesExpiryAndReusesRefreshToken()
    {
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var tokens = EbayOAuthFlow.ParseTokenResponse("""{ "access_token": "at", "expires_in": 3600, "scope": "sell.inventory" }""", now, previousRefreshToken: "rt");

        Assert.Equal("at", tokens.AccessToken);
        Assert.Equal("rt", tokens.RefreshToken);
        Assert.Equal(now.AddSeconds(3600), tokens.ExpiresUtc);
        Assert.Contains("sell.inventory", tokens.Scopes);
    }

    [Fact]
    public void BasicAuthorizationHeader_EncodesClientCredentials()
    {
        var settings = new EbaySettings { ClientId = "user", ClientSecret = "pass" };

        var header = EbayOAuthFlow.BuildBasicAuthorizationHeader(settings);

        Assert.Equal("Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("user:pass")), header);
    }

    [Fact]
    public void Settings_FromEnvironment_ReadsValues()
    {
        var env = new Dictionary<string, string?>
        {
            ["EBAY_ENVIRONMENT"] = "production",
            ["EBAY_CLIENT_ID"] = "abc",
            ["EBAY_CLIENT_SECRET"] = "secret",
            ["EBAY_MARKETPLACE_ID"] = "EBAY_GB"
        };

        var settings = EbaySettings.FromEnvironment(env);

        Assert.Equal(MarketplaceEnvironment.Production, settings.Environment);
        Assert.Equal("abc", settings.ClientId);
        Assert.Equal("EBAY_GB", settings.MarketplaceId);
        Assert.True(settings.HasCredentials);
    }

    [Fact]
    public void ListingMapper_RendersTitleAndAspects()
    {
        var item = new InventoryItem { Id = "item-1", Year = "2023", Brand = "Topps Chrome", PlayerOrTitle = "Ruben Amaro", CardNumber = "12" };

        var draft = new EbayListingMapper().Map(item, [], null, Profile());

        Assert.Contains("#12", draft.Offer.Title);
        Assert.True(draft.Offer.Title.Length <= 80);
        Assert.Equal("12", draft.InventoryItem.Aspects["Card Number"]);
        Assert.Equal("Baseball", draft.InventoryItem.Aspects["Sport"]);
        Assert.Equal(0.99m, draft.Offer.Price);
    }

    [Fact]
    public void BulkDraftGenerator_ReportsEntitlementAndWarnings()
    {
        var item = new InventoryItem { Id = "item-1", Name = "Card", Year = "2020", Brand = "Topps", CardNumber = "5" };
        var free = new EntitlementService(new EntitlementOptions());
        var pro = new EntitlementService(new EntitlementOptions { Tier = EntitlementTier.Pro });
        var request = new BulkDraftRequest([item], new Dictionary<string, IReadOnlyList<Photo>>(), new Dictionary<string, PriceSnapshot>(), Profile());

        var freeResult = new BulkDraftGenerator(new EbayListingMapper(), free).Generate(request);
        var proResult = new BulkDraftGenerator(new EbayListingMapper(), pro).Generate(request);

        Assert.False(freeResult.BulkPublishingEntitled);
        Assert.True(proResult.BulkPublishingEntitled);
        Assert.Single(freeResult.Drafts);
        Assert.NotEmpty(freeResult.Warnings);
    }

    [Fact]
    public async Task Router_RetriesTransientFailures()
    {
        var transport = new FakeTransport(EbayApiSurface.Rest,
            new EbayApiResponse(false, 500, string.Empty, EbayApiSurface.Rest),
            new EbayApiResponse(true, 200, "{}", EbayApiSurface.Rest));
        var router = new EbayApiRouter(new EntitlementService(new EntitlementOptions { Tier = EntitlementTier.Pro }), [transport], delay: (_, _) => Task.CompletedTask);

        var response = await router.SendAsync(new EbayApiCall("getInventoryItems", "GET", "/sell/inventory/v1/inventory_item"));

        Assert.True(response.Success);
        Assert.Equal(2, response.Attempts);
        Assert.Equal(2, transport.Calls);
    }

    [Fact]
    public async Task Router_FallsBackToTradingWhenRestIsUnsupported()
    {
        var rest = new FakeTransport(EbayApiSurface.Rest, new EbayApiResponse(false, 404, string.Empty, EbayApiSurface.Rest));
        var trading = new FakeTransport(EbayApiSurface.Trading, new EbayApiResponse(true, 200, "<ok/>", EbayApiSurface.Trading));
        var router = new EbayApiRouter(new EntitlementService(new EntitlementOptions { Tier = EntitlementTier.Pro }), [rest, trading], delay: (_, _) => Task.CompletedTask);

        var response = await router.SendAsync(new EbayApiCall("addItem", "POST", "/ws/api.dll"));

        Assert.True(response.Success);
        Assert.Equal(EbayApiSurface.Trading, response.Surface);
    }

    [Fact]
    public async Task Router_RefusesPremiumCallWithoutEntitlement()
    {
        var transport = new FakeTransport(EbayApiSurface.Rest, new EbayApiResponse(true, 200, "{}", EbayApiSurface.Rest));
        var router = new EbayApiRouter(new EntitlementService(new EntitlementOptions()), [transport], delay: (_, _) => Task.CompletedTask);

        var response = await router.SendAsync(new EbayApiCall("publishOffer", "POST", "/sell/inventory/v1/offer/publish", RequiredFeature: FeatureKey.BulkApiPublishing));

        Assert.False(response.Success);
        Assert.Equal(402, response.StatusCode);
        Assert.Equal(0, transport.Calls);
    }
}
