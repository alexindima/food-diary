---
id: generated.module.consumptions
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# Consumptions

## Graph

- Origin: module-graph
- Business-module dependencies: Images, Nutrition, Users
- Abstraction-contract dependencies: Achievements, Images, Meals, Products, RecentItems, Recipes, Users
- Business-module consumers: Dashboard, Export, FavoriteMeals, Gamification, Usda, WeeklyCheckIn, WeeklyGoals
- Host/adapter consumers: FoodDiary.Presentation.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/Consumptions`
- `FoodDiary.Application.Abstractions/Meals`
- `FoodDiary.Application/Consumptions`
- `FoodDiary.Domain/Entities/Meals`
- `FoodDiary.Infrastructure/Persistence/Configurations/Meals`
- `FoodDiary.Infrastructure/Persistence/Meals`
- `FoodDiary.Presentation.Api/Features/Consumptions`

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

## Boundary Health

- Role: aggregate-owner
- Physical isolation: folder
- Architecture guardrails: explicit-boundary-tests
- Declared owned entities: Meal, MealItem, MealAiSession, MealAiItem
- Public contract files: 6
- Observed external consumer groups: 8
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 6
- Exported repository-shaped contracts: 6
- `interface IMealActivityReadRepository`
- `interface IMealConsumptionReadRepository`
- `interface IMealProductNutritionReadRepository`
- `interface IMealReadRepository`
- `interface IMealRepository`
- `interface IMealWriteRepository`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Consumptions/ConsumptionsAdditionalValidatorTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Consumptions/ConsumptionsFeatureTests.CreateCommandTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Consumptions/ConsumptionsFeatureTests.MappingTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Consumptions/ConsumptionsFeatureTests.ReadQueryTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Consumptions/ConsumptionsFeatureTests.RepeatAndDeleteCommandTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Consumptions/ConsumptionsFeatureTests.UpdateCommandTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Consumptions/ConsumptionsFeatureTests.ValidatorAndCalculatorTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Consumptions/ConsumptionsFeatureTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Consumptions/CreateConsumptionCommandValidatorTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Consumptions/MealNutritionServiceTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Consumptions/UpdateConsumptionCommandValidatorTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
