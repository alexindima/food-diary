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
- Abstraction-contract dependencies: none observed
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Modules.Dashboard.Application, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

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
- Public contract files: 0
- Observed external consumer groups: 5
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 0
- Interfaces: 0
- DTO/read-model/projection types: 0
- Enums: 0
- Exported repository-shaped contracts: 0
- Contracts referencing domain entities: 0
- No public declaration was found in the mapped abstraction areas.

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/Statistics/tests/FoodDiary.Modules.Statistics.Application.Tests/Statistics/StatisticsFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Statistics/tests/FoodDiary.Modules.Statistics.Application.Tests/Statistics/StatisticsSummaryFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Statistics/tests/FoodDiary.Modules.Statistics.Application.Tests/Statistics/UtcDateNormalizerTests.cs`
- [behavioral-or-text-match] `Modules/Statistics/tests/FoodDiary.Modules.Statistics.Application.Tests/Support/ResultAssert.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/StatisticsModuleExtractionTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/StatisticsControllerTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/StatisticsHttpMappingsTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
