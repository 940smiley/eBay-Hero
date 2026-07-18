/**
 * OAuthCallback — Route rendered at /oauth-callback
 *
 * eBay redirects back here with ?code=&state= after user consent.
 * We exchange the code for tokens via our local API bridge and navigate home.
 */

import React, { useEffect, useState } from 'react';
import { handleOAuthCallback } from '../lib/ebayOAuth.js';

export default function OAuthCallback({ onSuccess, onError }) {
  const [status, setStatus] = useState('exchanging'); // 'exchanging' | 'success' | 'error'
  const [message, setMessage] = useState('');

  useEffect(() => {
    (async () => {
      try {
        const params = new URLSearchParams(window.location.search);
        const error = params.get('error');
        if (error) throw new Error(`eBay returned error: ${error} — ${params.get('error_description')}`);
        if (!params.get('code')) throw new Error('No authorization code in callback URL.');

        await handleOAuthCallback();
        setStatus('success');
        setMessage('eBay account connected! Returning to dashboard...');
        onSuccess?.();
        setTimeout(() => {
          window.history.replaceState({}, '', '/');
          window.location.reload();
        }, 2000);
      } catch (e) {
        setStatus('error');
        setMessage(e.message);
        onError?.(e.message);
      }
    })();
  }, []);

  return (
    <div style={{
      minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center',
      background: 'var(--bg-primary)', flexDirection: 'column', gap: '20px',
    }}>
      <div style={{ fontSize: '40px' }}>{status === 'success' ? '✅' : status === 'error' ? '❌' : '⚡'}</div>
      <h2 style={{ fontFamily: 'var(--font-display)', fontSize: '24px' }}>
        {status === 'exchanging' && 'Connecting to eBay…'}
        {status === 'success'    && 'Connected!'}
        {status === 'error'      && 'Connection Failed'}
      </h2>
      <p style={{ color: 'var(--text-muted)', maxWidth: '480px', textAlign: 'center', fontSize: '14px' }}>
        {status === 'exchanging'
          ? 'Exchanging authorization code for access token. Please wait…'
          : message}
      </p>
      {status === 'error' && (
        <button
          onClick={() => window.history.back()}
          style={{ background: 'linear-gradient(135deg, var(--accent-primary), #4facfe)', border: 'none', padding: '10px 24px', borderRadius: '10px', cursor: 'pointer', fontWeight: '600', color: '#fff' }}
        >
          ← Go Back
        </button>
      )}
    </div>
  );
}
