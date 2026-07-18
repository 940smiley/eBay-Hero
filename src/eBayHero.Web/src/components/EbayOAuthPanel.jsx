/**
 * EbayOAuthPanel — Full eBay OAuth 2.0 connection management panel
 *
 * Handles:
 *  - Credential entry (App ID, Cert ID, RuName)
 *  - Sandbox vs Production toggle
 *  - "Connect to eBay" → redirect to consent screen
 *  - OAuth callback detection and token exchange
 *  - Live connection status display with capability chips
 *  - Disconnect / reconnect
 */

import React, { useState, useEffect, useCallback } from 'react';
import {
  CheckCircle, XCircle, RefreshCw, ExternalLink, ShieldCheck,
  AlertTriangle, Settings, Zap, Lock, Unlock, Link2
} from 'lucide-react';
import {
  redirectToEbayConsent,
  handleOAuthCallback,
  clearToken,
  isConnected,
  loadToken,
  loadSettings,
  saveSettings,
} from '../lib/ebayOAuth.js';
import { getEbayConnectionStatus, disconnectEbay } from '../lib/api.js';

const EMPTY_SETTINGS = {
  clientId:    '',
  certId:      '',
  ruName:      '',
  environment: 'sandbox',
};

const SCOPE_LABELS = {
  'https://api.ebay.com/oauth/api_scope':                            'Basic API access',
  'https://api.ebay.com/oauth/api_scope/sell.account.readonly':      'Account & policies',
  'https://api.ebay.com/oauth/api_scope/sell.inventory.readonly':    'Read inventory',
  'https://api.ebay.com/oauth/api_scope/sell.inventory':             'Manage inventory & offers',
  'https://api.ebay.com/oauth/api_scope/sell.fulfillment.readonly':  'Read orders',
};

export default function EbayOAuthPanel({ onStatusChange }) {
  const [settings, setSettings]     = useState(() => loadSettings() ?? EMPTY_SETTINGS);
  const [connected, setConnected]   = useState(false);
  const [token, setToken]           = useState(null);
  const [capabilities, setCaps]     = useState([]);
  const [loading, setLoading]       = useState(false);
  const [error, setError]           = useState(null);
  const [success, setSuccess]       = useState(null);
  const [showCreds, setShowCreds]   = useState(false);

  // Check if we returned from eBay with ?code=
  const checkCallback = useCallback(async () => {
    const params = new URLSearchParams(window.location.search);
    if (!params.has('code')) return;

    setLoading(true);
    setError(null);
    try {
      const tok = await handleOAuthCallback();
      setToken(tok);
      setConnected(true);
      setSuccess('eBay account connected successfully!');
      onStatusChange?.('connected');

      // Clean the URL so a refresh doesn't re-trigger callback
      window.history.replaceState({}, '', window.location.pathname);
    } catch (e) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }, [onStatusChange]);

  // Load current status from local token + API
  const refreshStatus = useCallback(async () => {
    setLoading(true);
    try {
      const tok = loadToken();
      setToken(tok);
      const connected = isConnected();
      setConnected(connected);

      if (connected) {
        try {
          const status = await getEbayConnectionStatus();
          setCaps(status.capabilities ?? []);
        } catch {
          // API not running yet — show token-based status
          setCaps([]);
        }
      }
      onStatusChange?.(connected ? 'connected' : 'disconnected');
    } finally {
      setLoading(false);
    }
  }, [onStatusChange]);

  useEffect(() => {
    checkCallback();
    refreshStatus();
  }, []);

  const handleSaveSettings = () => {
    saveSettings(settings);
    setSuccess('Settings saved.');
    setTimeout(() => setSuccess(null), 3000);
  };

  const handleConnect = () => {
    setError(null);
    try {
      saveSettings(settings);
      redirectToEbayConsent(settings);
    } catch (e) {
      setError(e.message);
    }
  };

  const handleDisconnect = async () => {
    setLoading(true);
    try {
      await disconnectEbay().catch(() => {}); // best-effort API call
      clearToken();
      setToken(null);
      setConnected(false);
      setCaps([]);
      setSuccess('Disconnected from eBay.');
      onStatusChange?.('disconnected');
    } finally {
      setLoading(false);
    }
  };

  const grantedScopes = token?.scope?.split(' ') ?? [];

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>

      {/* STATUS BANNER */}
      <div style={{
        display: 'flex', alignItems: 'center', justifyContent: 'space-between',
        padding: '16px 20px', borderRadius: '12px',
        background: connected ? 'rgba(46,213,115,0.08)' : 'rgba(255,71,87,0.08)',
        border: `1px solid ${connected ? 'rgba(46,213,115,0.3)' : 'rgba(255,71,87,0.2)'}`,
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
          {connected
            ? <CheckCircle size={24} style={{ color: 'var(--accent-success)' }} />
            : <XCircle size={24} style={{ color: 'var(--accent-danger)' }} />}
          <div>
            <div style={{ fontWeight: '700', fontSize: '15px' }}>
              {connected ? 'eBay Account Connected' : 'Not Connected to eBay'}
            </div>
            <div style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
              {connected
                ? `Token active · Env: ${settings.environment}`
                : 'Enter credentials below and connect your eBay seller account'}
            </div>
          </div>
        </div>
        <div style={{ display: 'flex', gap: '8px' }}>
          <button onClick={refreshStatus} disabled={loading} style={ghostBtn}>
            <RefreshCw size={14} className={loading ? 'spinning' : ''} /> Refresh
          </button>
          {connected && (
            <button onClick={handleDisconnect} disabled={loading} style={{ ...ghostBtn, color: 'var(--accent-danger)', borderColor: 'rgba(255,71,87,0.3)' }}>
              <Unlock size={14} /> Disconnect
            </button>
          )}
        </div>
      </div>

      {/* ALERTS */}
      {error && (
        <div style={{ ...alertBox, background: 'rgba(255,71,87,0.1)', borderColor: 'rgba(255,71,87,0.3)', color: 'var(--accent-danger)' }}>
          <AlertTriangle size={16} /> {error}
        </div>
      )}
      {success && (
        <div style={{ ...alertBox, background: 'rgba(46,213,115,0.1)', borderColor: 'rgba(46,213,115,0.3)', color: 'var(--accent-success)' }}>
          <CheckCircle size={16} /> {success}
        </div>
      )}

      {/* CREDENTIALS FORM */}
      <div style={{ background: 'rgba(255,255,255,0.02)', borderRadius: '12px', border: '1px solid rgba(255,255,255,0.06)', overflow: 'hidden' }}>
        <button
          onClick={() => setShowCreds(p => !p)}
          style={{ ...sectionHeader, borderBottom: showCreds ? '1px solid rgba(255,255,255,0.06)' : 'none' }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <Settings size={16} /> eBay Developer Credentials
          </div>
          <span style={{ fontSize: '11px', color: 'var(--accent-secondary)' }}>
            {showCreds ? 'Hide ▲' : 'Show ▼'}
          </span>
        </button>

        {showCreds && (
          <div style={{ padding: '20px', display: 'flex', flexDirection: 'column', gap: '14px' }}>

            {/* Environment toggle */}
            <div style={{ display: 'flex', gap: '8px' }}>
              {['sandbox', 'production'].map(env => (
                <button
                  key={env}
                  onClick={() => setSettings(s => ({ ...s, environment: env }))}
                  style={{
                    padding: '8px 16px', borderRadius: '8px', cursor: 'pointer', fontWeight: '600', fontSize: '13px',
                    border: settings.environment === env ? '1px solid var(--accent-secondary)' : '1px solid rgba(255,255,255,0.15)',
                    background: settings.environment === env ? 'rgba(0,242,254,0.1)' : 'transparent',
                    color: settings.environment === env ? 'var(--accent-secondary)' : 'var(--text-muted)',
                  }}
                >
                  {env === 'sandbox' ? '🧪 Sandbox' : '⚡ Production'}
                </button>
              ))}
            </div>

            <div style={{ background: 'rgba(255,215,0,0.06)', border: '1px solid rgba(255,215,0,0.2)', borderRadius: '8px', padding: '10px 14px', fontSize: '12px', color: 'var(--accent-gold)' }}>
              <strong>Never commit credentials.</strong> These are stored in localStorage only. Get your keys from the{' '}
              <a href="https://developer.ebay.com/my/keys" target="_blank" rel="noopener noreferrer" style={{ color: 'var(--accent-secondary)' }}>
                eBay Developer Portal ↗
              </a>
            </div>

            <Field label="App ID (Client ID)" value={settings.clientId}
              onChange={v => setSettings(s => ({ ...s, clientId: v }))}
              placeholder="e.g. YourApp-1234-SBX-..." />

            <Field label="Cert ID (Client Secret)" value={settings.certId}
              onChange={v => setSettings(s => ({ ...s, certId: v }))}
              placeholder="e.g. SBX-abc123..." type="password" />

            <Field label="RuName (OAuth Redirect URI Name)" value={settings.ruName}
              onChange={v => setSettings(s => ({ ...s, ruName: v }))}
              placeholder="e.g. Your_App-YourApp-1234-..." />

            <div style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
              <strong>Callback / Redirect URI</strong> to register in the eBay Developer Portal:
              <code style={{ display: 'block', marginTop: '4px', padding: '6px 10px', background: 'rgba(0,0,0,0.3)', borderRadius: '4px', fontSize: '11px', color: 'var(--accent-secondary)', wordBreak: 'break-all' }}>
                {window.location.origin}/oauth-callback
              </code>
            </div>

            <div style={{ display: 'flex', gap: '8px', paddingTop: '4px' }}>
              <button onClick={handleSaveSettings} style={ghostBtn}>
                <Lock size={14} /> Save Settings
              </button>
              <button onClick={handleConnect} style={primaryBtn} disabled={!settings.clientId || !settings.ruName}>
                <Link2 size={14} /> Connect to eBay
              </button>
            </div>
          </div>
        )}
      </div>

      {/* GRANTED SCOPES */}
      {connected && grantedScopes.length > 0 && (
        <div>
          <h4 style={{ fontSize: '13px', color: 'var(--text-muted)', marginBottom: '10px', textTransform: 'uppercase', letterSpacing: '0.05em' }}>Granted Permissions</h4>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px' }}>
            {grantedScopes.map(scope => (
              <div key={scope} style={{
                display: 'flex', alignItems: 'center', gap: '6px',
                padding: '5px 10px', borderRadius: '6px', fontSize: '12px',
                background: 'rgba(46,213,115,0.1)', border: '1px solid rgba(46,213,115,0.25)',
                color: 'var(--accent-success)',
              }}>
                <ShieldCheck size={12} />
                {SCOPE_LABELS[scope] ?? scope}
              </div>
            ))}
          </div>
        </div>
      )}

      {/* CAPABILITY CHIPS from API */}
      {capabilities.length > 0 && (
        <div>
          <h4 style={{ fontSize: '13px', color: 'var(--text-muted)', marginBottom: '10px', textTransform: 'uppercase', letterSpacing: '0.05em' }}>API Capabilities</h4>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px' }}>
            {capabilities.map(cap => (
              <div key={cap.id} style={{
                padding: '5px 10px', borderRadius: '6px', fontSize: '12px',
                background: cap.available ? 'rgba(46,213,115,0.1)' : 'rgba(255,71,87,0.08)',
                border: `1px solid ${cap.available ? 'rgba(46,213,115,0.25)' : 'rgba(255,71,87,0.2)'}`,
                color: cap.available ? 'var(--accent-success)' : 'var(--accent-danger)',
              }}>
                {cap.available ? '✓' : '✗'} {cap.displayName}
              </div>
            ))}
          </div>
        </div>
      )}

      {/* QUICK LINKS */}
      <div style={{ borderTop: '1px solid rgba(255,255,255,0.06)', paddingTop: '16px', display: 'flex', gap: '12px', flexWrap: 'wrap' }}>
        {[
          { label: 'eBay Developer Portal', url: 'https://developer.ebay.com' },
          { label: 'OAuth Token Types', url: 'https://developer.ebay.com/api-docs/static/oauth-token-types.html' },
          { label: 'Sell Inventory API', url: 'https://developer.ebay.com/api-docs/sell/inventory/resources/methods' },
        ].map(link => (
          <a key={link.url} href={link.url} target="_blank" rel="noopener noreferrer" style={{ display: 'flex', alignItems: 'center', gap: '4px', fontSize: '12px', color: 'var(--accent-secondary)', textDecoration: 'none' }}>
            <ExternalLink size={12} /> {link.label}
          </a>
        ))}
      </div>

    </div>
  );
}

// ── Sub-components ──────────────────────────────────────────────────────────

function Field({ label, value, onChange, placeholder, type = 'text' }) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '5px' }}>
      <label style={{ fontSize: '12px', color: 'var(--text-muted)', fontWeight: '600' }}>{label}</label>
      <input
        type={type}
        value={value}
        onChange={e => onChange(e.target.value)}
        placeholder={placeholder}
        style={{
          background: 'var(--bg-primary)', border: '1px solid rgba(255,255,255,0.12)',
          padding: '10px 12px', borderRadius: '8px', fontSize: '13px',
          color: 'var(--text-main)', outline: 'none',
          transition: 'border-color 0.2s',
        }}
        onFocus={e => (e.target.style.borderColor = 'var(--accent-secondary)')}
        onBlur={e => (e.target.style.borderColor = 'rgba(255,255,255,0.12)')}
      />
    </div>
  );
}

// ── Styles ──────────────────────────────────────────────────────────────────

const ghostBtn = {
  background: 'rgba(255,255,255,0.06)', border: '1px solid rgba(255,255,255,0.15)',
  padding: '8px 14px', borderRadius: '8px', cursor: 'pointer', fontWeight: '500',
  fontSize: '13px', display: 'flex', alignItems: 'center', gap: '6px', color: 'var(--text-main)',
  transition: 'all 0.2s',
};

const primaryBtn = {
  background: 'linear-gradient(135deg, var(--accent-primary), #4facfe)',
  border: 'none', padding: '8px 20px', borderRadius: '8px', cursor: 'pointer',
  fontWeight: '700', fontSize: '13px', display: 'flex', alignItems: 'center',
  gap: '6px', color: '#fff', boxShadow: '0 4px 15px rgba(138,43,226,0.4)',
  transition: 'all 0.2s',
};

const alertBox = {
  display: 'flex', alignItems: 'center', gap: '8px',
  padding: '10px 14px', borderRadius: '8px', fontSize: '13px',
  border: '1px solid', fontWeight: '500',
};

const sectionHeader = {
  width: '100%', background: 'transparent', border: 'none',
  padding: '14px 20px', display: 'flex', justifyContent: 'space-between',
  alignItems: 'center', cursor: 'pointer', color: 'var(--text-main)',
  fontWeight: '600', fontSize: '14px',
};
