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
- Abstraction-contract dependencies: Ai, Favorites, Images, Nutrition, Products, RecentItems, Recipes, Usda, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/Meals/Application`
- `Modules/Meals/Application.Abstractions`
- `Modules/Meals/Contracts`
- `Modules/Meals/Domain`
- `Modules/Meals/Domain.Contracts`
- `Modules/Meals/Infrastructure`
- `Modules/Meals/PersistenceModel`
- `Modules/Meals/Presentation`
- `Modules/Meals/Service.Contracts`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: module-root
- Architecture guardrails: project-reference-matrix
- Declared owned entities: Meal, MealItem, MealAiSession, MealAiItem
- Public contract files: 35
- Observed external consumer groups: 3
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 35
- Interfaces: 14
- DTO/read-model/projection types: 11
- Enums: 0
- Exported repository-shaped contracts: 7
- Contracts referencing domain entities: 0
- `class MealErrors`
- `interface IMealAchievementEvaluationRequest`
- `interface IMealActivityReadRepository`
- `interface IMealDailyCalorieReadService`
- `interface IMealItemDisplayReadService`
- `interface IMealNutritionStatisticsReadService`
- `interface IMealProductNutritionQuery`
- `interface IMealProductNutritionReadRepository`
- `interface IMealProjectionReadRepository`
- `interface IMealReadRepository`
- `interface IMealRecognitionReceiptRepository`
- `interface IMealRecognitionTransactionRunner`
- `interface IMealRepository`
- `interface IMealSourceSnapshotQuery`
- `interface IMealWriteRepository`
- `record GetMealsQuery`
- `record MealAiItemModel`
- `record MealAiItemProjectionReadModel`
- `record MealAiSessionModel`
- `record MealAiSessionProjectionReadModel`
- `record MealDailyCalories`
- `record MealDaySummary`
- `record MealItemDisplayReadModel`
- `record MealItemModel`
- `record MealItemProjectionReadModel`
- `record MealModel`
- `record MealNutritionStatisticsBucket`
- `record MealProjectionReadModel`
- `record MealQueryFilters`
- `record MealRecipeSourceReadModel`
- ... 5 more type(s)

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/CreateMealCommandValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/CreateMealFromRecognitionTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/FeatureErrorContractTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/MealNutritionServiceTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/MealsAdditionalValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/MealsFeatureTests.CreateCommandTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/MealsFeatureTests.DaySummaryTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/MealsFeatureTests.MappingTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/MealsFeatureTests.ReadQueryTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/MealsFeatureTests.RepeatAndDeleteCommandTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/MealsFeatureTests.UpdateCommandTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/MealsFeatureTests.ValidatorAndCalculatorTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/MealsFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/Nutrition/NutritionMappingCompatibilityTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/ReadMealCountQueryHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/Support/AllowImageAssetAccessService.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/Support/RecordingImageAssetAccessService.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/TestProductOverview.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/TestRecipeOverview.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/UndoRecognizedMealTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/UpdateMealCommandValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/UtcDateNormalizerTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/Validation/EnumValueParserTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Domain.Tests/MealAiInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Domain.Tests/MealDomainEventLifecycleTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Domain.Tests/MealEnumContractTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Domain.Tests/MealExtractedInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Domain.Tests/MealIdInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Meals/tests/FoodDiary.Modules.Meals.Domain.Tests/MealInvariantTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
