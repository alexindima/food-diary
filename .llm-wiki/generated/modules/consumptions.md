---
id: generated.module.consumptions
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# Consumptions

## Graph

- Origin: module-graph
- Dependencies: Images, Nutrition, Users
- Consumers: Dashboard, Export, FavoriteMeals, Gamification, Usda, WeeklyCheckIn

## Source Areas

- `FoodDiary.Application.Abstractions/Consumptions`
- `FoodDiary.Application/Consumptions`
- `FoodDiary.Presentation.Api/Features/Consumptions`
- `tests/FoodDiary.Application.Tests/Consumptions`

## HTTP Surface

### ConsumptionsController

Source: `FoodDiary.Presentation.Api/Features/Consumptions/ConsumptionsController.cs`

- `GET /api/v{version:apiVersion}/consumptions/overview`
- `GET /api/v{version:apiVersion}/consumptions`
- `GET /api/v{version:apiVersion}/consumptions/{id:guid}`
- `POST /api/v{version:apiVersion}/consumptions`
- `PATCH /api/v{version:apiVersion}/consumptions/{id:guid}`
- `POST /api/v{version:apiVersion}/consumptions/{id:guid}/repeat`
- `DELETE /api/v{version:apiVersion}/consumptions/{id:guid}`

## Focused Tests

- `tests/FoodDiary.Application.Tests/Consumptions/ConsumptionsAdditionalValidatorTests.cs`
- `tests/FoodDiary.Application.Tests/Consumptions/ConsumptionsFeatureTests.CreateCommandTests.cs`
- `tests/FoodDiary.Application.Tests/Consumptions/ConsumptionsFeatureTests.MappingTests.cs`
- `tests/FoodDiary.Application.Tests/Consumptions/ConsumptionsFeatureTests.ReadQueryTests.cs`
- `tests/FoodDiary.Application.Tests/Consumptions/ConsumptionsFeatureTests.RepeatAndDeleteCommandTests.cs`
- `tests/FoodDiary.Application.Tests/Consumptions/ConsumptionsFeatureTests.UpdateCommandTests.cs`
- `tests/FoodDiary.Application.Tests/Consumptions/ConsumptionsFeatureTests.ValidatorAndCalculatorTests.cs`
- `tests/FoodDiary.Application.Tests/Consumptions/ConsumptionsFeatureTests.cs`
- `tests/FoodDiary.Application.Tests/Consumptions/CreateConsumptionCommandValidatorTests.cs`
- `tests/FoodDiary.Application.Tests/Consumptions/MealNutritionServiceTests.cs`
- `tests/FoodDiary.Application.Tests/Consumptions/UpdateConsumptionCommandValidatorTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
