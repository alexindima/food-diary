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

- Origin: extracted-project
- Extracted project: `Modules/Recipes/Application/FoodDiary.Modules.Recipes.Application.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Favorites, Images, Nutrition, Products, RecentItems, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/Recipes/Application`
- `Modules/Recipes/Application.Abstractions`
- `Modules/Recipes/Contracts`
- `Modules/Recipes/PersistenceModel`
- `Modules/Recipes/Presentation`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: Recipe, RecipeIngredient, RecipeStep
- Public contract files: 14
- Observed external consumer groups: 3
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 14
- Interfaces: 9
- DTO/read-model/projection types: 0
- Enums: 0
- Exported repository-shaped contracts: 3
- Contracts referencing domain entities: 0
- `class RecipeErrors`
- `interface IRecipeAccessService`
- `interface IRecipeLookupService`
- `interface IRecipeMutationTransactionRunner`
- `interface IRecipeNutritionWriter`
- `interface IRecipeOverviewReadService`
- `interface IRecipeReadRepository`
- `interface IRecipeRepository`
- `interface IRecipeUsageQuery`
- `interface IRecipeWriteRepository`
- `record RecipeOverviewIngredientReadItem`
- `record RecipeOverviewReadItem`
- `record RecipeOverviewStepReadItem`
- `record RecipeQueryFilters`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/Recipes/tests/FoodDiary.Modules.Recipes.Application.Tests/ApplicationDependencyInjectionTests.cs`
- [behavioral-or-text-match] `Modules/Recipes/tests/FoodDiary.Modules.Recipes.Application.Tests/CentralRelocated/RecipesFeatureTests.CreateCommandTests.cs`
- [behavioral-or-text-match] `Modules/Recipes/tests/FoodDiary.Modules.Recipes.Application.Tests/CentralRelocated/RecipesFeatureTests.DeleteCommandTests.cs`
- [behavioral-or-text-match] `Modules/Recipes/tests/FoodDiary.Modules.Recipes.Application.Tests/CentralRelocated/RecipesFeatureTests.DuplicateCommandTests.cs`
- [behavioral-or-text-match] `Modules/Recipes/tests/FoodDiary.Modules.Recipes.Application.Tests/CentralRelocated/RecipesFeatureTests.NutritionAndIngredientTests.cs`
- [behavioral-or-text-match] `Modules/Recipes/tests/FoodDiary.Modules.Recipes.Application.Tests/CentralRelocated/RecipesFeatureTests.ReadQueryTests.cs`
- [behavioral-or-text-match] `Modules/Recipes/tests/FoodDiary.Modules.Recipes.Application.Tests/CentralRelocated/RecipesFeatureTests.UpdateCommandTests.cs`
- [behavioral-or-text-match] `Modules/Recipes/tests/FoodDiary.Modules.Recipes.Application.Tests/CentralRelocated/RecipesFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Recipes/tests/FoodDiary.Modules.Recipes.Application.Tests/CreateRecipeCommandValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Recipes/tests/FoodDiary.Modules.Recipes.Application.Tests/ExploreRecipesQueryValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Recipes/tests/FoodDiary.Modules.Recipes.Application.Tests/FeatureErrorContractTests.cs`
- [behavioral-or-text-match] `Modules/Recipes/tests/FoodDiary.Modules.Recipes.Application.Tests/Nutrition/NutritionMappingCompatibilityTests.cs`
- [behavioral-or-text-match] `Modules/Recipes/tests/FoodDiary.Modules.Recipes.Application.Tests/RecipeNutritionCalculatorTests.cs`
- [behavioral-or-text-match] `Modules/Recipes/tests/FoodDiary.Modules.Recipes.Application.Tests/RecipesAdditionalValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Recipes/tests/FoodDiary.Modules.Recipes.Application.Tests/Support/AllowImageAssetAccessService.cs`
- [behavioral-or-text-match] `Modules/Recipes/tests/FoodDiary.Modules.Recipes.Application.Tests/Support/RecordingImageAssetAccessService.cs`
- [behavioral-or-text-match] `Modules/Recipes/tests/FoodDiary.Modules.Recipes.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/Recipes/tests/FoodDiary.Modules.Recipes.Application.Tests/TestProductOverview.cs`
- [behavioral-or-text-match] `Modules/Recipes/tests/FoodDiary.Modules.Recipes.Application.Tests/TestRecipeOverview.cs`
- [behavioral-or-text-match] `Modules/Recipes/tests/FoodDiary.Modules.Recipes.Application.Tests/UpdateRecipeCommandHandlerTests.Media.cs`
- [behavioral-or-text-match] `Modules/Recipes/tests/FoodDiary.Modules.Recipes.Application.Tests/UpdateRecipeCommandHandlerTests.NestedIngredients.cs`
- [behavioral-or-text-match] `Modules/Recipes/tests/FoodDiary.Modules.Recipes.Application.Tests/UpdateRecipeCommandHandlerTests.UpdateFlow.cs`
- [behavioral-or-text-match] `Modules/Recipes/tests/FoodDiary.Modules.Recipes.Application.Tests/UpdateRecipeCommandHandlerTests.Validation.cs`
- [behavioral-or-text-match] `Modules/Recipes/tests/FoodDiary.Modules.Recipes.Application.Tests/UpdateRecipeCommandHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Recipes/tests/FoodDiary.Modules.Recipes.Application.Tests/UpdateRecipeCommandValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Recipes/tests/FoodDiary.Modules.Recipes.Domain.Tests/RecipeInvariantAndEventsTests.cs`
- [behavioral-or-text-match] `Modules/Recipes/tests/FoodDiary.Modules.Recipes.Domain.Tests/RecipesIdConversionTests.cs`
- [presentation] `Modules/Recipes/tests/FoodDiary.Modules.Recipes.Presentation.Tests/RecipeExploreControllerTests.cs`
- [presentation] `Modules/Recipes/tests/FoodDiary.Modules.Recipes.Presentation.Tests/RecipeHttpMappingsTests.cs`
- [architecture-boundary] `Tooling/tests/FoodDiary.ArchitectureTests/RecipesModuleExtractionTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
