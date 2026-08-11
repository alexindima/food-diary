---
id: generated.module.wearables
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# Wearables

## Graph

- Origin: module-graph
- Business-module dependencies: Users
- Abstraction-contract dependencies: Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Integrations, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/Wearables`
- `FoodDiary.Application/Wearables`
- `FoodDiary.Domain/Entities/Wearables`
- `FoodDiary.Infrastructure/Persistence/Configurations/Wearables`
- `FoodDiary.Infrastructure/Persistence/Wearables`
- `FoodDiary.Presentation.Api/Features/Wearables`

## HTTP Surface

### WearablesController

Source: `FoodDiary.Presentation.Api/Features/Wearables/WearablesController.cs`

- `GET /api/v{version:apiVersion}/wearables/connections`
- `GET /api/v{version:apiVersion}/wearables/{provider}/auth-url`
- `POST /api/v{version:apiVersion}/wearables/{provider}/connect`
- `DELETE /api/v{version:apiVersion}/wearables/{provider}/disconnect`
- `POST /api/v{version:apiVersion}/wearables/{provider}/sync`
- `GET /api/v{version:apiVersion}/wearables/daily-summary`

## Boundary Health

- Role: aggregate-owner
- Physical isolation: folder
- Architecture guardrails: graph-only
- Declared owned entities: not yet enumerated
- Public contract files: 16
- Observed external consumer groups: 3
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 16
- Interfaces: 10
- DTO/read-model/projection types: 3
- Enums: 0
- Exported repository-shaped contracts: 7
- Contracts referencing domain entities: 4
- `class WearableErrors`
- `interface IWearableClient`
- `interface IWearableConnectionReadRepository`
- `interface IWearableConnectionRepository`
- `interface IWearableConnectionWriteRepository`
- `interface IWearableOAuthStateService`
- `interface IWearableSyncReadModelRepository`
- `interface IWearableSyncReadRepository`
- `interface IWearableSyncRepository`
- `interface IWearableSyncWriteRepository`
- `interface IWearableTokenProtector`
- `record WearableConnectionModel`
- `record WearableDailySummaryModel`
- `record WearableDataPoint`
- `record WearableSyncEntryReadModel`
- `record WearableTokenResult`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Wearables/WearablesFeatureTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/WearablesControllerTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
