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
- Extracted project: `Modules/Favorites/Application/FoodDiary.Application.Favorites.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: FavoriteMeals, FavoriteProducts, FavoriteRecipes, Meals, Products, Recipes, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/Favorites/Application`
- `Modules/Favorites/Application/Abstractions`
- `Modules/Favorites/Contracts`
- `Modules/Favorites/Domain`
- `Modules/Favorites/Infrastructure`
- `Modules/Favorites/Infrastructure/Model`
- `Modules/Favorites/Presentation`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: module-root
- Architecture guardrails: project-reference-matrix
- Declared owned entities: FavoriteMeal, FavoriteProduct, FavoriteRecipe
- Public contract files: 26
- Observed external consumer groups: 3
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

- [behavioral-or-text-match] `Modules/Favorites/tests/FoodDiary.Modules.Favorites.Application.Tests/FavoriteMeals/FavoriteMealReadServiceCoverageTests.cs`
- [behavioral-or-text-match] `Modules/Favorites/tests/FoodDiary.Modules.Favorites.Application.Tests/FavoriteMeals/FavoriteMealsFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Favorites/tests/FoodDiary.Modules.Favorites.Application.Tests/FavoriteMeals/FavoriteMealsValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Favorites/tests/FoodDiary.Modules.Favorites.Application.Tests/FavoriteProducts/FavoriteProductsAdditionalFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Favorites/tests/FoodDiary.Modules.Favorites.Application.Tests/FavoriteRecipes/FavoriteRecipesAdditionalFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Favorites/tests/FoodDiary.Modules.Favorites.Application.Tests/Favorites/FavoriteCommandValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Favorites/tests/FoodDiary.Modules.Favorites.Application.Tests/Favorites/FavoriteWriteContractTests.cs`
- [behavioral-or-text-match] `Modules/Favorites/tests/FoodDiary.Modules.Favorites.Application.Tests/FeatureErrorContractTests.cs`
- [behavioral-or-text-match] `Modules/Favorites/tests/FoodDiary.Modules.Favorites.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/Favorites/tests/FoodDiary.Modules.Favorites.Application.Tests/TestProductOverview.cs`
- [behavioral-or-text-match] `Modules/Favorites/tests/FoodDiary.Modules.Favorites.Application.Tests/TestRecipeOverview.cs`
- [behavioral-or-text-match] `Modules/Favorites/tests/FoodDiary.Modules.Favorites.Domain.Tests/ContentInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Favorites/tests/FoodDiary.Modules.Favorites.Domain.Tests/Domain/FavoriteIdConversionTests.cs`
- [behavioral-or-text-match] `Modules/Favorites/tests/FoodDiary.Modules.Favorites.Domain.Tests/FavoriteInvariantTests.cs`
- [presentation] `Modules/Favorites/tests/FoodDiary.Modules.Favorites.Presentation.Tests/FavoriteMealHttpMappingsTests.cs`
- [presentation] `Modules/Favorites/tests/FoodDiary.Modules.Favorites.Presentation.Tests/FavoriteProductHttpMappingsTests.cs`
- [presentation] `Modules/Favorites/tests/FoodDiary.Modules.Favorites.Presentation.Tests/FavoriteProductsControllerTests.cs`
- [presentation] `Modules/Favorites/tests/FoodDiary.Modules.Favorites.Presentation.Tests/FavoriteRecipeHttpMappingsTests.cs`
- [presentation] `Modules/Favorites/tests/FoodDiary.Modules.Favorites.Presentation.Tests/FavoriteRecipesControllerTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/FavoritesContractOwnershipTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/FavoritesModuleExtractionTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
