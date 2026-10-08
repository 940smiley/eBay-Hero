# CI & GitHub Pages setup

The repository's workflow files (`.github/workflows/*`) can only be updated by an identity
with the GitHub **`workflows`** permission. The changes below were prepared and verified
locally but are documented here so a maintainer (or a token with that permission) can apply
them.

Two problems were fixed in the proposal:

1. `.github/workflows/ci.yml` and `release.yml` referenced a solution (`InventoryPhotoOps.sln`)
   and script paths (`./scripts/...`) that do not exist. They must point at
   `csharp/eBayHero.sln` and `csharp/scripts/...`.
2. CI ran only on Windows. A Linux job builds/tests the cross-platform projects (the WPF
   shell targets `net8.0-windows` and cannot build on Linux).

## 1. Replace `.github/workflows/ci.yml`

```yaml
name: ci

on:
  push:
  pull_request:

jobs:
  windows:
    runs-on: windows-latest
    env:
      EA_BUILD_ROOT: ${{ runner.temp }}\ebay-hero-build
      EA_RELEASE_ROOT: ${{ runner.temp }}\ebay-hero-release
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
          cache: true
      - name: Restore tools
        run: dotnet tool restore
      - name: Restore
        run: dotnet restore csharp/eBayHero.sln
      - name: Build
        run: dotnet build csharp/eBayHero.sln -c Release --no-restore
      - name: Test
        run: dotnet test csharp/eBayHero.sln -c Release --no-build

  linux:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
          cache: true
      - name: Restore tools
        run: dotnet tool restore
      - name: Build cross-platform projects
        run: |
          for project in \
            csharp/src/eBayHero.Core/eBayHero.Core.csproj \
            csharp/src/eBayHero.Infrastructure/eBayHero.Infrastructure.csproj \
            csharp/src/eBayHero.FileSystem/eBayHero.FileSystem.csproj \
            csharp/src/eBayHero.Ocr/eBayHero.Ocr.csproj \
            csharp/src/eBayHero.Export/eBayHero.Export.csproj \
            csharp/src/eBayHero.Plugins.CardOps/eBayHero.Plugins.CardOps.csproj \
            csharp/src/eBayHero.Plugins.Stamplicity/eBayHero.Plugins.Stamplicity.csproj \
            csharp/src/eBayHero.Plugins.AiSuite/eBayHero.Plugins.AiSuite.csproj \
            csharp/tools/eBayHero.Cli/eBayHero.Cli.csproj ; do
            dotnet build "$project" -c Release
          done
      - name: Test cross-platform projects
        run: |
          dotnet test csharp/tests/eBayHero.Core.Tests/eBayHero.Core.Tests.csproj -c Release
          dotnet test csharp/tests/eBayHero.Infrastructure.Tests/eBayHero.Infrastructure.Tests.csproj -c Release
          dotnet test csharp/tests/eBayHero.IntegrationTests/eBayHero.IntegrationTests.csproj -c Release
          dotnet test csharp/tests/eBayHero.Plugins.Tests/eBayHero.Plugins.Tests.csproj -c Release

  demo:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: '20'
      - name: Syntax-check demo modules
        run: |
          cp docs/demo/data.js /tmp/data.mjs
          cp docs/demo/app.js /tmp/app.mjs
          node --check /tmp/data.mjs
          node --check /tmp/app.mjs
```

## 2. Add `.github/workflows/pages.yml`

Publishes the interactive demo in `docs/` to GitHub Pages. In repository settings set
**Pages → Build and deployment → Source = GitHub Actions**.

```yaml
name: pages

on:
  push:
    branches: [ main, master ]
    paths: [ 'docs/**', '.github/workflows/pages.yml' ]
  workflow_dispatch:

permissions:
  contents: read
  pages: write
  id-token: write

concurrency:
  group: pages
  cancel-in-progress: true

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/configure-pages@v5
      - uses: actions/upload-pages-artifact@v3
        with:
          path: docs

  deploy:
    needs: build
    runs-on: ubuntu-latest
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    steps:
      - id: deployment
        uses: actions/deploy-pages@v4
```

Add the default branch to the `branches` list if it is not `main`/`master`.

## 3. Replace `.github/workflows/release.yml` script paths

```yaml
name: release

on:
  workflow_dispatch:

jobs:
  windows-release:
    runs-on: windows-latest
    env:
      EA_BUILD_ROOT: ${{ runner.temp }}\ebay-hero-build
      EA_RELEASE_ROOT: ${{ github.workspace }}\artifacts\release\windows
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet restore csharp/eBayHero.sln
      - run: dotnet test csharp/eBayHero.sln -c Release
      - shell: pwsh
        run: ./csharp/scripts/publish-portable.ps1
      - uses: actions/upload-artifact@v4
        with:
          name: eBay-Hero-windows-release
          path: artifacts/release/windows
```

## Alternative: no workflow-file permission

If you cannot change workflow files, you can still run everything locally:

```bash
cd csharp
dotnet restore eBayHero.sln
dotnet build eBayHero.sln -c Release --no-restore   # Windows only
dotnet test  tests/eBayHero.Plugins.Tests/eBayHero.Plugins.Tests.csproj -c Release
```
