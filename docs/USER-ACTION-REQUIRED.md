# User Action Required

| Needed | Why | Where to obtain | Where to enter | Can development continue? | Mock/Sandbox available? |
| --- | --- | --- | --- | --- | --- |
| eBay developer app credentials | OAuth and seller APIs | eBay Developer Program | Local secret/config flow, not git | Yes | Mock and dry-run workflows |
| eBay OAuth authorization | Connect a seller account | eBay consent screen | App connection flow | Yes | Sandbox first |
| Code-signing certificate | Public Windows installer trust | Certificate authority | Release pipeline | Yes | Unsigned dev builds |
| Product name/logo approval | Installer, app title, website | Owner decision | Source/resources/docs | Yes | Provisional `eBay Assistance` |
| Privacy-policy business details | Public release compliance | Business/legal owner | `docs/PRIVACY.md` and website | Yes | Draft exists |
| Support email | User support and store listings | Owner decision | Docs/release metadata | Yes | Temporary local diagnostics |
| Apple Team ID/signing identity | iOS build/release | Apple Developer | Xcode/project config | Yes | Not buildable on Windows |
| Android signing key | Android public release | Android/Play Console tooling | Android release config | Yes | Debug scaffold later |
| Payment provider account | Paid plans | Stripe/PayPal/etc. | Future billing config | Yes | No payment code yet |
| External pricing/API subscriptions | Paid pricing data and AI | Provider portals | Provider adapters | Yes | Manual/mock values |

