# Quickstart

From a clean machine to your first validated draft.

## 1. Prerequisites

- .NET SDK **8.0+** (`dotnet --version`)
- Node.js **20+** (only for the web companion and demo)
- Optional: Tesseract OCR for local text extraction
- An eBay developer keyset (sandbox is enough to start)

## 2. Clone and build

```bash
git clone https://github.com/940smiley/eBay-Hero.git
cd eBay-Hero/csharp

dotnet restore eBayHero.sln
dotnet build   eBayHero.sln -c Release --no-restore
dotnet test    eBayHero.sln -c Release --no-build
```

On Linux/macOS the WPF shell is skipped; build the cross-platform projects instead:

```bash
dotnet build src/eBayHero.Core/eBayHero.Core.csproj -c Release
dotnet test  tests/eBayHero.Plugins.Tests/eBayHero.Plugins.Tests.csproj -c Release
```

### Low-disk machines

```powershell
$env:EA_BUILD_ROOT  = 'D:\WORK\BuildArtifacts\ebay-hero'
$env:NUGET_PACKAGES = 'D:\WORK\.nuget-packages'
dotnet restore .\csharp\eBayHero.sln --packages $env:NUGET_PACKAGES
```

## 3. Configure credentials

```bash
cp .env.example .env
# edit .env
```

Minimum for a sandbox connection: `EBAY_CLIENT_ID`, `EBAY_CLIENT_SECRET`,
`EBAY_RUNAME`, and (after consent) `EBAY_REFRESH_TOKEN`. Keep
`EBAY_ENVIRONMENT=sandbox` until you have validated a draft end to end.

Secrets entered in the app are stored through the platform secret store, not in `.env`.

## 4. Connect to eBay (sandbox)

1. Open the app (or the companion surface) and go to **Settings → eBay**.
2. Enter the App ID, Cert ID, RuName, and marketplace.
3. Click **Connect**; the app builds the OAuth authorization URL and opens it.
4. Complete consent; the loopback callback captures the code and stores a refresh token.
5. Click **Test connection** to verify inventory and account scope access.

## 5. Enable plugins

Open **Settings → Plugins** and enable the verticals you sell in:

- **CardOps** — trading cards (free schema/entry/export, premium AI).
- **Stamplicity** — stamps (free catalog/staging, premium AI).
- **All-in-One AI Suite** — unlocks AI vision/OCR/valuation everywhere.

Disabling a plugin immediately unmounts its hooks and routes; core keeps working.

## 6. First draft

1. Add a photo root under **Settings → Sources** and run **Scan**.
2. Review the import plan, then apply it.
3. Select an item, run OCR if available, and fill in the metadata.
4. Choose a listing template and field-mapping profile.
5. Click **Create draft**; review the audit findings.
6. Export an eBay-safe CSV, or — if entitled — publish a batch.

## 7. Try the demo

```bash
npx serve docs        # then open the printed URL
```

Or visit the hosted demo: https://940smiley.github.io/eBay-Hero/

## Troubleshooting

| Symptom | Fix |
|---|---|
| `dotnet` not found | install the .NET 8 SDK |
| WPF project fails on Linux/macOS | expected — build the cross-platform projects |
| SQLite ordering error on DateTimeOffset | ensure migrations are applied (`dotnet ef database update`) |
| OAuth returns `invalid_client` | check App ID / Cert ID and that you're using the matching environment |
| No AI steps in the demo | enable the AI Suite addon or switch to the Pro preset |
