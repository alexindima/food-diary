---
id: generated.module.statistics
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# Statistics

## Graph

- Origin: module-graph
- Dependencies: Users
- Consumers: Dashboard

## Source Areas

- `FoodDiary.Application/Statistics`
- `FoodDiary.Presentation.Api/Features/Statistics`
- `FoodDiary.Web.Client/src/app/features/statistics`
- `tests/FoodDiary.Application.Tests/Statistics`

## HTTP Surface

### StatisticsController

Source: `FoodDiary.Presentation.Api/Features/Statistics/StatisticsController.cs`

- `GET /api/v{version:apiVersion}/statistics`

## Focused Tests

- `tests/FoodDiary.Application.Tests/Statistics/StatisticsFeatureTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/StatisticsHttpMappingsTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
