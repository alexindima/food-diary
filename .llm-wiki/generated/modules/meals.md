---
id: generated.module.meals
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# Meals

## Graph

- Origin: extracted-project
- Extracted project: `FoodDiary.Application.Meals/FoodDiary.Application.Meals.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Achievements, FavoriteMeals, Images, Nutrition, Products, RecentItems, Recipes, Users
- Business-module consumers: Dashboard, Export
- Host/adapter consumers: FoodDiary.Application, FoodDiary.Application.Gamification, FoodDiary.Application.Usda, FoodDiary.Application.WeeklyCheckIn, FoodDiary.Application.WeeklyGoals, FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/Meals`
- `FoodDiary.Application.Meals`
- `FoodDiary.Domain/Entities/Meals`
- `FoodDiary.Infrastructure/Persistence/Configurations/Meals`
- `FoodDiary.Infrastructure/Persistence/Meals`
- `FoodDiary.Presentation.Api/Features/Meals`

## HTTP Surface

### MealsController

Source: `FoodDiary.Presentation.Api/Features/Meals/MealsController.cs`

- `GET /api/v{version:apiVersion}/meals/overview`
- `GET /api/v{version:apiVersion}/meals`
- `GET /api/v{version:apiVersion}/meals/{id:guid}`
- `POST /api/v{version:apiVersion}/meals`
- `PATCH /api/v{version:apiVersion}/meals/{id:guid}`
- `POST /api/v{version:apiVersion}/meals/{id:guid}/repeat`
- `DELETE /api/v{version:apiVersion}/meals/{id:guid}`

## Boundary Health

- Role: aggregate-owner
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: Meal, MealItem, MealAiSession, MealAiItem
- Public contract files: 15
- Observed external consumer groups: 11
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 15
- Interfaces: 7
- DTO/read-model/projection types: 6
- Enums: 0
- Exported repository-shaped contracts: 6
- Contracts referencing domain entities: 2
- `class MealErrors`
- `interface IMealActivityReadRepository`
- `interface IMealFavoriteReadService`
- `interface IMealProductNutritionReadRepository`
- `interface IMealProjectionReadRepository`
- `interface IMealReadRepository`
- `interface IMealRepository`
- `interface IMealWriteRepository`
- `record MealAiItemProjectionReadModel`
- `record MealAiSessionProjectionReadModel`
- `record MealFavoriteMealModel`
- `record MealItemProjectionReadModel`
- `record MealProductNutritionReadModel`
- `record MealProjectionReadModel`
- `record MealQueryFilters`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Meals/CreateMealCommandValidatorTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Meals/MealNutritionServiceTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Meals/MealsAdditionalValidatorTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Meals/MealsFeatureTests.CreateCommandTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Meals/MealsFeatureTests.MappingTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Meals/MealsFeatureTests.ReadQueryTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Meals/MealsFeatureTests.RepeatAndDeleteCommandTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Meals/MealsFeatureTests.UpdateCommandTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Meals/MealsFeatureTests.ValidatorAndCalculatorTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Meals/MealsFeatureTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Meals/UpdateMealCommandValidatorTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/MealsModuleExtractionTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
