# Diagnostics

Create a redacted diagnostic bundle:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Collect-Diagnostics.ps1
```

Default output:

```text
%LOCALAPPDATA%\EbayAssistance\DiagnosticBundles\DiagnosticBundle-YYYYMMDD-HHMMSS.zip
```

Verify redaction:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Verify-Secret-Redaction.ps1 -Path <bundle-or-folder>
```

The bundle includes environment, application, git/build info, redacted config, dependency lists, log tails when available, and reproduction notes. It excludes secrets, tokens, full databases, raw images, buyer data, and inventory exports.

