---
id: generated.module.recipes
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# Recipes

## Graph

- Origin: module-graph
- Dependencies: FavoriteRecipes, Images, Nutrition, RecentItems, Users
- Consumers: none

## Source Areas

- `FoodDiary.Application.Abstractions/Recipes`
- `FoodDiary.Application/Recipes`
- `FoodDiary.Domain/Entities/Recipes`
- `FoodDiary.Infrastructure/Persistence/Configurations/Recipes`
- `FoodDiary.Infrastructure/Persistence/Recipes`
- `FoodDiary.Presentation.Api/Features/Recipes`
- `FoodDiary.Web.Client/src/app/features/recipes`
- `tests/FoodDiary.Application.Tests/Recipes`

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

## Focused Tests

- `tests/FoodDiary.Application.Tests/Recipes/CreateRecipeCommandValidatorTests.cs`
- `tests/FoodDiary.Application.Tests/Recipes/ExploreRecipesQueryValidatorTests.cs`
- `tests/FoodDiary.Application.Tests/Recipes/RecipeNutritionCalculatorTests.cs`
- `tests/FoodDiary.Application.Tests/Recipes/RecipesAdditionalValidatorTests.cs`
- `tests/FoodDiary.Application.Tests/Recipes/RecipesFeatureTests.CreateCommandTests.cs`
- `tests/FoodDiary.Application.Tests/Recipes/RecipesFeatureTests.cs`
- `tests/FoodDiary.Application.Tests/Recipes/RecipesFeatureTests.DeleteCommandTests.cs`
- `tests/FoodDiary.Application.Tests/Recipes/RecipesFeatureTests.DuplicateCommandTests.cs`
- `tests/FoodDiary.Application.Tests/Recipes/RecipesFeatureTests.NutritionAndIngredientTests.cs`
- `tests/FoodDiary.Application.Tests/Recipes/RecipesFeatureTests.ReadQueryTests.cs`
- `tests/FoodDiary.Application.Tests/Recipes/RecipesFeatureTests.UpdateCommandTests.cs`
- `tests/FoodDiary.Application.Tests/Recipes/UpdateRecipeCommandHandlerTests.cs`
- `tests/FoodDiary.Application.Tests/Recipes/UpdateRecipeCommandHandlerTests.Media.cs`
- `tests/FoodDiary.Application.Tests/Recipes/UpdateRecipeCommandHandlerTests.NestedIngredients.cs`
- `tests/FoodDiary.Application.Tests/Recipes/UpdateRecipeCommandHandlerTests.UpdateFlow.cs`
- `tests/FoodDiary.Application.Tests/Recipes/UpdateRecipeCommandHandlerTests.Validation.cs`
- `tests/FoodDiary.Application.Tests/Recipes/UpdateRecipeCommandValidatorTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
