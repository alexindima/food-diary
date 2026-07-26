---
id: generated.module.recipe-likes
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# RecipeLikes

## Graph

- Origin: module-graph
- Dependencies: Users
- Consumers: none

## Source Areas

- `FoodDiary.Application.Abstractions/RecipeLikes`
- `FoodDiary.Application/RecipeLikes`
- `FoodDiary.Infrastructure/Persistence/RecipeLikes`
- `FoodDiary.Presentation.Api/Features/RecipeLikes`
- `tests/FoodDiary.Application.Tests/RecipeLikes`

## HTTP Surface

### RecipeLikesController

Source: `FoodDiary.Presentation.Api/Features/RecipeLikes/RecipeLikesController.cs`

- `POST /api/v{version:apiVersion}/recipes/{recipeId:guid}/likes/toggle`
- `GET /api/v{version:apiVersion}/recipes/{recipeId:guid}/likes`

## Focused Tests

- `tests/FoodDiary.Application.Tests/RecipeLikes/RecipeLikesFeatureTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
