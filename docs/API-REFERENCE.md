# API & workflow reference

## eBay OAuth 2.0

| Step | Detail |
|---|---|
| Authorization URL | `{authBase}/oauth2/authorize?client_id&redirect_uri&response_type=code&scope&state` |
| Sandbox auth host | `https://auth.sandbox.ebay.com` |
| Production auth host | `https://auth.ebay.com` |
| Token endpoint | `{apiBase}/identity/v1/oauth2/token` |
| Token auth | HTTP Basic, `base64(clientId:clientSecret)` |
| Code exchange body | `grant_type=authorization_code&code=...` |
| Refresh body | `grant_type=refresh_token&refresh_token=...` |

Implemented in `EbayOAuthFlow` (pure) and `HttpEbayTokenClient` (HTTP).

### Scopes

| Constant | Scope | Used for |
|---|---|---|
| `EbayScopes.SellInventory` | `.../sell.inventory` | create/replace inventory items, offers, publish |
| `EbayScopes.SellInventoryReadonly` | `.../sell.inventory.readonly` | read inventory |
| `EbayScopes.SellAccountReadonly` | `.../sell.account.readonly` | read business policies |
| `EbayScopes.SellFulfillmentReadonly` | `.../sell.fulfillment.readonly` | read orders |

`EbayScopes.MinimalSeller` is the default set; `FullSeller` adds the write scopes.

## Supported eBay surfaces

| Surface | Used for | Transport |
|---|---|---|
| Inventory REST (`/sell/inventory/v1/...`) | inventory items, offers, publish | `IEbayTransport` (REST) |
| Account REST (`/sell/account/v1/...`) | fulfillment / return / payment policies | REST |
| Fulfillment REST (`/sell/fulfillment/v1/...`) | orders | REST |
| Trading API (`/ws/api.dll`) | operations REST does not expose | `IEbayTransport` (Trading) |

`EbayApiRouter` sends to the preferred surface, retries transient failures (429/5xx) with
exponential backoff, and falls back to Trading when REST returns 404/405/501.

## Configuration parameters

| Setting | Env var | Notes |
|---|---|---|
| Environment | `EBAY_ENVIRONMENT` | `sandbox` \| `production` |
| Marketplace | `EBAY_MARKETPLACE_ID` | default `EBAY_US` |
| Client ID | `EBAY_CLIENT_ID` | App ID |
| Client Secret | `EBAY_CLIENT_SECRET` | Cert ID |
| Refresh Token | `EBAY_REFRESH_TOKEN` | from consent flow |
| RuName | `EBAY_RUNAME` | redirect_uri |
| Redirect URI | `EBAY_REDIRECT_URI` | local loopback |
| License key | `EH_LICENSE_KEY` | `EH1.<payload>.<signature>` |
| Addons | `EH_ADDONS` | comma-separated ids |

Derived: `ApiBaseUrl`, `AuthBaseUrl`, `IsSandbox`, `HasCredentials`, `IsFullyConnected`.

## Field mapping

`EbayFieldMappingProfile` bundles:

- `ListingTemplate` — title/description templates, category, condition, default item specifics.
- `ShippingPolicyProfile` — fulfillment policy id, shipping cost, service code, handling time.
- `ReturnPolicyProfile` — return policy id, window, who pays return shipping.
- `PaymentPolicyProfile` — payment policy id.
- `FieldMap` — internal field name → eBay aspect name.

Templates support `{Year} {Brand} {Player} {CardNumber} {Set} {Serial} {Team} {Grade}`
placeholders. Titles are clamped to 80 characters on a word boundary.

`EbayListingMapper.Map(item, photos, price, profile)` produces an `EbayDraftPayload`
containing an `EbayInventoryItemPayload` (sku, condition, aspects, images) and an
`EbayOfferPayload` (marketplace, category, format, price, policies, quantity, images).

## Entitlement API

```csharp
public interface IEntitlementService
{
    EntitlementSnapshot Snapshot { get; }
    bool HasFeature(FeatureKey key);
    bool IsAddonActive(string addonId);
    bool TryApplyLicense(string licenseKey, out string message);
    void ApplyLicense(string licenseKey);
    void SetTier(EntitlementTier tier, IEnumerable<string>? addons = null);
    void ClearLicense();
    event EventHandler? Changed;
}
```

## License key format

```
EH1.<base64url(json payload)>.<base64url(hmac-sha256(payload))>

payload = { "sub": "buyer@example.com", "tier": "pro", "addons": ["cardops"], "exp": 1798761600 }
```

Mint a key (tests/CLI only):

```csharp
var key = LicenseKeySigner.Sign(secret, new LicensePayload("buyer@example.com", EntitlementTier.Pro, ["cardops"], null));
```

## Plugin API

```csharp
var registry = new PluginRegistry(entitlements);
registry.Register(new CardOpsPlugin());
var host = new PluginHost(registry, entitlements, onPluginError: (id, ex) => Log(id, ex));

await host.InitializeAsync();
var outcomes = await host.DispatchHookAsync("inventory.item.created", payload);
var result   = await host.InvokeRouteAsync("POST", "/plugins/cardops/recognize", body: json);
```

### First-party routes

| Method | Path | Required feature |
|---|---|---|
| POST | `/plugins/cardops/recognize` | `AiCardRecognition` |
| POST | `/plugins/cardops/price` | `CardCompPricing` |
| POST | `/plugins/cardops/draft` | `CardDraftExport` |
| POST | `/plugins/stamplicity/identify` | `AiPhilatelyVisualId` |
| POST | `/plugins/stamplicity/valuation` | `StampValuationComps` |
| POST | `/plugins/ai-suite/analyze` | `AiVisionSuite` |

### Route status codes

| Code | Meaning |
|---|---|
| 200 | handled |
| 402 | mounted but the required feature is not entitled |
| 404 | plugin inactive or route unknown |
| 500 | plugin handler threw (isolated) |

## Hooks

| Hook | Payload keys | When |
|---|---|---|
| `inventory.item.created` | `sportOrGame`, item fields | after an item is persisted |
| `listing.draft.created` | draft fields | before draft validation |
