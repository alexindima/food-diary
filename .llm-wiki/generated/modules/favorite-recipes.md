---
id: generated.module.favorite-recipes
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# FavoriteRecipes

## Graph

- Origin: module-graph
- Business-module dependencies: Users
- Abstraction-contract dependencies: Recipes, Users
- Business-module consumers: Recipes
- Host/adapter consumers: FoodDiary.Presentation.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/FavoriteRecipes`
- `FoodDiary.Application/FavoriteRecipes`
- `FoodDiary.Domain/Entities/FavoriteRecipes`
- `FoodDiary.Infrastructure/Persistence/FavoriteRecipes`
- `FoodDiary.Presentation.Api/Features/FavoriteRecipes`

## HTTP Surface

### FavoriteRecipesController

Source: `FoodDiary.Presentation.Api/Features/FavoriteRecipes/FavoriteRecipesController.cs`

- `GET /api/v{version:apiVersion}/favorite-recipes`
- `GET /api/v{version:apiVersion}/favorite-recipes/check/{recipeId:guid}`
- `POST /api/v{version:apiVersion}/favorite-recipes`
- `DELETE /api/v{version:apiVersion}/favorite-recipes/{id:guid}`

## Boundary Health

- Role: aggregate-owner
- Physical isolation: folder
- Architecture guardrails: graph-only
- Declared owned entities: not yet enumerated
- Public contract files: 6
- Observed external consumer groups: 2
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 6
- Interfaces: 4
- DTO/read-model/projection types: 1
- Enums: 0
- Exported repository-shaped contracts: 4
- Contracts referencing domain entities: 2
- `class FavoriteRecipeErrors`
- `interface IFavoriteRecipeReadModelRepository`
- `interface IFavoriteRecipeReadRepository`
- `interface IFavoriteRecipeRepository`
- `interface IFavoriteRecipeWriteRepository`
- `record FavoriteRecipeReadModel`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/FavoriteRecipes/FavoriteRecipesAdditionalFeatureTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/FavoriteRecipesControllerTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
