# Monetization Plan

## Free/local tier

Implemented or realistic for first release: manual inventory, local image intake, basic OCR, listing drafts, CSV export, demo/safe mode, local SQLite database, diagnostics, and CardOps dry-run migration.

## Pro tier

Add after the Windows vertical slice stabilizes: higher OCR volume, advanced image processing, AI-assisted identification, pricing-provider connectors, bulk listing generation, listing audits, lot recommendations, advanced automation, and priority updates.

## Business tier

Requires cloud or team infrastructure: multiple stores, multiple users, shared inventory, cloud sync, scheduled automation, advanced analytics, higher provider limits, and support SLAs.

## Cost and privacy split

- Fully local: inventory, image intake, SQLite, basic OCR, CSV export, diagnostics.
- Recurring API costs: cloud AI identification, premium pricing data, marketplace sync.
- Cloud infrastructure: multi-user sync, hosted backups, billing, analytics.
- eBay approvals: OAuth scopes, seller account access, sandbox/live app review where required.

## Launch pricing test

- Free: local-only usage with limited listing drafts.
- Pro: $19-$39/month or $199-$399/year.
- Business: $79-$199/month depending on stores, users, and provider limits.
- Services: paid migration/setup for existing CardOps or Inventory Photo Ops data.

Do not implement payment processing until the Windows release is stable. Keep licensing behind feature flags and service interfaces so billing can be added without rewriting core workflows.

