---
id: generated.module.recipes
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# Recipes

## Graph

- Origin: module-graph
- Business-module dependencies: FavoriteRecipes, Images, Nutrition, RecentItems, Users
- Abstraction-contract dependencies: Images, Products, RecentItems, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Presentation.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/Recipes`
- `FoodDiary.Application/Recipes`
- `FoodDiary.Domain/Entities/Recipes`
- `FoodDiary.Infrastructure/Persistence/Configurations/Recipes`
- `FoodDiary.Infrastructure/Persistence/Recipes`
- `FoodDiary.Presentation.Api/Features/Recipes`

## HTTP Surface

### RecipeExploreController

Source: `FoodDiary.Presentation.Api/Features/Recipes/RecipeExploreController.cs`

- `GET /api/v{version:apiVersion}/recipes/explore`

### RecipesController

Source: `FoodDiary.Presentation.Api/Features/Recipes/RecipesController.cs`

- `GET /api/v{version:apiVersion}/recipes`
- `GET /api/v{version:apiVersion}/recipes/overview`
- `GET /api/v{version:apiVersion}/recipes/recent`
- `GET /api/v{version:apiVersion}/recipes/{id:guid}`
- `POST /api/v{version:apiVersion}/recipes`
- `PATCH /api/v{version:apiVersion}/recipes/{id:guid}`
- `DELETE /api/v{version:apiVersion}/recipes/{id:guid}`
- `POST /api/v{version:apiVersion}/recipes/{id:guid}/duplicate`

## Boundary Health

- Role: aggregate-owner
- Physical isolation: folder
- Architecture guardrails: explicit-boundary-tests
- Declared owned entities: Recipe, RecipeIngredient, RecipeStep
- Public contract files: 7
- Observed external consumer groups: 1
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 7
- Exported repository-shaped contracts: 3
- `interface IRecipeAccessService`
- `interface IRecipeLookupService`
- `interface IRecipeNutritionWriter`
- `interface IRecipeOverviewReadService`
- `interface IRecipeReadRepository`
- `interface IRecipeRepository`
- `interface IRecipeWriteRepository`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Recipes/CreateRecipeCommandValidatorTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Recipes/ExploreRecipesQueryValidatorTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Recipes/RecipeNutritionCalculatorTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Recipes/RecipesAdditionalValidatorTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Recipes/RecipesFeatureTests.CreateCommandTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Recipes/RecipesFeatureTests.DeleteCommandTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Recipes/RecipesFeatureTests.DuplicateCommandTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Recipes/RecipesFeatureTests.NutritionAndIngredientTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Recipes/RecipesFeatureTests.ReadQueryTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Recipes/RecipesFeatureTests.UpdateCommandTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Recipes/RecipesFeatureTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Recipes/UpdateRecipeCommandHandlerTests.Media.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Recipes/UpdateRecipeCommandHandlerTests.NestedIngredients.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Recipes/UpdateRecipeCommandHandlerTests.UpdateFlow.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Recipes/UpdateRecipeCommandHandlerTests.Validation.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Recipes/UpdateRecipeCommandHandlerTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Recipes/UpdateRecipeCommandValidatorTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
