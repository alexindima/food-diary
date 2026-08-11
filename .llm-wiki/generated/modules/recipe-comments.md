---
id: generated.module.recipe-comments
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# RecipeComments

## Graph

- Origin: module-graph
- Business-module dependencies: Notifications, Users
- Abstraction-contract dependencies: Notifications, Recipes, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Presentation.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/RecipeComments`
- `FoodDiary.Application/RecipeComments`
- `FoodDiary.Infrastructure/Persistence/Configurations/RecipeSocial`
- `FoodDiary.Presentation.Api/Features/RecipeComments`

## HTTP Surface

### RecipeCommentsController

Source: `FoodDiary.Presentation.Api/Features/RecipeComments/RecipeCommentsController.cs`

- `GET /api/v{version:apiVersion}/recipes/{recipeId:guid}/comments`
- `POST /api/v{version:apiVersion}/recipes/{recipeId:guid}/comments`
- `PATCH /api/v{version:apiVersion}/recipes/{recipeId:guid}/comments/{commentId:guid}`
- `DELETE /api/v{version:apiVersion}/recipes/{recipeId:guid}/comments/{commentId:guid}`

## Boundary Health

- Role: aggregate-owner
- Physical isolation: folder
- Architecture guardrails: graph-only
- Declared owned entities: not yet enumerated
- Public contract files: 6
- Observed external consumer groups: 1
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 6
- Interfaces: 4
- DTO/read-model/projection types: 1
- Enums: 0
- Exported repository-shaped contracts: 4
- Contracts referencing domain entities: 2
- `class RecipeCommentErrors`
- `interface IRecipeCommentReadModelRepository`
- `interface IRecipeCommentReadRepository`
- `interface IRecipeCommentRepository`
- `interface IRecipeCommentWriteRepository`
- `record RecipeCommentReadModel`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/RecipeComments/RecipeCommentsFeatureTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/RecipeComments/RecipeCommentsValidatorTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/RecipeCommentsControllerTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
