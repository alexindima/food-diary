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
- Abstraction-contract dependencies: Audit, BodyMetrics, Cycles, DailyAdvices, Dietologist, Exercises, Fasting, Hydration, Identity, Meals, Tdee, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/Dashboard/Application`
- `Modules/Dashboard/Application.Abstractions`
- `Modules/Dashboard/Contracts`
- `Modules/Dashboard/Presentation`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: read-composer
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: not yet enumerated
- Public contract files: 27
- Observed external consumer groups: 3
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 27
- Interfaces: 4
- DTO/read-model/projection types: 20
- Enums: 0
- Exported repository-shaped contracts: 0
- Contracts referencing domain entities: 0
- `interface IDashboardBodyReadService`
- `interface IDashboardMealsReadService`
- `interface IDashboardReadService`
- `interface IDashboardStatisticsReadService`
- `record DailyCaloriesModel`
- `record DashboardBodyReadModel`
- `record DashboardCalendarRange`
- `record DashboardMealAiItemReadModel`
- `record DashboardMealAiSessionReadModel`
- `record DashboardMealItemReadModel`
- `record DashboardMealReadModel`
- `record DashboardMealsModel`
- `record DashboardMealsReadModel`
- `record DashboardReadModel`
- `record DashboardReadSections`
- `record DashboardSnapshotModel`
- `record DashboardStatisticsBucketReadModel`
- `record DashboardStatisticsModel`
- `record DashboardWaistModel`
- `record DashboardWaistPointReadModel`
- `record DashboardWaistSummaryReadModel`
- `record DashboardWeightModel`
- `record DashboardWeightPointReadModel`
- `record DashboardWeightSummaryReadModel`
- `record GetDietologistClientDashboardQuery`
- `record WaistPointModel`
- `record WeightPointModel`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [integration] `Hosts/tests/FoodDiary.Web.Api.IntegrationTests/DashboardTimeZoneIntegrationTests.cs`
- [behavioral-or-text-match] `Modules/Dashboard/tests/FoodDiary.Modules.Dashboard.Application.Tests/ApplicationDependencyInjectionTests.cs`
- [behavioral-or-text-match] `Modules/Dashboard/tests/FoodDiary.Modules.Dashboard.Application.Tests/ComposedDashboardReadServiceTests.cs`
- [behavioral-or-text-match] `Modules/Dashboard/tests/FoodDiary.Modules.Dashboard.Application.Tests/DashboardCalendarTests.cs`
- [behavioral-or-text-match] `Modules/Dashboard/tests/FoodDiary.Modules.Dashboard.Application.Tests/DashboardCompositionTests.cs`
- [behavioral-or-text-match] `Modules/Dashboard/tests/FoodDiary.Modules.Dashboard.Application.Tests/DashboardFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Dashboard/tests/FoodDiary.Modules.Dashboard.Application.Tests/DashboardSnapshotBuilderTests.cs`
- [behavioral-or-text-match] `Modules/Dashboard/tests/FoodDiary.Modules.Dashboard.Application.Tests/DashboardValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Dashboard/tests/FoodDiary.Modules.Dashboard.Application.Tests/FastingContractsGlobalUsings.cs`
- [behavioral-or-text-match] `Modules/Dashboard/tests/FoodDiary.Modules.Dashboard.Application.Tests/SenderStatisticsFixture.cs`
- [behavioral-or-text-match] `Modules/Dashboard/tests/FoodDiary.Modules.Dashboard.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/Dashboard/tests/FoodDiary.Modules.Dashboard.Application.Tests/Time/UtcDateNormalizerTests.cs`
- [presentation] `Modules/Dashboard/tests/FoodDiary.Modules.Dashboard.Presentation.Tests/DashboardControllerTests.cs`
- [presentation] `Modules/Dashboard/tests/FoodDiary.Modules.Dashboard.Presentation.Tests/DashboardHttpMappingsTests.cs`
- [integration] `Platform/tests/FoodDiary.Infrastructure.IntegrationTests/Integration/DashboardBodyReadServiceIntegrationTests.cs`
- [behavioral-or-text-match] `Platform/tests/FoodDiary.Infrastructure.Tests/Persistence/Dashboard/DashboardBodyReadServiceTests.cs`
- [behavioral-or-text-match] `Platform/tests/FoodDiary.Infrastructure.Tests/Persistence/Dashboard/DashboardMealsReadServiceTests.cs`
- [behavioral-or-text-match] `Platform/tests/FoodDiary.Infrastructure.Tests/Persistence/Dashboard/DashboardStatisticsReadServiceTests.cs`
- [architecture-boundary] `Tooling/tests/FoodDiary.ArchitectureTests/DashboardModuleExtractionTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
