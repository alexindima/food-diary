# ADR 0026: Extract the RecentItems Bounded Context

- Status: Accepted
- Date: 2026-09-01
- Owners: Backend architecture
- Related: ADR 0007, ADR 0008, ADR 0016
- Supersedes: None

## Context

Recent product/recipe usage was a coherent responsibility split across central Domain, Application.Abstractions and Infrastructure. Products and Recipes read semantic usage projections, Meals records usage, and Users cleanup deletes the persistence graph. The aggregate has only a forward navigation to central User and no inverse User navigation.

## Decision Drivers

- Align physical ownership with the proven bounded context.
- Preserve CLR namespaces, HTTP behavior, EF identity and post-commit transaction semantics.
- Keep shared database history and User lifecycle orchestration central.
- Avoid an empty Application project without use cases.

## Considered Options

1. Keep the responsibility split across central projects.
2. Extract owner-only types, ports, adapters and tests while retaining explicit central seams.
3. Move User, consumer entities or cleanup orchestration with RecentItems.

## Decision

Select option 2. `Modules/RecentItems` owns `RecentItem`, `RecentItemType`, `RecentItemId`, narrow usage/repository abstractions, repository, post-commit recorder, DI and EF configuration. Existing CLR namespaces remain unchanged. Central Domain supplies User/UserId and shared IDs/primitives one-way; no inverse navigation is added.

Central `FoodDiaryDbContext`, migrations/snapshot, post-commit queue/UoW and Users cleanup orchestration remain central. The context explicitly applies the module persistence model. Products, Recipes and Meals reference only module abstractions. Hosts compose `AddRecentItemsModule`. There is no EF model or HTTP contract delta, so no migration or API snapshot is created.

## Consequences

Ownership and focused tests are cohesive, while module Domain/Infrastructure retain deliberate one-way dependencies on central compatibility seams. Precompiled consumers require coordinated rebuild because type assembly identity moves even though CLR namespaces remain stable.

## Enforcement

`RecentItemsModuleExtractionTests`, `ProjectDependencyMatrixTests`, module unit/PostgreSQL tests, EF pending-model checks and HTTP integration tests enforce this decision.
