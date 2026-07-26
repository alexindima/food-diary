---
id: generated.module.dashboard
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# Dashboard

## Graph

- Origin: module-graph
- Dependencies: Consumptions, Cycles, DailyAdvices, Exercises, Fasting, Hydration, Statistics, Tdee, Users, WaistEntries, WeightEntries
- Consumers: Dietologist

## Source Areas

- `FoodDiary.Application.Abstractions/Dashboard`
- `FoodDiary.Application/Dashboard`
- `FoodDiary.Infrastructure/Persistence/Dashboard`
- `FoodDiary.Presentation.Api/Features/Dashboard`
- `FoodDiary.Web.Client/src/app/features/dashboard`
- `tests/FoodDiary.Application.Tests/Dashboard`

## HTTP Surface

### DashboardController

Source: `FoodDiary.Presentation.Api/Features/Dashboard/DashboardController.cs`

- `GET /api/v{version:apiVersion}/dashboard`
- `GET /api/v{version:apiVersion}/dashboard/advice`
- `POST /api/v{version:apiVersion}/dashboard/test-email`

## Focused Tests

- `tests/FoodDiary.Application.Tests/Dashboard/DashboardFeatureTests.cs`
- `tests/FoodDiary.Application.Tests/Dashboard/DashboardSnapshotBuilderTests.cs`
- `tests/FoodDiary.Application.Tests/Dashboard/DashboardValidatorTests.cs`
- `tests/FoodDiary.Infrastructure.Tests/Persistence/DashboardBodyReadServiceTests.cs`
- `tests/FoodDiary.Infrastructure.Tests/Persistence/DashboardMealsReadServiceTests.cs`
- `tests/FoodDiary.Infrastructure.Tests/Persistence/DashboardReadServiceTests.cs`
- `tests/FoodDiary.Infrastructure.Tests/Persistence/DashboardStatisticsReadServiceTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/DashboardControllerTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/DashboardHttpMappingsTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
