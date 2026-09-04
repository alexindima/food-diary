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

- Origin: extracted-project
- Extracted project: `Modules/Wearables/Application/FoodDiary.Application.Wearables.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.Integrations, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Integrations/Wearables`
- `FoodDiary.Presentation.Api/Features/Wearables`
- `Modules/Wearables/Application`
- `Modules/Wearables/Application/Abstractions`

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
- Physical isolation: project
- Architecture guardrails: assembly-isolated
- Declared owned entities: WearableConnection, WearableSyncEntry
- Public contract files: 18
- Observed external consumer groups: 4
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 18
- Interfaces: 11
- DTO/read-model/projection types: 3
- Enums: 0
- Exported repository-shaped contracts: 7
- Contracts referencing domain entities: 4
- `class WearableErrors`
- `class WearableInputLimits`
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
- `interface IWearableTransactionRunner`
- `record WearableConnectionModel`
- `record WearableDailySummaryModel`
- `record WearableDataPoint`
- `record WearableSyncEntryReadModel`
- `record WearableTokenResult`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/Wearables/tests/FoodDiary.Modules.Wearables.Application.Tests/FeatureErrorContractTests.cs`
- [behavioral-or-text-match] `Modules/Wearables/tests/FoodDiary.Modules.Wearables.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/Wearables/tests/FoodDiary.Modules.Wearables.Application.Tests/Wearables/WearableDateValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Wearables/tests/FoodDiary.Modules.Wearables.Application.Tests/Wearables/WearablesFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Wearables/tests/FoodDiary.Modules.Wearables.Domain.Tests/Domain/WearableIdConversionTests.cs`
- [behavioral-or-text-match] `Modules/Wearables/tests/FoodDiary.Modules.Wearables.Domain.Tests/Domain/WearableInvariantTests.cs`
- [integration] `Modules/Wearables/tests/FoodDiary.Modules.Wearables.Infrastructure.IntegrationTests/PostgresDatabaseCollection.cs`
- [integration] `Modules/Wearables/tests/FoodDiary.Modules.Wearables.Infrastructure.IntegrationTests/PostgresDatabaseFixture.cs`
- [integration] `Modules/Wearables/tests/FoodDiary.Modules.Wearables.Infrastructure.IntegrationTests/WearableTransactionRunnerIntegrationTests.cs`
- [behavioral-or-text-match] `Modules/Wearables/tests/FoodDiary.Modules.Wearables.Infrastructure.Tests/Authentication/WearableOAuthStateServiceTests.cs`
- [behavioral-or-text-match] `Modules/Wearables/tests/FoodDiary.Modules.Wearables.Infrastructure.Tests/LegacyTokenUpgradeTests.cs`
- [behavioral-or-text-match] `Modules/Wearables/tests/FoodDiary.Modules.Wearables.Infrastructure.Tests/ModuleRegistrationTests.cs`
- [behavioral-or-text-match] `Modules/Wearables/tests/FoodDiary.Modules.Wearables.Infrastructure.Tests/Services/WearableTokenProtectorTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/WearablesModuleBoundaryTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/WearablesModuleExtractionTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/WearablesControllerTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
