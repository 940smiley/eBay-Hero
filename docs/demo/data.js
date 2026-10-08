/**
 * Demo data model.
 *
 * This mirrors the C# FeatureCatalog / EntitlementEvaluator in eBayHero.Core so the
 * interactive demo computes exactly the same Free/Pro/addon gating rules as the product.
 * Keeping it as plain data (no framework) means the demo runs on GitHub Pages with no
 * build step.
 */

export const TIERS = {
  free: { id: 'free', label: 'Free', rank: 0 },
  pro: { id: 'pro', label: 'Pro', rank: 1 },
  business: { id: 'business', label: 'Business', rank: 2 },
};

export const ADDONS = {
  core: 'ebayhero.core',
  cardops: 'cardops',
  stamplicity: 'stamplicity',
  aiSuite: 'ai-suite',
};

/**
 * Feature catalog: key -> { name, minTier, ownerAddon, description }.
 * `minTier` is 'free' or 'pro'; `ownerAddon` scopes the feature to a plugin (or core).
 */
export const FEATURES = {
  // Core eBay Hero
  ManualListingGeneration: { name: 'Manual listing generation', minTier: 'free', ownerAddon: 'ebayhero.core', description: 'Build a single listing draft by hand.' },
  CsvImportExport: { name: 'CSV import/export', minTier: 'free', ownerAddon: 'ebayhero.core', description: 'Import inventory and export eBay-safe CSVs.' },
  ManualInventorySync: { name: 'Manual inventory sync', minTier: 'free', ownerAddon: 'ebayhero.core', description: 'Push inventory to eBay on demand.' },
  StandardDraftCreation: { name: 'Standard draft creation', minTier: 'free', ownerAddon: 'ebayhero.core', description: 'Create standard listing drafts.' },
  SingleAccountConnection: { name: 'Single account connection', minTier: 'free', ownerAddon: 'ebayhero.core', description: 'Connect one eBay seller account.' },
  ContinuousBackgroundSync: { name: 'Continuous background sync', minTier: 'pro', ownerAddon: 'ebayhero.core', description: 'Keep inventory and orders in sync automatically.' },
  MultiAccountRouting: { name: 'Multi-account routing', minTier: 'pro', ownerAddon: 'ebayhero.core', description: 'Route items to different seller accounts.' },
  AutoRelisting: { name: 'Auto-relisting', minTier: 'pro', ownerAddon: 'ebayhero.core', description: 'Relist ended items on a schedule.' },
  AutomatedRepricing: { name: 'Automated repricing rules', minTier: 'pro', ownerAddon: 'ebayhero.core', description: 'Apply repricing rules from comparable sales.' },
  BulkApiPublishing: { name: 'Bulk API batch publishing', minTier: 'pro', ownerAddon: 'ebayhero.core', description: 'Publish many drafts in a single batch.' },

  // CardOps
  CardInventorySchema: { name: 'Card inventory schema', minTier: 'free', ownerAddon: 'cardops', description: 'Sports/TCG card fields (year, brand, set, number, grade).' },
  ManualCardEntry: { name: 'Manual card detail entry', minTier: 'free', ownerAddon: 'cardops', description: 'Type in card details by hand.' },
  CardDraftExport: { name: 'Card export to eBay drafts', minTier: 'free', ownerAddon: 'cardops', description: 'Turn cards into standard eBay drafts.' },
  AiCardRecognition: { name: 'AI card recognition/OCR', minTier: 'pro', ownerAddon: 'cardops', description: 'Read card fronts/backs and fill fields automatically.' },
  CardGradingDetection: { name: 'Automated grading detection', minTier: 'pro', ownerAddon: 'cardops', description: 'Detect grading company and grade from labels.' },
  CardCompPricing: { name: 'Automated comp pricing', minTier: 'pro', ownerAddon: 'cardops', description: 'Price from authorized sold-comparable data.' },
  CardAttributeAutofill: { name: 'Automated attribute population', minTier: 'pro', ownerAddon: 'cardops', description: 'Populate item specifics from recognition output.' },


  // Stamplicity
  StampCatalogSchema: { name: 'Stamp catalog schema', minTier: 'free', ownerAddon: 'stamplicity', description: 'Scott/Stanley Gibbons catalog fields.' },
  ManualStampImageAttach: { name: 'Manual image attachment', minTier: 'free', ownerAddon: 'stamplicity', description: 'Attach and order stamp photos.' },
  StampDraftStaging: { name: 'Standard eBay draft staging', minTier: 'free', ownerAddon: 'stamplicity', description: 'Stage stamp drafts for review.' },
  AiPhilatelyVisualId: { name: 'AI philately visual identification', minTier: 'pro', ownerAddon: 'stamplicity', description: 'Perforation counts, watermark tagging, centering estimation.' },
  StampValuationComps: { name: 'Automated stamp valuation comps', minTier: 'pro', ownerAddon: 'stamplicity', description: 'Value stamps from comparable sales.' },
  StampAutoListing: { name: 'Stamp auto-listing', minTier: 'pro', ownerAddon: 'stamplicity', description: 'Publish fully-mapped stamp listings.' },

  // All-in-One AI Suite
  AiVisionSuite: { name: 'All-in-One AI Suite', minTier: 'pro', ownerAddon: 'ai-suite', description: 'Unlocks AI vision, OCR, and automated valuation across every active plugin.' },
};

/** Features the AI Suite unlocks across all plugins when it is active. */
export const AI_SUITE_UNLOCKED = [
  'AiVisionSuite',
  'AiCardRecognition',
  'CardGradingDetection',
  'CardCompPricing',
  'CardAttributeAutofill',
  'AiPhilatelyVisualId',
  'StampValuationComps',
  'StampAutoListing',
];

/**
 * Port of eBayHero.Core EntitlementEvaluator.Evaluate. A feature is granted when its
 * owning addon is enabled and the tier is high enough; the AI Suite addon then grants
 * every AI feature across all plugins.
 */
export function evaluateEntitlement(tier, addonIds) {
  const tierRank = TIERS[tier].rank;
  const features = new Set();

  for (const [key, descriptor] of Object.entries(FEATURES)) {
    const isCore = descriptor.ownerAddon === 'ebayhero.core';
    if (!isCore && !addonIds.includes(descriptor.ownerAddon)) continue;
    const tierOk = descriptor.minTier === 'free' || tierRank >= TIERS[descriptor.minTier].rank;
    if (tierOk) features.add(key);
  }

  if (addonIds.includes('ai-suite')) {
    AI_SUITE_UNLOCKED.forEach((key) => features.add(key));
  }

  return features;
}

export const PLUGINS = [
  {
    id: 'cardops',
    name: 'CardOps',
    tier: 'free',
    tagline: 'Trading-card inventory routines',
    hooks: ['inventory.item.created', 'listing.draft.created'],
    routes: ['POST /plugins/cardops/recognize', 'POST /plugins/cardops/price', 'POST /plugins/cardops/draft'],
    capabilities: ['CardInventorySchema', 'ManualCardEntry', 'CardDraftExport', 'AiCardRecognition', 'CardGradingDetection', 'CardCompPricing', 'CardAttributeAutofill'],
  },
  {
    id: 'stamplicity',
    name: 'Stamplicity',
    tier: 'free',
    tagline: 'Philately inventory workflows',
    hooks: ['inventory.item.created', 'listing.draft.created'],
    routes: ['POST /plugins/stamplicity/identify', 'POST /plugins/stamplicity/valuation'],
    capabilities: ['StampCatalogSchema', 'ManualStampImageAttach', 'StampDraftStaging', 'AiPhilatelyVisualId', 'StampValuationComps', 'StampAutoListing'],
  },
  {
    id: 'ai-suite',
    name: 'All-in-One AI Suite',
    tier: 'pro',
    tagline: 'Master AI unlock across every plugin',
    hooks: ['listing.draft.created'],
    routes: ['POST /plugins/ai-suite/analyze'],
    capabilities: ['AiVisionSuite', 'AiCardRecognition', 'CardCompPricing'],
  },
];

export const MOCK_ITEMS = [
  { id: 'card-1', type: 'card', title: '2023 Topps Chrome Ruben Amaro #12 23/99', price: 15.0 },
  { id: 'card-2', type: 'card', title: '1989 Upper Deck Ken Griffey Jr. #1 RC', price: 42.5 },
  { id: 'card-3', type: 'card', title: '2018 Panini Prizm Luka Doncic #280 RC', price: 88.0 },
  { id: 'stamp-1', type: 'stamp', title: 'US Scott 594 Crown CA watermark', price: 9.5 },
  { id: 'stamp-2', type: 'stamp', title: 'GB Stanley Gibbons SG 589 11x11 perforations', price: 12.0 },
];

export const ONBOARDING_STEPS = [
  {
    title: 'Create an eBay Developer account',
    body: 'Register at developer.ebay.com, create an application keyset, and note the App ID (Client ID), Cert ID (Client Secret), and RuName.',
    checklist: ['Developer account created', 'Sandbox keyset generated', 'RuName configured'],
  },
  {
    title: 'Enter your credentials',
    body: 'Paste Client ID, Client Secret, Refresh Token, and RuName into the settings panel. Keep sandbox enabled until you have verified a draft end to end.',
    checklist: ['Client ID saved', 'Client Secret stored securely', 'Environment set to Sandbox'],
  },
  {
    title: 'Enable plugins',
    body: 'Turn on the verticals you sell in. CardOps and Stamplicity are free to start; the All-in-One AI Suite unlocks AI vision, OCR, and valuation everywhere.',
    checklist: ['CardOps enabled', 'Stamplicity enabled', 'AI Suite decision made'],
  },
  {
    title: 'Preview your first bulk publish',
    body: 'Generate drafts from your inventory, review the field mapping, then publish as a batch (Pro) or export for manual review (Free).',
    checklist: ['Drafts generated', 'Field mapping reviewed', 'Publish path chosen'],
  },
];
