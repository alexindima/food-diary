---
id: generated.module.favorite-meals
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# FavoriteMeals

## Graph

- Origin: module-graph
- Dependencies: Consumptions, Users
- Consumers: none

## Source Areas

- `FoodDiary.Application.Abstractions/FavoriteMeals`
- `FoodDiary.Application/FavoriteMeals`
- `FoodDiary.Domain/Entities/FavoriteMeals`
- `FoodDiary.Infrastructure/Persistence/FavoriteMeals`
- `FoodDiary.Presentation.Api/Features/FavoriteMeals`
- `tests/FoodDiary.Application.Tests/FavoriteMeals`

## HTTP Surface

### FavoriteMealsController

Source: `FoodDiary.Presentation.Api/Features/FavoriteMeals/FavoriteMealsController.cs`

- `GET /api/v{version:apiVersion}/favorite-meals`
- `GET /api/v{version:apiVersion}/favorite-meals/check/{mealId:guid}`
- `POST /api/v{version:apiVersion}/favorite-meals`
- `DELETE /api/v{version:apiVersion}/favorite-meals/{id:guid}`

## Focused Tests

- `tests/FoodDiary.Application.Tests/FavoriteMeals/FavoriteMealReadServiceCoverageTests.cs`
- `tests/FoodDiary.Application.Tests/FavoriteMeals/FavoriteMealsFeatureTests.cs`
- `tests/FoodDiary.Application.Tests/FavoriteMeals/FavoriteMealsValidatorTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
