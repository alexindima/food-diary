# ADR 0025: Extract the BodyMetrics Measurement Domain

- Status: Accepted
- Date: 2026-09-01
- Owners: Backend architecture
- Related: ADR 0016, ADR 0017, ADR 0019
- Supersedes: None

## Context

The BodyMetrics logical module already owns weight and waist measurement use cases, repository ports, persistence adapters, and EF configurations, while `WeightEntry`, `WaistEntry`, and their identifiers remained in central Domain. The remaining compatibility seam consisted of forward entry-to-`User` navigations and inverse read-only collections on `User`. Production consumers use the central `DbSet` or BodyMetrics repositories; only EF configuration and a central collection-shape test consumed the inverse measurement collections.

Weight and waist goals are materially different: their lifecycle and status behavior remains part of the central User responsibility and is outside this decision. The shared context, historical migrations, and snapshot must also remain central.

## Decision Drivers

- Make measurement-domain ownership match the established BodyMetrics application and persistence ownership.
- Preserve CLR namespaces, domain behavior, health-data authorization, and the complete relational model.
- Avoid a central Domain dependency on a module Domain assembly.
- Keep goal lifecycle and central database history unchanged.

## Considered Options

1. Retain measurement entities, identifiers, and inverse User collections in central Domain.
2. Extract the measurement types and remove the non-behavioral inverse collections while retaining forward entry-to-User navigations.
3. Move measurement and goal types together or move the User aggregate.

## Decision

Create `Modules/BodyMetrics/Domain/FoodDiary.Modules.BodyMetrics.Domain.csproj` and move `WeightEntry`, `WaistEntry`, `WeightEntryId`, and `WaistEntryId` into it while preserving their existing `FoodDiary.Domain.*` namespaces and runtime behavior. The module Domain depends one-way on central Domain for `User`, `UserId`, primitives, and shared value-object bounds.

Remove `User.WeightEntries` and `User.WaistEntries` and their backing fields. Configure both relationships from the BodyMetrics persistence model with `HasOne(entry => entry.User).WithMany()` while preserving foreign keys, cascade deletion, tables, schema, unique indexes, date mapping, and ID conversions. Keep `WeightGoal`, `WaistGoal`, their IDs, lifecycle/status behavior, and `User` goal navigations central. Keep `FoodDiaryDbContext`, migrations, and snapshot central and create no migration when the model-delta check is empty.

The assembly identity changes for the four moved types. All supported in-repository consumers are rebuilt together; no packable or supported external binary contract was found. HTTP routes, payloads, status codes, calculations, normalization, cleanup, and retention behavior do not change.

## Consequences

### Positive

- BodyMetrics owns its measurement entities, identifiers, invariants, EF configurations, and focused tests under one logical root.
- Central User no longer exposes collections that carry no domain behavior.
- The project graph remains acyclic and follows the established module-Domain pattern.

### Negative

- BodyMetrics Domain remains coupled to central Domain for shared compatibility types and bounds.
- Stale precompiled binaries that referenced the moved types from `FoodDiary.Domain` require recompilation.

## Enforcement

- `BodyMetricsModuleExtractionTests` verifies exclusive source ownership and absence of inverse User measurement navigations.
- `ProjectDependencyMatrixTests` governs the new production and test project edges.
- Module Domain tests preserve creation, update, validation, normalization, and audit behavior.
- EF pending-model and provider-backed integration checks prove relational compatibility and that no migration is required.

## Follow-up

None.
