---
id: generated.module.recipe-comments
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# RecipeComments

## Graph

- Origin: module-graph
- Dependencies: Notifications, Users
- Consumers: none

## Source Areas

- `FoodDiary.Application.Abstractions/RecipeComments`
- `FoodDiary.Application/RecipeComments`
- `FoodDiary.Infrastructure/Persistence/RecipeComments`
- `FoodDiary.Presentation.Api/Features/RecipeComments`
- `tests/FoodDiary.Application.Tests/RecipeComments`

## HTTP Surface

### RecipeCommentsController

Source: `FoodDiary.Presentation.Api/Features/RecipeComments/RecipeCommentsController.cs`

- `GET /api/v{version:apiVersion}/recipes/{recipeId:guid}/comments`
- `POST /api/v{version:apiVersion}/recipes/{recipeId:guid}/comments`
- `PATCH /api/v{version:apiVersion}/recipes/{recipeId:guid}/comments/{commentId:guid}`
- `DELETE /api/v{version:apiVersion}/recipes/{recipeId:guid}/comments/{commentId:guid}`

## Focused Tests

- `tests/FoodDiary.Application.Tests/RecipeComments/RecipeCommentsFeatureTests.cs`
- `tests/FoodDiary.Application.Tests/RecipeComments/RecipeCommentsValidatorTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/RecipeCommentsControllerTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
