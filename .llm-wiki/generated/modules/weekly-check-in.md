---
id: generated.module.weekly-check-in
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# WeeklyCheckIn

## Graph

- Origin: module-graph
- Dependencies: Consumptions, Hydration, Users, WaistEntries, WeightEntries
- Consumers: none

## Source Areas

- `FoodDiary.Application/WeeklyCheckIn`
- `FoodDiary.Presentation.Api/Features/WeeklyCheckIn`
- `tests/FoodDiary.Application.Tests/WeeklyCheckIn`

## HTTP Surface

### WeeklyCheckInController

Source: `FoodDiary.Presentation.Api/Features/WeeklyCheckIn/WeeklyCheckInController.cs`

- `GET /api/v{version:apiVersion}/weekly-check-in`

## Focused Tests

- `tests/FoodDiary.Application.Tests/WeeklyCheckIn/WeeklyCheckInCalculatorTests.cs`
- `tests/FoodDiary.Application.Tests/WeeklyCheckIn/WeeklyCheckInFeatureTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/WeeklyCheckInHttpMappingsTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
