---
id: generated.module.statistics
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# Statistics

## Graph

- Origin: extracted-project
- Extracted project: `FoodDiary.Application.Statistics/FoodDiary.Application.Statistics.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Dashboard, Users, WaistEntries, WeightEntries
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Application.Dashboard, FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/Dashboard`
- `FoodDiary.Application.Abstractions/WaistEntries`
- `FoodDiary.Application.Abstractions/WeightEntries`
- `FoodDiary.Application.Statistics`
- `FoodDiary.Presentation.Api/Features/Statistics`

## HTTP Surface

### StatisticsController

Source: `FoodDiary.Presentation.Api/Features/Statistics/StatisticsController.cs`

- `GET /api/v{version:apiVersion}/statistics`
- `GET /api/v{version:apiVersion}/statistics/summary`

## Boundary Health

- Role: read-composer
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: not yet enumerated
- Public contract files: 35
- Observed external consumer groups: 5
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 35
- Interfaces: 14
- DTO/read-model/projection types: 18
- Enums: 0
- Exported repository-shaped contracts: 8
- Contracts referencing domain entities: 4
- `class WaistEntryErrors`
- `class WeightEntryErrors`
- `interface IDashboardBodyReadService`
- `interface IDashboardMealsReadService`
- `interface IDashboardReadService`
- `interface IDashboardStatisticsReadService`
- `interface IWaistEntryReadModelRepository`
- `interface IWaistEntryReadRepository`
- `interface IWaistEntryReadService`
- `interface IWaistEntryRepository`
- `interface IWaistEntryWriteRepository`
- `interface IWeightEntryReadModelRepository`
- `interface IWeightEntryReadRepository`
- `interface IWeightEntryReadService`
- `interface IWeightEntryRepository`
- `interface IWeightEntryWriteRepository`
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
- `record WaistEntryModel`
- ... 5 more type(s)

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Statistics/StatisticsFeatureTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Statistics/StatisticsSummaryFeatureTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/StatisticsModuleExtractionTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/StatisticsControllerTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/StatisticsHttpMappingsTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
