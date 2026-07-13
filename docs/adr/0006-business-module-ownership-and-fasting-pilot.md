# ADR 0006: Business-Module Ownership And Fasting Pilot

- Status: Accepted
- Date: 2026-07-13
- Owners: Backend architecture
- Related: ADR-0001, ADR-0004, ADR-0007, ADR-0008, ADR-0009
- Supersedes: None

## Context

The primary backend has strong horizontal Clean Architecture boundaries, but business-feature ownership inside those layers was previously implicit. A shared `DbContext` and in-process dependency injection make it technically possible for one feature to acquire another feature's repository, gradually turning a modular monolith into a coupled monolith.

Creating an assembly, schema, or service for every feature would add migration and operational cost before module boundaries are stable.

## Decision Drivers

- Shared infrastructure must not imply shared write ownership.
- Cross-module coupling must be intentional and reviewable.
- Strong logical boundaries are needed without premature distributed deployment or assembly extraction.
- The policy needs an incremental migration path for an established codebase.

## Considered Options

1. Keep ownership informal and rely on code review. This preserves flexibility but does not prevent repository sharing and boundary erosion.
2. Extract every feature into its own assembly, schema, or service immediately. This makes some boundaries physical but adds substantial migration and operational cost.
3. Define logical ownership, expose semantic module APIs, and enforce boundaries incrementally with architecture tests.

## Decision

- Every persisted business concept has an owning module.
- Only the owner may mutate its aggregates and tables.
- Cross-module reads use explicit projection or read-service contracts rather than foreign repositories or aggregates.
- Cross-module actions use a capability, command/service contract, domain event, or integration event owned by the appropriate boundary.
- Domain events may add state in the current transaction but may not perform external I/O; durable external reactions use an outbox as refined by ADR-0007.
- A shared `DbContext` and database remain acceptable. New assemblies, schemas, brokers, or network calls are not required merely to claim modularity.
- Fasting is the pilot for executable vertical-boundary enforcement. Extend the pattern module by module after validating it rather than attempting a single repository-wide rewrite.
- Dashboard-style composed reads may consume stable projections from multiple owners, but this never grants write ownership.

## Consequences

### Positive

- Vertical boundaries become reviewable and executable without a rewrite.
- Modules can evolve toward stronger physical isolation only when concrete pressure appears.
- Cross-module writes and reads communicate intent through semantic contracts.
- The pilot provides a repeatable migration pattern.

### Negative

- Many boundaries rely on architecture tests rather than CLR visibility.
- The ownership map and allowed dependencies require ongoing maintenance.
- Legitimate collaboration can require a new capability instead of direct repository reuse.
- A shared database still permits infrastructure-level joins; the ownership policy governs behavior and writes, not every reporting query.

## Enforcement

- `docs/backend/BACKEND_MODULE_OWNERSHIP.md` is the current ownership and interaction map.
- `docs/architecture/module-dependencies.json` is the current Application dependency graph, governed by ADR-0009.
- `tests/FoodDiary.ArchitectureTests/BusinessModuleBoundaryTests.cs`
- `tests/FoodDiary.ArchitectureTests/ModuleDependencyGraphTests.cs`
- `tests/FoodDiary.ArchitectureTests/FeatureStructureTests.cs`

## Follow-up

- Continue migrating modules incrementally when a concrete cross-boundary leak is identified.
- Record new enduring policies as new ADRs rather than appending module inventories to this record.
