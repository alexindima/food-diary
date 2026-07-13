# ADR 0008: Product And Recipe Read-Model Query Paths

- Status: Accepted
- Date: 2026-07-05
- Owners: Backend Food modules
- Related: ADR-0006
- Supersedes: None

## Context

Product and recipe list screens, overview payloads, recent items, and explore results need compact projection data. Loading full domain aggregates for these paths couples query handlers to aggregate persistence shape, risks accidental eager loading, and makes usage-count checks depend on navigation collections.

Product and recipe command paths still need aggregate repositories for write-oriented validation and mutation. Some read-side payloads also need behavior-compatible projection logic, including recipe nutrition summaries for auto-calculated recipes when persisted totals are not available yet.

## Decision Drivers

- Query performance and payload shape should not depend on aggregate-loading graphs.
- Command paths must preserve aggregate behavior and validation.
- Existing API behavior must remain stable while read paths are optimized.

## Considered Options

1. Use aggregate repositories for both commands and queries. This minimizes interfaces but couples read endpoints to persistence and aggregate shape.
2. Introduce a general-purpose query repository shared by all food features. This centralizes reads but weakens module ownership and tends to accumulate unrelated projections.
3. Provide module-owned read services for projection queries while retaining narrow aggregate repositories for commands.

## Decision

- Product query handlers use `IProductOverviewReadService` for list, overview, recent, and by-id read payloads.
- Recipe query handlers use `IRecipeOverviewReadService` for list, overview, recent, explore, and by-id read payloads.
- Product and recipe aggregate read repositories remain narrow and write-oriented: load aggregates by id or ids, and expose explicit usage-count queries.
- Command validators and handlers use count projections for product and recipe usage checks instead of loading navigation collections and counting them in memory.
- Recipe overview projections preserve existing API behavior by calculating nutrition summaries from projected ingredients for auto-calculated recipes when stored totals are absent.

## Consequences

### Positive

- Query handlers depend on stable application projections instead of EF aggregate-loading details.
- Infrastructure can optimize read payloads independently from command aggregate loading.
- Aggregate repositories are less likely to grow list-screen-specific include graphs.

### Negative

- Separate command and query contracts add mapping and interface surface.
- Projection behavior that mirrors domain calculations requires consistency tests.

## Enforcement

- `tests/FoodDiary.ArchitectureTests/BusinessModuleBoundaryTests.cs`
- `tests/FoodDiary.ArchitectureTests/ApplicationGuardrailTests.cs`

## Follow-up

- New product or recipe projection endpoints should use module-owned read services.
- New command usage checks should use explicit count queries rather than loaded navigation collections.
