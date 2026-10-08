# ADR 0001 — Canonical architecture

- **Status:** Accepted
- **Date:** 2026-08-12 (baseline) · updated 2026-10-08
- **Supersedes:** the split eBay Hero / CardOps repositories

## Context

eBay Hero consolidates a set of related reseller tools (eBay Hero, CardOps, Stamplicity)
into one platform. The risk with consolidation is a monolith where one vertical's changes
break the others, and where premium features get tangled into core workflows.

## Decision

Adopt a **local-first, hub-and-spoke architecture** in .NET 8:

1. A platform-neutral `eBayHero.Core` owns the domain, business rules, entitlement
   controller, plugin runtime, and the eBay engine.
2. Infrastructure concerns (EF Core SQLite, secrets, diagnostics) live in
   `eBayHero.Infrastructure`; file and OCR concerns live in dedicated projects.
3. Vertical capabilities ship as **plugins** implementing `IEcosystemPlugin`, referencing
   Core but never referenced by it.
4. All feature gating flows through `IEntitlementService`; core code never checks a tier.
5. eBay-specific field names live in mapping profiles, not the domain model.

Dependency direction is strictly `Plugins -> Core <- Infrastructure` (and `App/Web` above
both). Core has no reference to any plugin project.

## Consequences

**Positive**

- Removing or disabling a plugin cannot break core functionality.
- Paywalls can be added or changed without touching workflows.
- The plugin boundary is a clean test seam.
- The same core can back the WPF shell, the web companion, and the demo.

**Negative**

- Some glue code is duplicated per plugin.
- Cross-plugin coordination must go through hooks rather than direct calls.
- The dependency rule must be enforced in review (and, ideally, CI).

## Compliance

- `eBayHero.Core.csproj` references no plugin project.
- `tests/eBayHero.Plugins.Tests` asserts registry/host isolation and gating.
- Removing a plugin from the registry is covered by a test.
