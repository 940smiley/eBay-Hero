# Production Release Checklist

Use this checklist to move the eBay assistance product from development to a public download.

## Blocked Until Provided

- Actual source repository URL or a populated local checkout.
- Target platform and packaging format: web app, Chrome extension, desktop app, mobile app, CLI, or another distribution model.
- Product name, public positioning, and intended buyer persona.
- eBay integration method: official eBay API, browser automation, import/export workflow, or manual listing workflow.
- Required credentials and accounts: eBay developer app, analytics, payments, hosting, code signing, and support inbox.
- Legal requirements: privacy policy, terms of service, refund policy, and data retention rules.

## Engineering Release Gates

- Install dependencies from a clean checkout without manual local-only steps.
- Add a reproducible build command and document it in the README.
- Add automated tests for the highest-risk workflows: authentication, listing import/export, pricing calculations, fee estimates, inventory updates, and order-related actions.
- Add linting and formatting checks.
- Add CI that runs install, lint, test, and build on pull requests.
- Add environment variable documentation with safe example values.
- Remove secrets, local paths, test credentials, and generated artifacts from the repository.
- Add structured error handling for eBay API failures, rate limits, invalid listings, expired tokens, and network timeouts.
- Add logging that is useful for support without exposing customer data.
- Add a privacy-safe telemetry plan for activation, retention, errors, and paid conversion.
- Add dependency and vulnerability scanning.
- Confirm license compatibility for all runtime dependencies.

## Product Release Gates

- Define a public version number and changelog.
- Create installation instructions for the target platform.
- Create an uninstall or account disconnect path.
- Add first-run onboarding that gets a user to one successful eBay workflow quickly.
- Add support contact and troubleshooting documentation.
- Prepare screenshots or a short demo video for the download page.
- Confirm app behavior complies with eBay developer terms and marketplace policies.
- Prepare a release candidate build and test it on a clean machine or clean browser profile.

## Public Download Readiness

- Choose distribution channel: GitHub Releases, hosted download page, Chrome Web Store, Microsoft Store, Apple App Store, or direct installer.
- Add release artifacts to the chosen channel.
- Include checksum or platform-native signature for downloadable binaries.
- Add release notes with known limitations.
- Set up crash/error monitoring before launch.
- Set up a feedback channel and triage process.
- Run a final smoke test from the public download path, not from the development environment.

