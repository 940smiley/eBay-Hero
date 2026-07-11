# Security

Report security issues privately to the repository owner before public disclosure.

Current controls:

- Local-first data model.
- Live eBay publishing disabled by default.
- Explicit approval required for live or destructive actions.
- Secret redaction in logging helpers and diagnostics.
- `.env`, `.ENV`, local secrets, logs, databases, and runtime data ignored by git.
- DPAPI-backed secret store exists for Windows.

Known gaps:

- Final code signing is not configured.
- eBay Sandbox OAuth requires user credentials.
- Linux/mobile secure storage abstractions are not implemented yet.

