using eBayHero.Core.Models;

namespace eBayHero.Core.Ebay;

/// <summary>
/// Strongly-typed eBay configuration. Values are normally supplied by the settings panel
/// or environment variables (see <see cref="FromEnvironment"/>) and never hard-coded, so
/// sandbox and production credentials stay swappable.
/// </summary>
public sealed record EbaySettings
{
    public MarketplaceEnvironment Environment { get; init; } = MarketplaceEnvironment.Sandbox;

    public string MarketplaceId { get; init; } = "EBAY_US";

    public string ClientId { get; init; } = string.Empty;

    public string ClientSecret { get; init; } = string.Empty;

    /// <summary>Long-lived refresh token obtained from the OAuth consent flow.</summary>
    public string RefreshToken { get; init; } = string.Empty;

    /// <summary>eBay RuName (redirect_uri) registered on the developer application.</summary>
    public string RuName { get; init; } = string.Empty;

    /// <summary>Local loopback URI used to capture the authorization code.</summary>
    public string RedirectUri { get; init; } = "http://127.0.0.1:49152/callback";

    public IReadOnlyList<string> Scopes { get; init; } = EbayScopes.MinimalSeller;

    public bool IsSandbox => Environment == MarketplaceEnvironment.Sandbox;

    public string ApiBaseUrl => IsSandbox ? "https://api.sandbox.ebay.com" : "https://api.ebay.com";

    public string AuthBaseUrl => IsSandbox ? "https://auth.sandbox.ebay.com" : "https://auth.ebay.com";

    public bool HasCredentials =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);

    public bool IsFullyConnected => HasCredentials && !string.IsNullOrWhiteSpace(RefreshToken);

    /// <summary>
    /// Reads configuration from environment variables, falling back to the supplied
    /// defaults. Recognised keys: EBAY_ENVIRONMENT, EBAY_MARKETPLACE_ID, EBAY_CLIENT_ID,
    /// EBAY_CLIENT_SECRET, EBAY_REFRESH_TOKEN, EBAY_RUNAME, EBAY_REDIRECT_URI.
    /// </summary>
    public static EbaySettings FromEnvironment(IReadOnlyDictionary<string, string?> environment, EbaySettings? defaults = null)
    {
        var baseline = defaults ?? new EbaySettings();

        string? Read(string key) =>
            environment.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;

        var environmentName = Read("EBAY_ENVIRONMENT");
        var marketplace = environmentName?.ToLowerInvariant() switch
        {
            "production" or "live" or "prod" => MarketplaceEnvironment.Production,
            "sandbox" => MarketplaceEnvironment.Sandbox,
            _ => baseline.Environment
        };

        return baseline with
        {
            Environment = marketplace,
            MarketplaceId = Read("EBAY_MARKETPLACE_ID") ?? baseline.MarketplaceId,
            ClientId = Read("EBAY_CLIENT_ID") ?? baseline.ClientId,
            ClientSecret = Read("EBAY_CLIENT_SECRET") ?? baseline.ClientSecret,
            RefreshToken = Read("EBAY_REFRESH_TOKEN") ?? baseline.RefreshToken,
            RuName = Read("EBAY_RUNAME") ?? baseline.RuName,
            RedirectUri = Read("EBAY_REDIRECT_URI") ?? baseline.RedirectUri
        };
    }
}

/// <summary>Well-known eBay OAuth scopes grouped by capability.</summary>
public static class EbayScopes
{
    public const string SellInventory = "https://api.ebay.com/oauth/api_scope/sell.inventory";
    public const string SellInventoryReadonly = "https://api.ebay.com/oauth/api_scope/sell.inventory.readonly";
    public const string SellAccountReadonly = "https://api.ebay.com/oauth/api_scope/sell.account.readonly";
    public const string SellFulfillmentReadonly = "https://api.ebay.com/oauth/api_scope/sell.fulfillment.readonly";

    /// <summary>The minimum set required to read inventory and push offers.</summary>
    public static readonly IReadOnlyList<string> MinimalSeller =
    [
        SellInventory,
        SellInventoryReadonly,
        SellAccountReadonly,
        SellFulfillmentReadonly
    ];

    public static readonly IReadOnlyList<string> FullSeller =
    [
        SellInventory,
        SellInventoryReadonly,
        SellAccountReadonly,
        SellFulfillmentReadonly,
        "https://api.ebay.com/oauth/api_scope/sell.account",
        "https://api.ebay.com/oauth/api_scope/sell.fulfillment"
    ];
}
