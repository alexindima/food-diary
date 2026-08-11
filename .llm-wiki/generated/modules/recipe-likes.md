---
id: generated.module.recipe-likes
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# RecipeLikes

## Graph

- Origin: module-graph
- Business-module dependencies: Users
- Abstraction-contract dependencies: Recipes, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Presentation.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/RecipeLikes`
- `FoodDiary.Application/RecipeLikes`
- `FoodDiary.Infrastructure/Persistence/Configurations/RecipeSocial`
- `FoodDiary.Presentation.Api/Features/RecipeLikes`

## HTTP Surface

### RecipeLikesController

Source: `FoodDiary.Presentation.Api/Features/RecipeLikes/RecipeLikesController.cs`

- `POST /api/v{version:apiVersion}/recipes/{recipeId:guid}/likes/toggle`
- `GET /api/v{version:apiVersion}/recipes/{recipeId:guid}/likes`

## Boundary Health

- Role: aggregate-owner
- Physical isolation: folder
- Architecture guardrails: graph-only
- Declared owned entities: not yet enumerated
- Public contract files: 3
- Observed external consumer groups: 1
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 3
- Exported repository-shaped contracts: 3
- `interface IRecipeLikeReadRepository`
- `interface IRecipeLikeRepository`
- `interface IRecipeLikeWriteRepository`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/RecipeLikes/RecipeLikesFeatureTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
