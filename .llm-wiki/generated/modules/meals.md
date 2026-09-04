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
- Extracted project: `Modules/Meals/Application/FoodDiary.Modules.Meals.Application.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Achievements, FavoriteMeals, Images, Nutrition, Products, RecentItems, Recipes, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Modules.Dashboard.Application, FoodDiary.Presentation.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Presentation.Api/Features/Meals`
- `Modules/Meals/Application`
- `Modules/Meals/Application/Abstractions`
- `Modules/Meals/Contracts`

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
- Physical isolation: module-root
- Architecture guardrails: project-reference-matrix
- Declared owned entities: Meal, MealItem, MealAiSession, MealAiItem
- Public contract files: 18
- Observed external consumer groups: 2
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 18
- Interfaces: 10
- DTO/read-model/projection types: 6
- Enums: 0
- Exported repository-shaped contracts: 6
- Contracts referencing domain entities: 2
- `class MealErrors`
- `interface IMealActivityReadRepository`
- `interface IMealActivityReadService`
- `interface IMealExportReadService`
- `interface IMealFavoriteReadService`
- `interface IMealProductNutritionReadRepository`
- `interface IMealProductNutritionReadService`
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

- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/Meals/CreateMealCommandValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/Meals/MealNutritionServiceTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/Meals/MealsAdditionalValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/Meals/MealsFeatureTests.CreateCommandTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/Meals/MealsFeatureTests.MappingTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/Meals/MealsFeatureTests.ReadQueryTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/Meals/MealsFeatureTests.RepeatAndDeleteCommandTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/Meals/MealsFeatureTests.UpdateCommandTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/Meals/MealsFeatureTests.ValidatorAndCalculatorTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/Meals/MealsFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/Meals/UpdateMealCommandValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/Meals/UtcDateNormalizerTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/Support/AllowImageAssetAccessService.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/Support/RecordingImageAssetAccessService.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/TestProductOverview.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/TestRecipeOverview.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Domain.Tests/Domain/MealAiInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Domain.Tests/Domain/MealEnumContractTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Domain.Tests/Domain/MealExtractedInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Domain.Tests/Domain/MealIdInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Domain.Tests/Domain/MealInvariantTests.cs`
- [integration] `Modules/Meals/tests/FoodDiary.Modules.Meals.Infrastructure.IntegrationTests/Integration/MealRepositoryIntegrationTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/MealsModuleExtractionTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
