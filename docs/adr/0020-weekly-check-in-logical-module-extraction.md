# ADR 0020: WeeklyCheckIn Logical Module Extraction

- Status: Accepted
- Date: 2026-08-30
- Owners: Backend architecture
- Related: ADR 0004, ADR 0009, ADR 0010, ADR 0016, ADR 0017, ADR 0019
- Supersedes: None

## Context

WeeklyCheckIn is a read composer that owns a query, summary/trend calculations, suggestions, and user-profile composition. It owns no domain aggregate, persistence port, EF mapping, repository implementation, background job, or separately consumed module contract. Its application project nevertheless remained at the horizontal repository root and depended on the Meals implementation assembly only to consume `IMealActivityReadService`.

Hydration already exposes its weekly totals through a stable Contracts project. Dashboard statistics, body metrics, and user profile reads already flow through Application Abstractions. A physical extraction should make WeeklyCheckIn ownership consistent without inventing empty layers or changing its HTTP contract.

## Decision Drivers

- Give WeeklyCheckIn one discoverable logical root while preserving runtime and HTTP behavior.
- Create projects only for types and adapters the module actually owns.
- Prevent cross-module read composition from depending on another module's implementation assembly.
- Preserve the existing WeeklyCheckIn application assembly and CLR namespaces for rebuilt consumers.
- Keep deployment topology, persistence model, and migration history unchanged.

## Considered Options

1. Keep the horizontal application project and document ownership only.
2. Create a symmetric Application, Contracts, Domain, and Infrastructure stack regardless of actual ownership.
3. Extract only the owned Application project and its tests, and move the shared meal-activity read contract to Application Abstractions.

## Decision

Adopt option 3.

- `Modules/WeeklyCheckIn` is the canonical logical root.
- `Application/FoodDiary.Modules.WeeklyCheckIn.Application.csproj` owns all WeeklyCheckIn application behavior and retains assembly name `FoodDiary.Application.WeeklyCheckIn` plus existing `FoodDiary.Application.WeeklyCheckIn.*` CLR namespaces.
- Do not create WeeklyCheckIn Contracts, Domain, Application Abstractions, Infrastructure, or persistence-model projects until a separately proven owned surface exists.
- Move WeeklyCheckIn-owned calculation, query, and application-service tests to `Modules/WeeklyCheckIn/tests/FoodDiary.Modules.WeeklyCheckIn.Application.Tests`.
- Move `IMealActivityReadService` from the Meals implementation assembly to `FoodDiary.Application.Abstractions/Meals/Common` and use namespace `FoodDiary.Application.Abstractions.Meals.Common`. Meals remains the implementation owner; WeeklyCheckIn, WeeklyGoals, and other read consumers compile against the abstraction assembly.
- WeeklyCheckIn consumes Hydration through `Modules/Hydration/Contracts` and dashboard statistics, meal activity, body metrics, and user profile data through Application Abstractions.
- Web API and Initializer remain composition roots and call the unchanged `AddWeeklyCheckInModule`. JobManager has no WeeklyCheckIn runtime behavior and drops its unused implementation reference.
- HTTP routes, request/response shapes, status codes, serialization, calculation behavior, database schema, migrations, and deployment topology do not change.

## Consequences

### Positive

- Physical layout matches actual ownership without empty symmetry projects.
- WeeklyCheckIn and WeeklyGoals no longer depend on Meals implementation for meal-activity reads.
- Module-owned tests follow the established extracted-module pattern.
- Existing WeeklyCheckIn transport and application consumers retain their CLR namespaces and assembly identity.

### Negative

- The meal-activity interface changes CLR namespace and all source consumers must be rebuilt together.
- WeeklyCheckIn remains intentionally dependent on central Application Abstractions and central Domain identifiers.
- HTTP presentation and composition roots remain horizontal by design.

## Enforcement

- `tests/FoodDiary.ArchitectureTests/WeeklyCheckInModuleExtractionTests.cs` verifies exclusive application ownership, approved references, absent unowned layers, and composition registration.
- `tests/FoodDiary.ArchitectureTests/ProjectDependencyMatrixTests.cs` governs production and test project edges.
- `docs/architecture/backend-modules.json` and `docs/architecture/module-dependencies.json` declare logical ownership and executable dependencies.
- Focused WeeklyCheckIn tests and central presentation tests protect calculation and HTTP mapping behavior.

## Follow-up

None.
