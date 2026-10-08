/**
 * eBay Hero ecosystem demo.
 *
 * A dependency-free interactive showcase of the entitlement + plugin model. All gating
 * logic mirrors eBayHero.Core (EntitlementEvaluator) so toggling a tier or plugin here
 * shows exactly what the desktop app would unlock.
 */

import { FEATURES, PLUGINS, MOCK_ITEMS, ONBOARDING_STEPS, evaluateEntitlement } from './data.js';

const state = {
  tier: 'free',
  addons: { cardops: true, stamplicity: false, 'ai-suite': false },
  licensed: false,
};

/**
 * Port of EntitlementEvaluator.Evaluate: a feature is granted when its owning addon is
 * enabled and the tier is high enough. The AI Suite addon then grants every AI feature
 * across all plugins.
 */
function computeEntitlement() {
  const addonIds = Object.entries(state.addons).filter(([, on]) => on).map(([id]) => id);
  const features = evaluateEntitlement(state.tier, addonIds);
  return { features, addonIds, source: state.licensed ? 'license' : 'local' };
}

function isPluginActive(plugin) {
  const { addonIds } = computeEntitlement();
  if (plugin.tier === 'core') return true;
  return addonIds.includes(plugin.id);
}

const $ = (selector) => document.querySelector(selector);
const $$ = (selector) => Array.from(document.querySelectorAll(selector));

function el(tag, className, text) {
  const node = document.createElement(tag);
  if (className) node.className = className;
  if (text !== undefined) node.textContent = text;
  return node;
}

function log(target, message, kind = 'info') {
  const line = el('div', `log-line log-${kind}`);
  const time = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });
  line.append(el('span', 'log-time', time), el('span', 'log-text', message));
  target.append(line);
  target.scrollTop = target.scrollHeight;
}

function clearLog(target) {
  target.innerHTML = '';
}

const wait = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

/* ------------------------------------------------------------------ rendering */

function renderTier() {
  $$('[data-tier]').forEach((button) => {
    button.classList.toggle('active', button.dataset.tier === state.tier);
  });
  $('#license-state').textContent = state.licensed ? 'Licensed (Pro)' : 'Local / unlicensed';
}

function renderPlugins() {
  const container = $('#plugin-list');
  container.innerHTML = '';

  PLUGINS.forEach((plugin) => {
    const active = isPluginActive(plugin);
    const card = el('article', `plugin-card ${active ? 'active' : 'locked'}`);

    const head = el('div', 'plugin-head');
    head.append(el('h3', null, plugin.name), el('span', `badge badge-${plugin.tier}`, plugin.tier === 'pro' ? 'Pro addon' : 'Free addon'));
    card.append(head);
    card.append(el('p', 'plugin-tagline', plugin.tagline));

    const toggle = el('label', 'switch');
    const input = el('input');
    input.type = 'checkbox';
    input.checked = Boolean(state.addons[plugin.id]);
    input.dataset.plugin = plugin.id;
    input.addEventListener('change', () => {
      state.addons[plugin.id] = input.checked;
      refresh();
    });
    toggle.append(input, el('span', 'slider'));
    card.append(toggle);

    const caps = el('ul', 'capability-list');
    plugin.capabilities.forEach((key) => {
      const granted = computeEntitlement().features.has(key);
      const item = el('li', granted ? 'granted' : 'denied');
      item.append(el('span', 'cap-mark', granted ? '✓' : '🔒'), el('span', null, FEATURES[key] ? FEATURES[key].name : key));
      caps.append(item);
    });
    card.append(caps);

    card.append(el('p', 'plugin-meta', `hooks: ${plugin.hooks.join(', ')}`));
    container.append(card);
  });
}

function renderFeatures() {
  const { features } = computeEntitlement();
  const container = $('#feature-grid');
  container.innerHTML = '';

  Object.entries(FEATURES).forEach(([key, descriptor]) => {
    const granted = features.has(key);
    const chip = el('span', `feature-chip ${granted ? 'on' : 'off'}`);
    chip.title = descriptor.description;
    chip.textContent = descriptor.name;
    container.append(chip);
  });

  $('#feature-count').textContent = `${features.size} of ${Object.keys(FEATURES).length} features unlocked`;
}

function renderMatrix() {
  const body = $('#matrix-body');
  if (body.dataset.rendered) return;
  body.dataset.rendered = '1';

  Object.entries(FEATURES).forEach(([key, descriptor]) => {
    const row = el('tr');
    const owner = PLUGINS.find((plugin) => plugin.id === descriptor.ownerAddon);
    row.append(
      el('td', null, descriptor.name),
      el('td', null, owner ? owner.name : 'eBay Hero core'),
      el('td', null, descriptor.minTier === 'free' ? '✓' : '—'),
      el('td', null, '✓'),
      el('td', null, descriptor.ownerAddon === 'cardops' || descriptor.ownerAddon === 'stamplicity' ? (descriptor.minTier === 'pro' ? 'Pro addon' : '✓') : '—'),
    );
    body.append(row);
  });
}

function refresh() {
  renderTier();
  renderPlugins();
  renderFeatures();
}


/* --------------------------------------------------------------- simulations */

async function runConnectionTest() {
  const target = $('#connection-log');
  clearLog(target);
  const steps = [
    'Reading configuration (sandbox mode)…',
    'Building OAuth 2.0 authorization URL…',
    'Exchanging refresh token for access token…',
    'GET /sell/inventory/v1/inventory_item?limit=1 … 200 OK',
    'GET /sell/account/v1/fulfillment_policy … 200 OK',
    'Capability check: inventory.write requires publishing enablement (skipped)',
  ];

  for (const step of steps) {
    await wait(320);
    log(target, step, step.includes('200 OK') ? 'ok' : 'info');
  }
  log(target, 'Sandbox connection healthy. Live publishing remains disabled.', 'ok');
}

async function runBulkDrafts() {
  const target = $('#bulk-log');
  clearLog(target);
  const { features } = computeEntitlement();
  const items = MOCK_ITEMS.filter((item) => (item.type === 'card' ? state.addons.cardops : state.addons.stamplicity));

  if (items.length === 0) {
    log(target, 'No plugins enabled — enable CardOps or Stamplicity to generate drafts.', 'warn');
    return;
  }

  log(target, `Generating drafts for ${items.length} inventory item(s)…`, 'info');
  for (const item of items) {
    await wait(280);
    const sku = `EH-${item.id.toUpperCase()}`;
    log(target, `draft ${sku} — "${item.title.slice(0, 46)}…" price $${item.price.toFixed(2)}`, 'ok');
  }

  const entitled = features.has('BulkApiPublishing');
  log(target, entitled
    ? 'Bulk API batch publishing entitled — pushing batch to /sell/inventory/v1/bulk_create_or_replace_inventory_item.'
    : 'Drafts ready for manual review. Bulk API publishing is a Pro feature.', entitled ? 'ok' : 'warn');
}

async function runCardOcr() {
  const target = $('#ocr-log');
  clearLog(target);
  const { features } = computeEntitlement();

  log(target, 'CardOps: preprocessing front image (deskew + threshold)…', 'info');
  await wait(320);

  if (!features.has('AiCardRecognition')) {
    log(target, 'AI card recognition is a premium addon. Falling back to manual detail entry.', 'warn');
    log(target, 'Manual entry schema: Year, Brand, Set, Card Number, Serial, Grade.', 'info');
    return;
  }

  log(target, 'POST /plugins/cardops/recognize … 200 OK', 'ok');
  await wait(260);
  const fields = ['Year: 2023', 'Brand: Topps Chrome', 'Player: Ruben Amaro', 'Card Number: 12', 'Serial: 23/99'];
  fields.forEach((field) => log(target, `field → ${field}`, 'ok'));

  if (features.has('CardGradingDetection')) {
    await wait(240);
    log(target, 'grading detected → PSA 9 (confidence 0.86)', 'ok');
  }
  if (features.has('CardCompPricing')) {
    await wait(240);
    log(target, 'comps → 6 sold comparables, median $15.00 (authorized sold evidence only)', 'ok');
  }
  if (features.has('CardAttributeAutofill')) {
    await wait(200);
    log(target, 'item specifics populated: Sport, Manufacturer, Set, Card Number', 'ok');
  }
}

async function runStampCatalog() {
  const target = $('#stamp-log');
  clearLog(target);
  const { features } = computeEntitlement();

  log(target, 'Stamplicity: staging stamp catalog entry (Scott / Stanley Gibbons)…', 'info');
  await wait(300);
  log(target, 'catalog fields → Scott 594, SG 589, Country US, Denomination 3c', 'ok');

  if (!features.has('AiPhilatelyVisualId')) {
    log(target, 'AI philately visual identification is a premium addon. Add perforation/watermark values manually.', 'warn');
    return;
  }

  log(target, 'POST /plugins/stamplicity/identify … 200 OK', 'ok');
  await wait(260);
  log(target, 'perforations → 11 x 11 (confidence 0.86)', 'ok');
  log(target, 'watermark → Crown CA detected (confidence 0.79)', 'ok');
  log(target, 'centering → H 45% / V 52% (F-VF)', 'ok');

  if (features.has('StampValuationComps')) {
    await wait(240);
    log(target, 'valuation → 4 sold comparables, median $9.50', 'ok');
  }
}


/* ------------------------------------------------------------ onboarding flow */

let wizardStep = 0;

function renderWizard() {
  const step = ONBOARDING_STEPS[wizardStep];
  $('#wizard-step').textContent = `Step ${wizardStep + 1} of ${ONBOARDING_STEPS.length}`;
  $('#wizard-title').textContent = step.title;
  $('#wizard-body').textContent = step.body;

  const checklist = $('#wizard-checklist');
  checklist.innerHTML = '';
  step.checklist.forEach((item, index) => {
    const done = index < wizardStep;
    const li = el('li', done ? 'done' : '');
    li.append(el('span', 'check', done ? '✓' : '○'), el('span', null, item));
    checklist.append(li);
  });

  $('#wizard-progress').style.width = `${((wizardStep + 1) / ONBOARDING_STEPS.length) * 100}%`;
  $('#wizard-back').disabled = wizardStep === 0;
  $('#wizard-next').textContent = wizardStep === ONBOARDING_STEPS.length - 1 ? 'Preview first publish' : 'Next';
}

function wizardNext() {
  if (wizardStep < ONBOARDING_STEPS.length - 1) {
    wizardStep += 1;
    renderWizard();
    return;
  }

  // Final step: jump to the bulk draft simulation with a sensible preset.
  state.tier = 'pro';
  state.addons.cardops = true;
  state.addons.stamplicity = true;
  refresh();
  document.getElementById('simulations').scrollIntoView({ behavior: 'smooth' });
  runBulkDrafts();
}

/* ----------------------------------------------------------------------- init */

function init() {
  $$('[data-tier]').forEach((button) => {
    button.addEventListener('click', () => {
      state.tier = button.dataset.tier;
      refresh();
    });
  });

  $('#license-toggle').addEventListener('change', (event) => {
    state.licensed = event.target.checked;
    if (state.licensed) state.tier = 'pro';
    refresh();
  });

  $('#preset-free').addEventListener('click', () => {
    state.tier = 'free';
    state.licensed = false;
    state.addons = { cardops: true, stamplicity: true, 'ai-suite': false };
    $('#license-toggle').checked = false;
    refresh();
  });

  $('#preset-pro').addEventListener('click', () => {
    state.tier = 'pro';
    state.licensed = true;
    state.addons = { cardops: true, stamplicity: true, 'ai-suite': false };
    $('#license-toggle').checked = true;
    refresh();
  });

  $('#preset-ai').addEventListener('click', () => {
    state.tier = 'free';
    state.licensed = false;
    state.addons = { cardops: true, stamplicity: true, 'ai-suite': true };
    $('#license-toggle').checked = false;
    refresh();
  });

  $('#run-connection').addEventListener('click', runConnectionTest);
  $('#run-bulk').addEventListener('click', runBulkDrafts);
  $('#run-ocr').addEventListener('click', runCardOcr);
  $('#run-stamp').addEventListener('click', runStampCatalog);
  $('#wizard-next').addEventListener('click', wizardNext);
  $('#wizard-back').addEventListener('click', () => {
    wizardStep = Math.max(0, wizardStep - 1);
    renderWizard();
  });

  renderMatrix();
  renderWizard();
  refresh();
}

document.addEventListener('DOMContentLoaded', init);

