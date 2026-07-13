# ADR 0004: Application Abstractions Project

- Status: Accepted
- Date: 2026-05-21
- Owners: Backend architecture
- Related: ADR-0001, ADR-0006
- Supersedes: None

## Context
The primary application layer needs ports and shared application-facing models that infrastructure, integrations, resources, and other adapters can implement or consume.

Putting all ports directly in `FoodDiary.Application` would force more projects to reference concrete application implementation. Putting them in infrastructure would invert dependency direction.

## Decision Drivers

- Adapters need stable ports without depending on application implementation.
- Dependency direction must remain inward.
- Feature-specific contracts should not accumulate in a global shared bucket.

## Considered Options

1. Keep ports in `FoodDiary.Application`. Fewer projects, but adapters depend on application implementation.
2. Keep ports beside infrastructure implementations. Convenient locally, but reverses dependency direction.
3. Maintain a dedicated application-facing abstraction and model boundary.

## Decision
Keep `FoodDiary.Application.Abstractions` as the primary port/model boundary for the main FoodDiary backend.

Feature-specific abstractions should live near their feature. Only truly cross-feature contracts belong under `Common`.

## Consequences

### Positive

- Infrastructure and integrations can implement ports without depending on application implementation.
- Application implementation stays separated from adapter contracts.
- Architecture tests can prevent shared buckets from regrowing unintentionally.

### Negative

- Developers must choose between feature-local abstractions and `Common`.
- Some types move between projects when a contract becomes shared or feature-specific.

## Enforcement

- `tests/FoodDiary.ArchitectureTests/ApplicationAbstractionsBoundaryTests.cs`
- `tests/FoodDiary.ArchitectureTests/ProjectDependencyMatrixTests.cs`

## Follow-up

- None.
