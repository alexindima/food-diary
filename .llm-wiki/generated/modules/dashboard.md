---
id: generated.module.dashboard
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# Dashboard

## Graph

- Origin: extracted-project
- Extracted project: `Modules/Dashboard/Application/FoodDiary.Modules.Dashboard.Application.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Audit, Authentication, Dietologist, Fasting, Users, WaistEntries, WeightEntries
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Presentation.Api/Features/Dashboard`
- `Modules/Dashboard/Application`
- `Modules/Dashboard/Application/Abstractions`
- `Modules/Dashboard/Contracts`
- `Modules/Dashboard/Infrastructure`

## HTTP Surface

### DashboardController

Source: `FoodDiary.Presentation.Api/Features/Dashboard/DashboardController.cs`

- `GET /api/v{version:apiVersion}/dashboard`
- `GET /api/v{version:apiVersion}/dashboard/advice`
- `POST /api/v{version:apiVersion}/dashboard/test-email`

## Boundary Health

- Role: read-composer
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: not yet enumerated
- Public contract files: 17
- Observed external consumer groups: 4
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 17
- Interfaces: 4
- DTO/read-model/projection types: 12
- Enums: 0
- Exported repository-shaped contracts: 0
- Contracts referencing domain entities: 0
- `interface IDashboardBodyReadService`
- `interface IDashboardMealsReadService`
- `interface IDashboardReadService`
- `interface IDashboardStatisticsReadService`
- `record DashboardBodyReadModel`
- `record DashboardMealAiItemReadModel`
- `record DashboardMealAiSessionReadModel`
- `record DashboardMealItemReadModel`
- `record DashboardMealReadModel`
- `record DashboardMealsReadModel`
- `record DashboardReadModel`
- `record DashboardReadSections`
- `record DashboardStatisticsBucketReadModel`
- `record DashboardWaistPointReadModel`
- `record DashboardWaistSummaryReadModel`
- `record DashboardWeightPointReadModel`
- `record DashboardWeightSummaryReadModel`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/Dashboard/tests/FoodDiary.Modules.Dashboard.Application.Tests/Dashboard/DashboardCompositionTests.cs`
- [behavioral-or-text-match] `Modules/Dashboard/tests/FoodDiary.Modules.Dashboard.Application.Tests/Dashboard/DashboardFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Dashboard/tests/FoodDiary.Modules.Dashboard.Application.Tests/Dashboard/DashboardSnapshotBuilderTests.cs`
- [behavioral-or-text-match] `Modules/Dashboard/tests/FoodDiary.Modules.Dashboard.Application.Tests/Dashboard/DashboardValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Dashboard/tests/FoodDiary.Modules.Dashboard.Application.Tests/Dashboard/SenderStatisticsFixture.cs`
- [behavioral-or-text-match] `Modules/Dashboard/tests/FoodDiary.Modules.Dashboard.Application.Tests/FastingContractsGlobalUsings.cs`
- [behavioral-or-text-match] `Modules/Dashboard/tests/FoodDiary.Modules.Dashboard.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/Dashboard/tests/FoodDiary.Modules.Dashboard.Infrastructure.Tests/Persistence/DashboardBodyReadServiceTests.cs`
- [behavioral-or-text-match] `Modules/Dashboard/tests/FoodDiary.Modules.Dashboard.Infrastructure.Tests/Persistence/DashboardMealsReadServiceTests.cs`
- [behavioral-or-text-match] `Modules/Dashboard/tests/FoodDiary.Modules.Dashboard.Infrastructure.Tests/Persistence/DashboardReadServiceTests.cs`
- [behavioral-or-text-match] `Modules/Dashboard/tests/FoodDiary.Modules.Dashboard.Infrastructure.Tests/Persistence/DashboardStatisticsReadServiceTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Dashboard/DashboardValidatorTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/DashboardModuleExtractionTests.cs`
- [integration] `tests/FoodDiary.Infrastructure.IntegrationTests/Integration/DashboardBodyReadServiceIntegrationTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/DashboardControllerTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/DashboardHttpMappingsTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
