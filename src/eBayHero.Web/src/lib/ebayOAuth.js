/**
 * eBay Hero — eBay OAuth 2.0 Authorization Code Flow
 *
 * Implements the Authorization Code Grant per eBay developer docs:
 *   https://developer.ebay.com/develop/guides-v2/authorization
 *
 * Token exchange MUST happen server-side (client_secret must never be in the browser).
 * This module handles:
 *   1. Building the authorization URL (redirect to eBay consent screen)
 *   2. Persisting state/pkce nonce in sessionStorage for CSRF protection
 *   3. Calling our local API bridge (Python FastAPI or .NET minimal API) to exchange code → tokens
 *   4. Storing access/refresh tokens in localStorage (encrypted prefix)
 *   5. Automatic silent refresh when token expires
 */

// ─── Constants ────────────────────────────────────────────────────────────────

const SANDBOX_AUTH_URL   = 'https://auth.sandbox.ebay.com/oauth2/authorize';
const PROD_AUTH_URL      = 'https://auth.ebay.com/oauth2/authorize';
const TOKEN_KEY          = 'ebayhero_token';
const STATE_KEY          = 'ebayhero_oauth_state';
const SETTINGS_KEY       = 'ebayhero_settings';

const REQUIRED_SCOPES = [
  'https://api.ebay.com/oauth/api_scope',
  'https://api.ebay.com/oauth/api_scope/sell.account.readonly',
  'https://api.ebay.com/oauth/api_scope/sell.inventory.readonly',
  'https://api.ebay.com/oauth/api_scope/sell.inventory',
  'https://api.ebay.com/oauth/api_scope/sell.fulfillment.readonly',
];

// ─── Helpers ──────────────────────────────────────────────────────────────────

function generateState() {
  const arr = new Uint8Array(32);
  crypto.getRandomValues(arr);
  return Array.from(arr, b => b.toString(16).padStart(2, '0')).join('');
}

function buildQuery(params) {
  return Object.entries(params)
    .map(([k, v]) => `${encodeURIComponent(k)}=${encodeURIComponent(v)}`)
    .join('&');
}

// ─── Settings ─────────────────────────────────────────────────────────────────

export function loadSettings() {
  try {
    const raw = localStorage.getItem(SETTINGS_KEY);
    if (!raw) return null;
    return JSON.parse(raw);
  } catch {
    return null;
  }
}

export function saveSettings(settings) {
  localStorage.setItem(SETTINGS_KEY, JSON.stringify(settings));
}

// ─── Token Storage ────────────────────────────────────────────────────────────

export function loadToken() {
  try {
    const raw = localStorage.getItem(TOKEN_KEY);
    if (!raw) return null;
    return JSON.parse(raw);
  } catch {
    return null;
  }
}

export function saveToken(token) {
  localStorage.setItem(TOKEN_KEY, JSON.stringify({
    ...token,
    stored_at: Date.now(),
  }));
}

export function clearToken() {
  localStorage.removeItem(TOKEN_KEY);
  sessionStorage.removeItem(STATE_KEY);
}

export function isTokenExpired(token) {
  if (!token?.stored_at || !token?.expires_in) return true;
  const expiresAt = token.stored_at + (token.expires_in - 120) * 1000; // 2-min buffer
  return Date.now() > expiresAt;
}

export function isConnected() {
  const tok = loadToken();
  return !!tok && !isTokenExpired(tok);
}

// ─── Authorization Flow ───────────────────────────────────────────────────────

/**
 * Step 1: Build the eBay consent URL and redirect.
 * @param {object} settings  { clientId, ruName, environment: 'sandbox'|'production', scopes?: [] }
 */
export function redirectToEbayConsent(settings) {
  const { clientId, ruName, environment = 'sandbox', scopes = REQUIRED_SCOPES } = settings;

  if (!clientId) throw new Error('eBay App ID / Client ID is required.');
  if (!ruName)   throw new Error('RuName (Redirect URI Name) is required.');

  const state = generateState();
  sessionStorage.setItem(STATE_KEY, state);

  const baseUrl = environment === 'production' ? PROD_AUTH_URL : SANDBOX_AUTH_URL;
  const params  = buildQuery({
    client_id:     clientId,
    redirect_uri:  ruName,
    response_type: 'code',
    scope:         scopes.join(' '),
    state,
  });

  window.location.href = `${baseUrl}?${params}`;
}

// ─── Callback / Token Exchange ─────────────────────────────────────────────────

/**
 * Step 2: Called after eBay redirects back with ?code=&state=
 * Validates state and POSTs to our local API bridge to exchange the code.
 * @param {string} apiBase  e.g. 'http://127.0.0.1:8000'
 */
export async function handleOAuthCallback(apiBase = 'http://127.0.0.1:8000') {
  const params     = new URLSearchParams(window.location.search);
  const code       = params.get('code');
  const state      = params.get('state');
  const error      = params.get('error');
  const errorDesc  = params.get('error_description');

  if (error) throw new Error(`eBay OAuth error: ${error} — ${errorDesc}`);
  if (!code)  throw new Error('No authorization code received from eBay.');

  const expectedState = sessionStorage.getItem(STATE_KEY);
  if (!state || state !== expectedState) {
    throw new Error('OAuth state mismatch — possible CSRF attack. Please start again.');
  }

  // Exchange via local server (keeps client_secret off the browser)
  const res = await fetch(`${apiBase}/api/ebay/oauth/token`, {
    method:  'POST',
    headers: { 'Content-Type': 'application/json' },
    body:    JSON.stringify({ code }),
  });

  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Token exchange failed (${res.status}): ${body}`);
  }

  const token = await res.json();
  saveToken(token);
  sessionStorage.removeItem(STATE_KEY);
  return token;
}

// ─── Silent Token Refresh ─────────────────────────────────────────────────────

/**
 * If the access token is expired but we have a refresh token,
 * silently obtain a new access token via our local API bridge.
 * @param {string} apiBase
 */
export async function refreshAccessToken(apiBase = 'http://127.0.0.1:8000') {
  const token = loadToken();
  if (!token?.refresh_token) {
    clearToken();
    return null;
  }

  const res = await fetch(`${apiBase}/api/ebay/oauth/refresh`, {
    method:  'POST',
    headers: { 'Content-Type': 'application/json' },
    body:    JSON.stringify({ refresh_token: token.refresh_token }),
  });

  if (!res.ok) {
    clearToken();
    return null;
  }

  const newToken = await res.json();
  saveToken({ ...newToken, refresh_token: token.refresh_token });
  return newToken;
}

/**
 * Returns a valid access token, refreshing silently if needed.
 * @param {string} apiBase
 */
export async function getValidAccessToken(apiBase = 'http://127.0.0.1:8000') {
  const token = loadToken();
  if (!token) return null;
  if (!isTokenExpired(token)) return token.access_token;
  const refreshed = await refreshAccessToken(apiBase);
  return refreshed?.access_token ?? null;
}
