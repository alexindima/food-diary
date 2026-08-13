---
id: generated.module.favorites
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# Favorites

## Graph

- Origin: extracted-project
- Extracted project: `FoodDiary.Application.Favorites/FoodDiary.Application.Favorites.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: FavoriteMeals, FavoriteProducts, FavoriteRecipes, Meals, Products, Recipes, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/FavoriteMeals`
- `FoodDiary.Application.Abstractions/FavoriteProducts`
- `FoodDiary.Application.Abstractions/FavoriteRecipes`
- `FoodDiary.Application.Favorites`
- `FoodDiary.Domain/Entities/FavoriteMeals`
- `FoodDiary.Domain/Entities/FavoriteProducts`
- `FoodDiary.Domain/Entities/FavoriteRecipes`
- `FoodDiary.Infrastructure/Persistence/Configurations/Favorites`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: FavoriteMeal, FavoriteProduct, FavoriteRecipe
- Public contract files: 26
- Observed external consumer groups: 4
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 26
- Interfaces: 16
- DTO/read-model/projection types: 7
- Enums: 0
- Exported repository-shaped contracts: 12
- Contracts referencing domain entities: 6
- `class FavoriteMealErrors`
- `class FavoriteProductErrors`
- `class FavoriteRecipeErrors`
- `interface IFavoriteMealReadModelRepository`
- `interface IFavoriteMealReadRepository`
- `interface IFavoriteMealReadService`
- `interface IFavoriteMealRepository`
- `interface IFavoriteMealSourceReadService`
- `interface IFavoriteMealWriteRepository`
- `interface IFavoriteProductReadModelRepository`
- `interface IFavoriteProductReadRepository`
- `interface IFavoriteProductReadService`
- `interface IFavoriteProductRepository`
- `interface IFavoriteProductWriteRepository`
- `interface IFavoriteRecipeReadModelRepository`
- `interface IFavoriteRecipeReadRepository`
- `interface IFavoriteRecipeReadService`
- `interface IFavoriteRecipeRepository`
- `interface IFavoriteRecipeWriteRepository`
- `record FavoriteMealModel`
- `record FavoriteMealReadModel`
- `record FavoriteMealSourceModel`
- `record FavoriteProductModel`
- `record FavoriteProductReadModel`
- `record FavoriteRecipeModel`
- `record FavoriteRecipeReadModel`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/FavoritesModuleExtractionTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
