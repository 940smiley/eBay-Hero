# Security Architecture

- Local app binds no public network service by default.
- SQLite is local by default.
- eBay publishing is disabled by default and requires explicit approval.
- Secrets are excluded from git and diagnostics.
- Windows secret storage uses DPAPI where implemented.
- OAuth state validation exists in domain services.
- File operations are planned through manifests and should require confirmation before destructive changes.
- Diagnostic bundles redact known secret patterns and exclude raw private data by default.

Open work:

- Linux/mobile secure storage.
- Installer signing.
- eBay Sandbox token exchange hardening.
- Full repository secret scan in CI.

