# eBay Assistance (eBay Hero)

eBay Assistance is the canonical consolidation repository for CardOps and eBay Hero.

Current implementation status: Windows production baseline imported from `eBayHero`, with .NET 8 WPF UI, SQLite persistence, image scanning, OCR services, pricing/lot/listing domain services, eBay-safe export, CardOps SQLite import support, CLI/migrator tools, tests, and release scripts. 

The repository has recently been restructured into a **polyglot monorepo** to support multiple languages and build-outs.

---

## 🚀 Beginner's Quick Start Guide

We've made it easy for beginners to get started! You don't need to use the command line if you're on Windows. Just double-click the `.cmd` launchers in the root directory:

### 1. Launching the App
To start the application and test out the interface:
- Double-click **`Run-Demo.cmd`**
- *This will automatically build and launch the main application.*

### 2. Running Tests
To ensure everything is working correctly on your machine:
- Double-click **`Run-Tests.cmd`**
- *This runs all the automated tests to verify the code is healthy.*

### 3. Building the Project
To compile the code without running it:
- Double-click **`Build-Project.cmd`**

### 4. Deploying & Publishing
To package the app for distribution:
- Double-click **`Deploy-Windows.cmd`**

### 5. Control Center
To open the administrative control center:
- Double-click **`Open-ControlCenter.cmd`**

---

## 📁 Repository Structure

This repository contains multiple languages for different components:

- **`csharp/`** - The primary .NET 8 WPF Application, tests, scripts, and installer tools.
- **`python/`** - Python build-outs and scripts.
- **`nodejs/`** - Node.js and TypeScript build-outs.
- **`java/`** - Java build-outs.
- **`go/`** - Go build-outs.

*(See the `README.md` in each folder for language-specific details.)*

---

## 🛠 Advanced Build Instructions (C# / .NET)

For developers who prefer the command line or need to configure build roots (e.g., if your `C:` drive is full), you can use the PowerShell scripts directly from the `csharp/` directory:

```powershell
$env:EA_BUILD_ROOT='D:\WORK\BuildArtifacts\ebay-assistance'
$env:NUGET_PACKAGES='D:\WORK\.nuget-packages'

# Restore packages
dotnet restore .\csharp\eBayHero.sln --packages $env:NUGET_PACKAGES

# Build and Test
dotnet build .\csharp\eBayHero.sln -c Release --no-restore
dotnet test .\csharp\eBayHero.sln -c Release --no-build
```

Alternatively, you can run the scripts via PowerShell:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\csharp\scripts\build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\csharp\scripts\test.ps1
```

### Product Build Flavors
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\csharp\scripts\build-flavors.ps1 -Flavor All
```
- `Development`: unlimited premium testing.
- `Public`: 14-day/25-action premium trial with an upgrade gate.

*(Set `EA_UPGRADE_URL` to the hosted production checkout URL before distributing the public build. Neither flavor enables live eBay publication.)*

---

## 📚 Key Docs

- [Architecture](docs/adr/0001-canonical-architecture.md)
- [Source discovery](docs/SOURCE-DISCOVERY-REPORT.md)
- [Import provenance](docs/IMPORT-PROVENANCE.md)
- [Roadmap](ROADMAP.md)
- [Monetization plan](MONETIZATION_PLAN.md)
- [User actions required](docs/USER-ACTION-REQUIRED.md)
- [Diagnostics](docs/DIAGNOSTICS.md)

---

## 🔒 Safety Defaults

- Live eBay publishing is not enabled by default.
- CardOps runtime data, `.ENV`, OAuth token data, logs, thumbnails, and local SQLite data were not imported.
- Build outputs are ignored and can be redirected with `EA_BUILD_ROOT`.
- Public Windows packaging is blocked on available local disk space or a redirected release root.
