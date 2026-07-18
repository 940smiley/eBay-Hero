import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.jsx'
import OAuthCallback from './components/OAuthCallback.jsx'

// ── Minimal client-side router ─────────────────────────────────────────────
// Detects /oauth-callback path and renders the callback handler,
// otherwise renders the main App. No external router dependency needed.

function Root() {
  const path = window.location.pathname;
  if (path === '/oauth-callback') {
    return <OAuthCallback
      onSuccess={() => console.log('[eBayHero] OAuth success')}
      onError={(e) => console.error('[eBayHero] OAuth error:', e)}
    />;
  }
  return <App />;
}

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <Root />
  </StrictMode>,
)

// ── PWA Service Worker ─────────────────────────────────────────────────────
if ('serviceWorker' in navigator) {
  window.addEventListener('load', () => {
    navigator.serviceWorker.register('/sw.js')
      .then(reg => console.log('[eBayHero] Service Worker registered', reg.scope))
      .catch(err => console.warn('[eBayHero] Service Worker registration failed', err));
  });
}
