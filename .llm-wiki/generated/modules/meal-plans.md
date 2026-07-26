---
id: generated.module.meal-plans
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# MealPlans

## Graph

- Origin: module-graph
- Dependencies: ShoppingLists, Users
- Consumers: none

## Source Areas

- `FoodDiary.Application.Abstractions/MealPlans`
- `FoodDiary.Application/MealPlans`
- `FoodDiary.Domain/Entities/MealPlans`
- `FoodDiary.Infrastructure/Persistence/Configurations/MealPlans`
- `FoodDiary.Infrastructure/Persistence/MealPlans`
- `FoodDiary.Presentation.Api/Features/MealPlans`
- `tests/FoodDiary.Application.Tests/MealPlans`

## HTTP Surface

### MealPlansController

Source: `FoodDiary.Presentation.Api/Features/MealPlans/MealPlansController.cs`

- `GET /api/v{version:apiVersion}/meal-plans`
- `GET /api/v{version:apiVersion}/meal-plans/{id:guid}`
- `POST /api/v{version:apiVersion}/meal-plans/{id:guid}/adopt`
- `POST /api/v{version:apiVersion}/meal-plans/{id:guid}/shopping-list`

## Focused Tests

- `tests/FoodDiary.Application.Tests/MealPlans/MealPlansFeatureTests.cs`
- `tests/FoodDiary.Application.Tests/MealPlans/MealPlansValidatorTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
