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
- Extracted project: `Modules/Statistics/Application/FoodDiary.Modules.Statistics.Application.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Dashboard, Users, WaistEntries, WeightEntries
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/Dashboard/Application/Abstractions`
- `Modules/Statistics/Application`
- `Modules/Statistics/Presentation`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: read-composer
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: not yet enumerated
- Public contract files: 15
- Observed external consumer groups: 3
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 15
- Interfaces: 3
- DTO/read-model/projection types: 11
- Enums: 0
- Exported repository-shaped contracts: 0
- Contracts referencing domain entities: 0
- `interface IDashboardBodyReadService`
- `interface IDashboardMealsReadService`
- `interface IDashboardReadService`
- `record DashboardBodyReadModel`
- `record DashboardMealAiItemReadModel`
- `record DashboardMealAiSessionReadModel`
- `record DashboardMealItemReadModel`
- `record DashboardMealReadModel`
- `record DashboardMealsReadModel`
- `record DashboardReadModel`
- `record DashboardReadSections`
- `record DashboardWaistPointReadModel`
- `record DashboardWaistSummaryReadModel`
- `record DashboardWeightPointReadModel`
- `record DashboardWeightSummaryReadModel`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/Statistics/tests/FoodDiary.Modules.Statistics.Application.Tests/Queries/GetStatisticsQueryValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Statistics/tests/FoodDiary.Modules.Statistics.Application.Tests/Statistics/StatisticsFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Statistics/tests/FoodDiary.Modules.Statistics.Application.Tests/Statistics/StatisticsSummaryFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Statistics/tests/FoodDiary.Modules.Statistics.Application.Tests/Statistics/UtcDateNormalizerTests.cs`
- [behavioral-or-text-match] `Modules/Statistics/tests/FoodDiary.Modules.Statistics.Application.Tests/Support/ResultAssert.cs`
- [presentation] `Modules/Statistics/tests/FoodDiary.Modules.Statistics.Presentation.Tests/StatisticsControllerTests.cs`
- [presentation] `Modules/Statistics/tests/FoodDiary.Modules.Statistics.Presentation.Tests/StatisticsHttpMappingsTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/StatisticsModuleExtractionTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
