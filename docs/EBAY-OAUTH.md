# eBay OAuth

Official eBay docs distinguish application tokens from user tokens. Seller-owned data and listing actions require the authorization-code flow with user consent.

References checked on 2026-07-01:

- [eBay Authorization guide](https://developer.ebay.com/develop/guides-v2/authorization)
- [eBay OAuth token types](https://developer.ebay.com/api-docs/static/oauth-token-types.html)
- [eBay Account API](https://developer.ebay.com/develop/api/sell/account_api_v1)
- [eBay Inventory API methods](https://developer.ebay.com/api-docs/sell/inventory/resources/methods)

The local eBay connection service builds Sandbox or Production authorization URLs, validates OAuth state using fixed-time comparison, tracks configured scopes, and reports capabilities.

Minimum local scope set currently requested by the mock/sandbox workflow:

- `https://api.ebay.com/oauth/api_scope`
- `https://api.ebay.com/oauth/api_scope/sell.account.readonly`
- `https://api.ebay.com/oauth/api_scope/sell.inventory.readonly`
- `https://api.ebay.com/oauth/api_scope/sell.inventory`
- `https://api.ebay.com/oauth/api_scope/sell.fulfillment.readonly`

Verify scopes on each official eBay method page before enabling new live operations.
