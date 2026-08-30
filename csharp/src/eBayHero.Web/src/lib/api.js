/**
 * eBay Hero — API client for local backend bridge
 *
 * The local FastAPI/ASP.NET backend runs on 127.0.0.1 and proxies:
 *   - eBay token exchange (keeps client_secret off browser)
 *   - SQLite inventory read/write
 *   - OCR / image analysis jobs
 *   - Listing draft creation
 */

import { getValidAccessToken } from './ebayOAuth.js';

export const API_BASE = import.meta.env.VITE_API_BASE ?? 'http://127.0.0.1:8000';

// ─── Generic fetch wrapper ─────────────────────────────────────────────────────

async function apiFetch(path, options = {}) {
  const url = `${API_BASE}${path}`;
  const res = await fetch(url, {
    headers: { 'Content-Type': 'application/json', ...options.headers },
    ...options,
  });
  if (!res.ok) {
    const body = await res.text().catch(() => '');
    throw new Error(`API ${options.method ?? 'GET'} ${path} → ${res.status}: ${body}`);
  }
  const ct = res.headers.get('Content-Type') ?? '';
  return ct.includes('application/json') ? res.json() : res.text();
}

// ─── Health ───────────────────────────────────────────────────────────────────

export async function checkHealth() {
  return apiFetch('/health');
}

// ─── eBay OAuth ───────────────────────────────────────────────────────────────

export async function exchangeOAuthCode(code) {
  return apiFetch('/api/ebay/oauth/token', {
    method: 'POST',
    body: JSON.stringify({ code }),
  });
}

export async function refreshOAuthToken(refreshToken) {
  return apiFetch('/api/ebay/oauth/refresh', {
    method: 'POST',
    body: JSON.stringify({ refresh_token: refreshToken }),
  });
}

export async function getEbayConnectionStatus() {
  return apiFetch('/api/ebay/connection/status');
}

export async function disconnectEbay() {
  return apiFetch('/api/ebay/connection/disconnect', { method: 'POST' });
}

// ─── Inventory ────────────────────────────────────────────────────────────────

export async function fetchInventory(params = {}) {
  const qs = new URLSearchParams(params).toString();
  return apiFetch(`/api/inventory${qs ? `?${qs}` : ''}`);
}

export async function fetchInventoryItem(id) {
  return apiFetch(`/api/inventory/${id}`);
}

export async function updateInventoryItem(id, payload) {
  return apiFetch(`/api/inventory/${id}`, {
    method: 'PATCH',
    body: JSON.stringify(payload),
  });
}

export async function deleteInventoryItem(id) {
  return apiFetch(`/api/inventory/${id}`, { method: 'DELETE' });
}

// ─── Image Ingest ─────────────────────────────────────────────────────────────

export async function ingestImages(formData) {
  const res = await fetch(`${API_BASE}/api/ingest/upload`, {
    method: 'POST',
    body: formData, // multipart, no Content-Type header override
  });
  if (!res.ok) throw new Error(`Ingest upload failed: ${res.status}`);
  return res.json();
}

export async function fetchIngestQueue() {
  return apiFetch('/api/ingest/queue');
}

export async function approveIngestItem(id, payload) {
  return apiFetch(`/api/ingest/${id}/approve`, {
    method: 'POST',
    body: JSON.stringify(payload),
  });
}

export async function rejectIngestItem(id) {
  return apiFetch(`/api/ingest/${id}/reject`, { method: 'POST' });
}

// ─── Image Editor ─────────────────────────────────────────────────────────────

export async function requestBackgroundRemoval(photoId) {
  return apiFetch(`/api/image/${photoId}/remove-background`, { method: 'POST' });
}

export async function rotateImage(photoId, degrees) {
  return apiFetch(`/api/image/${photoId}/rotate`, {
    method: 'POST',
    body: JSON.stringify({ degrees }),
  });
}

export async function cropImage(photoId, rect) {
  return apiFetch(`/api/image/${photoId}/crop`, {
    method: 'POST',
    body: JSON.stringify(rect),
  });
}

// ─── eBay Listing ─────────────────────────────────────────────────────────────

export async function createListingDraft(itemId) {
  return apiFetch(`/api/listing/${itemId}/draft`, { method: 'POST' });
}

export async function optimizeListing(itemId) {
  return apiFetch(`/api/listing/${itemId}/optimize`, { method: 'POST' });
}

export async function publishListing(itemId) {
  return apiFetch(`/api/listing/${itemId}/publish`, { method: 'POST' });
}

export async function fetchActiveListings() {
  return apiFetch('/api/listing/active');
}

// ─── Lot Builder ──────────────────────────────────────────────────────────────

export async function createLot(itemIds, lotName) {
  return apiFetch('/api/lot', {
    method: 'POST',
    body: JSON.stringify({ item_ids: itemIds, name: lotName }),
  });
}

export async function fetchLots() {
  return apiFetch('/api/lot');
}

// ─── eBay Categories ─────────────────────────────────────────────────────────

export async function fetchEbayCategories(parentId = 0) {
  return apiFetch(`/api/ebay/categories?parent_id=${parentId}`);
}

export async function searchEbayCategories(query) {
  return apiFetch(`/api/ebay/categories/search?q=${encodeURIComponent(query)}`);
}

// ─── Seed Sorting ─────────────────────────────────────────────────────────────

export async function runSeedSort(seedIds) {
  return apiFetch('/api/ai/seed-sort', {
    method: 'POST',
    body: JSON.stringify({ seed_ids: seedIds }),
  });
}

export async function addSeed(formData) {
  const res = await fetch(`${API_BASE}/api/ai/seeds`, {
    method: 'POST',
    body: formData,
  });
  if (!res.ok) throw new Error(`Add seed failed: ${res.status}`);
  return res.json();
}

export async function fetchSeeds() {
  return apiFetch('/api/ai/seeds');
}

// ─── Social Media ─────────────────────────────────────────────────────────────

export async function fetchSocialAccounts() {
  return apiFetch('/api/social/accounts');
}

export async function saveSocialAccount(platform, config) {
  return apiFetch('/api/social/accounts', {
    method: 'POST',
    body: JSON.stringify({ platform, ...config }),
  });
}

export async function shareToSocial(itemId, platforms, caption) {
  return apiFetch('/api/social/share', {
    method: 'POST',
    body: JSON.stringify({ item_id: itemId, platforms, caption }),
  });
}

// ─── Diagnostics ─────────────────────────────────────────────────────────────

export async function fetchDiagnostics() {
  return apiFetch('/api/diagnostics');
}

export async function fetchJobs() {
  return apiFetch('/api/jobs');
}
