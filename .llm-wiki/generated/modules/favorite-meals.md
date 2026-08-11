---
id: generated.module.favorite-meals
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# FavoriteMeals

## Graph

- Origin: module-graph
- Business-module dependencies: Consumptions, Users
- Abstraction-contract dependencies: Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Presentation.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/FavoriteMeals`
- `FoodDiary.Application/FavoriteMeals`
- `FoodDiary.Domain/Entities/FavoriteMeals`
- `FoodDiary.Infrastructure/Persistence/FavoriteMeals`
- `FoodDiary.Presentation.Api/Features/FavoriteMeals`

## HTTP Surface

### FavoriteMealsController

Source: `FoodDiary.Presentation.Api/Features/FavoriteMeals/FavoriteMealsController.cs`

- `GET /api/v{version:apiVersion}/favorite-meals`
- `GET /api/v{version:apiVersion}/favorite-meals/check/{mealId:guid}`
- `POST /api/v{version:apiVersion}/favorite-meals`
- `DELETE /api/v{version:apiVersion}/favorite-meals/{id:guid}`

## Boundary Health

- Role: aggregate-owner
- Physical isolation: folder
- Architecture guardrails: graph-only
- Declared owned entities: not yet enumerated
- Public contract files: 4
- Observed external consumer groups: 1
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 4
- Exported repository-shaped contracts: 4
- `interface IFavoriteMealReadModelRepository`
- `interface IFavoriteMealReadRepository`
- `interface IFavoriteMealRepository`
- `interface IFavoriteMealWriteRepository`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/FavoriteMeals/FavoriteMealReadServiceCoverageTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/FavoriteMeals/FavoriteMealsFeatureTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/FavoriteMeals/FavoriteMealsValidatorTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
