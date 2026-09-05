# ADR 0029: Reviewed Persistence Capabilities and Narrow Adapters

- Status: Accepted
- Date: 2026-09-05
- Owners: Backend architecture
- Related: ADR 0001, ADR 0007, ADR 0009, ADR 0017, ADR 0028
- Supersedes: None

## Context

Module-owned persistence models still compose one FoodDiaryDbContext. Referencing central Infrastructure therefore exposes every mapped aggregate to an adapter. The project-reference matrix protects assembly dependencies but cannot distinguish a no-tracking read from a foreign aggregate write. Some application guards also scanned retired directories and could succeed without inspecting current modules.

## Decision Drivers

- Preserve shared transactions, ChangeTracker identity, domain event dispatch, audit, outbox and migration history.
- Make persistence access growth reviewable without creating a context or assembly for every feature.
- Reduce demonstrated coupling and preserve HTTP, provider and database behavior.
- Keep architecture checks grounded in existing, nonempty physical source roots.

## Considered Options

1. Split DbContexts and databases immediately. This changes cross-module transaction and lifecycle semantics and requires an independent migration design.
2. Create a generic repository or context facade exposing every set. This retains the same capability under a different name.
3. Review actual adapter capabilities, restrict simple adapters to owned sets, and extend the pattern only where invariants permit it. Selected.

## Decision

Keep one central context, unit of work and migration history. Record each module adapter's entity access, tracking, writes, entity calls and context escape APIs in `docs/architecture/persistence-capabilities.json`. Changes to these capabilities require an explicit manifest review with a reason. Files using context APIs also require a reviewed source fingerprint: a preexisting Database capability must not silently permit arbitrary new SQL. A fingerprint is change detection, not a security proof or an automatic approval mechanism.

Use Hydration as the narrow adapter pilot. Its repository receives the scoped `DbSet<HydrationEntry>` from the existing context. It neither saves nor owns transactions. The domain stores scalar UserId without a User navigation or Users.Domain reference. PersistenceModel retains the User FK and cascade. Synchronize the EF snapshot with the navigation metadata; do not create an empty migration when the relational model has not changed.

Read queries should acquire read-model capabilities. Identity's active-session listing projects only session id, provider, user agent and timestamps, filters by user and revocation, and uses no tracking. Refresh token hashes remain outside that query's model. Authentication mutation paths retain their existing repository and ownership.

Resolve required source roots through the canonical 34-module inventory. Missing or empty required roots fail; legacy-absence assertions remain separately permissive. Discover module inventory independently before comparing the application dependency manifest.

Dashboard requires an independent statistics reader. Do not register a mediator fallback that routes a statistics request back to itself. Hosts compose SQL readers explicitly; incomplete composition fails instead of recursing.

Extract BCL-only transport primitives to `Shared/FoodDiary.Integrations.Http`, retaining CLR namespaces and telemetry names. Ai, Images, Usda, OpenFoodFacts and Wearables reference this project directly. The new assembly has no runtime ProjectReference or PackageReference. Mail clients and billing integrations remain in central Integrations. Modules still transitively see persistence models through the shared context; this extraction does not remove that intentional dependency.

Initializer composes by command: database inspection/targeted migration/seed commands use persistence; outbox administration additionally registers its four replay streams; update-to-latest retains bootstrap composition. Keep the audit interceptor and scoped lifetime behavior in every applicable profile. Do not eagerly resolve bootstrap and replay services before command selection.

Existing application implementation references are acknowledged API dependencies, not permission to instantiate foreign handlers or obtain aggregate repositories. See `docs/architecture/architecture-improvements-20260905.md` for their consumed surfaces. Moving all public requests/models to new assemblies is not required by this decision.

## Consequences

### Positive

- Access expansion, missing module roots and recursive fallback composition have executable regression checks.
- Hydration preserves transaction and FK behavior with less domain and adapter access.
- Five provider modules no longer acquire Stripe or mail client libraries through shared HTTP helpers.
- Operational Initializer commands avoid unrelated bootstrap application registration and resolution.

### Negative

- Most adapters still have full context access at compile time. Source checks do not replace authentication or row-level predicates and cannot prove dynamic reflection behavior.
- Raw SQL and ChangeTracker adapters require manual review when their source fingerprint changes.
- One small runtime assembly and its Docker/solution/lockfile maintenance are added.
- Users aggregate lifecycle and cross-module persistence models remain coupled; blindly removing their relationships would weaken existing invariants.

## Enforcement

- `ModuleSourceCatalogTests`, `ModuleDependencyGraphTests`, `ProjectDependencyMatrixTests`.
- `PersistenceCapabilityTests`, including alias, bulk write and nested no-tracking regressions.
- Hydration PostgreSQL rollback, no-tracking, cascade and pending-model-change tests.
- Identity active-session projection PostgreSQL test.
- Dashboard real-mediator composition tests and Initializer profile tests.
- `ProviderAdapterOwnershipTests`, Docker project-copy checks and dependency lockfiles.
- Existing `UserDataLifecycleGuardrailTests` retain full direct-User-FK coverage and cleanup ordering constraints.

## Follow-up

Extend scoped persistence access to another module only after checking its transactions, owned relationships and foreign reads. Separate User account/security/profile/lifecycle capabilities incrementally; preserve the existing deletion policy matrix. Extract additional application contracts or UI workflows when their change patterns justify a separate boundary. These are subsequent refactorings, not claims of completed isolation in this change.
