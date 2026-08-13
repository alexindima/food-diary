---
id: generated.module.recipe-community
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# RecipeCommunity

## Graph

- Origin: extracted-project
- Extracted project: `FoodDiary.Application.RecipeCommunity/FoodDiary.Application.RecipeCommunity.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Notifications, RecipeComments, RecipeLikes, Recipes, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/RecipeComments`
- `FoodDiary.Application.Abstractions/RecipeLikes`
- `FoodDiary.Application.RecipeCommunity`
- `FoodDiary.Infrastructure/Persistence/Configurations/RecipeSocial`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: not yet enumerated
- Public contract files: 9
- Observed external consumer groups: 4
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 9
- Interfaces: 7
- DTO/read-model/projection types: 1
- Enums: 0
- Exported repository-shaped contracts: 7
- Contracts referencing domain entities: 4
- `class RecipeCommentErrors`
- `interface IRecipeCommentReadModelRepository`
- `interface IRecipeCommentReadRepository`
- `interface IRecipeCommentRepository`
- `interface IRecipeCommentWriteRepository`
- `interface IRecipeLikeReadRepository`
- `interface IRecipeLikeRepository`
- `interface IRecipeLikeWriteRepository`
- `record RecipeCommentReadModel`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/RecipeCommunityModuleExtractionTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
