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
- Abstraction-contract dependencies: Ai, FavoriteMeals, Images, Nutrition, Products, RecentItems, Recipes, Usda, Users
- Business-module consumers: none observed
- Host/adapter consumers: none observed
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/Meals/Application`
- `Modules/Meals/Application/Abstractions`
- `Modules/Meals/Contracts`
- `Modules/Meals/Domain`
- `Modules/Meals/Domain.Contracts`
- `Modules/Meals/Infrastructure`
- `Modules/Meals/Infrastructure/Model`
- `Modules/Meals/Presentation`
- `Modules/Meals/Presentation/Features/Meals`
- `Modules/Meals/Service.Contracts`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: module-root
- Architecture guardrails: project-reference-matrix
- Declared owned entities: Meal, MealItem, MealAiSession, MealAiItem
- Public contract files: 28
- Observed external consumer groups: 0
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 28
- Interfaces: 14
- DTO/read-model/projection types: 10
- Enums: 0
- Exported repository-shaped contracts: 7
- Contracts referencing domain entities: 3
- `class MealErrors`
- `interface IMealAchievementEvaluationRequest`
- `interface IMealActivityReadRepository`
- `interface IMealActivityReadService`
- `interface IMealDailyCalorieReadService`
- `interface IMealExportReadService`
- `interface IMealItemDisplayReadService`
- `interface IMealNutritionStatisticsReadService`
- `interface IMealProductNutritionReadRepository`
- `interface IMealProjectionReadRepository`
- `interface IMealReadRepository`
- `interface IMealRecognitionReceiptRepository`
- `interface IMealRecognitionTransactionRunner`
- `interface IMealRepository`
- `interface IMealWriteRepository`
- `record GetMealsQuery`
- `record MealAiItemModel`
- `record MealAiItemProjectionReadModel`
- `record MealAiSessionModel`
- `record MealAiSessionProjectionReadModel`
- `record MealDailyCalories`
- `record MealItemDisplayReadModel`
- `record MealItemModel`
- `record MealItemProjectionReadModel`
- `record MealModel`
- `record MealNutritionStatisticsBucket`
- `record MealProjectionReadModel`
- `record MealQueryFilters`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/FeatureErrorContractTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/MealActivityReadServiceTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/Meals/CreateMealCommandValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/Meals/CreateMealFromRecognitionTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/Meals/MealNutritionServiceTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/Meals/MealsAdditionalValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/Meals/MealsFeatureTests.CreateCommandTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/Meals/MealsFeatureTests.MappingTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/Meals/MealsFeatureTests.ReadQueryTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/Meals/MealsFeatureTests.RepeatAndDeleteCommandTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/Meals/MealsFeatureTests.UpdateCommandTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/Meals/MealsFeatureTests.ValidatorAndCalculatorTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/Meals/MealsFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/Meals/UndoRecognizedMealTests.cs`
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
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Domain.Tests/MealRecognitionReceiptTests.cs`
- [integration] `Modules/Meals/tests/FoodDiary.Modules.Meals.Infrastructure.IntegrationTests/Integration/MealItemDisplayReadServiceIntegrationTests.cs`
- [integration] `Modules/Meals/tests/FoodDiary.Modules.Meals.Infrastructure.IntegrationTests/Integration/MealRecognitionReceiptIntegrationTests.cs`
- [integration] `Modules/Meals/tests/FoodDiary.Modules.Meals.Infrastructure.IntegrationTests/Integration/MealRecognitionTransactionIntegrationTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
