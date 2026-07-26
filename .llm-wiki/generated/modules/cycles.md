---
id: generated.module.cycles
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# Cycles

## Graph

- Origin: module-graph
- Dependencies: Users
- Consumers: Dashboard, Export

## Source Areas

- `FoodDiary.Application.Abstractions/Cycles`
- `FoodDiary.Application/Cycles`
- `FoodDiary.Infrastructure/Persistence/Configurations/Cycles`
- `FoodDiary.Presentation.Api/Features/Cycles`
- `tests/FoodDiary.Application.Tests/Cycles`

## HTTP Surface

### CyclesController

Source: `FoodDiary.Presentation.Api/Features/Cycles/CyclesController.cs`

- `GET /api/v{version:apiVersion}/cycles/current`
- `GET /api/v{version:apiVersion}/cycles/current/nutrition-summary`
- `POST /api/v{version:apiVersion}/cycles`
- `PUT /api/v{version:apiVersion}/cycles/{cycleProfileId:guid}/days`
- `DELETE /api/v{version:apiVersion}/cycles/{cycleProfileId:guid}/days`
- `PUT /api/v{version:apiVersion}/cycles/{cycleProfileId:guid}/factors`

## Focused Tests

- `tests/FoodDiary.Application.Tests/Cycles/CyclesFeatureTests.CreateAndRead.cs`
- `tests/FoodDiary.Application.Tests/Cycles/CyclesFeatureTests.cs`
- `tests/FoodDiary.Application.Tests/Cycles/CyclesFeatureTests.DayCommands.cs`
- `tests/FoodDiary.Application.Tests/Cycles/CyclesFeatureTests.FactorCommands.cs`
- `tests/FoodDiary.Application.Tests/Cycles/CyclesFeatureTests.MappingAndPrediction.cs`
- `tests/FoodDiary.Application.Tests/Cycles/CyclesFeatureTests.NutritionSummary.cs`
- `tests/FoodDiary.Application.Tests/Cycles/CyclesValidatorTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/CyclesControllerCoverageTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
