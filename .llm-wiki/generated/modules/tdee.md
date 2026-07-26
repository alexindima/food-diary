---
id: generated.module.tdee
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# Tdee

## Graph

- Origin: module-graph
- Dependencies: Exercises, Users, WeightEntries
- Consumers: Dashboard

## Source Areas

- `FoodDiary.Application/Tdee`
- `FoodDiary.Presentation.Api/Features/Tdee`
- `tests/FoodDiary.Application.Tests/Tdee`

## HTTP Surface

### TdeeController

Source: `FoodDiary.Presentation.Api/Features/Tdee/TdeeController.cs`

- `GET /api/v{version:apiVersion}/tdee`

## Focused Tests

- `tests/FoodDiary.Application.Tests/Tdee/TdeeCalculatorTests.cs`
- `tests/FoodDiary.Application.Tests/Tdee/TdeeFeatureTests.cs`
- `tests/FoodDiary.Application.Tests/Tdee/TdeeValidatorTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/TdeeControllerTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/TdeeHttpMappingsTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
