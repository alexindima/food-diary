---
id: generated.module.favorite-recipes
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# FavoriteRecipes

## Graph

- Origin: module-graph
- Dependencies: Users
- Consumers: Recipes

## Source Areas

- `FoodDiary.Application.Abstractions/FavoriteRecipes`
- `FoodDiary.Application/FavoriteRecipes`
- `FoodDiary.Domain/Entities/FavoriteRecipes`
- `FoodDiary.Infrastructure/Persistence/FavoriteRecipes`
- `FoodDiary.Presentation.Api/Features/FavoriteRecipes`
- `tests/FoodDiary.Application.Tests/FavoriteRecipes`

## HTTP Surface

### FavoriteRecipesController

Source: `FoodDiary.Presentation.Api/Features/FavoriteRecipes/FavoriteRecipesController.cs`

- `GET /api/v{version:apiVersion}/favorite-recipes`
- `GET /api/v{version:apiVersion}/favorite-recipes/check/{recipeId:guid}`
- `POST /api/v{version:apiVersion}/favorite-recipes`
- `DELETE /api/v{version:apiVersion}/favorite-recipes/{id:guid}`

## Focused Tests

- `tests/FoodDiary.Application.Tests/FavoriteRecipes/FavoriteRecipesAdditionalFeatureTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/FavoriteRecipesControllerTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
