---
id: generated.module.meal-plans
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# MealPlans

## Graph

- Origin: module-graph
- Business-module dependencies: ShoppingLists, Users
- Abstraction-contract dependencies: Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Presentation.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/MealPlans`
- `FoodDiary.Application/MealPlans`
- `FoodDiary.Domain/Entities/MealPlans`
- `FoodDiary.Infrastructure/Persistence/Configurations/MealPlans`
- `FoodDiary.Infrastructure/Persistence/MealPlans`
- `FoodDiary.Presentation.Api/Features/MealPlans`

## HTTP Surface

### MealPlansController

Source: `FoodDiary.Presentation.Api/Features/MealPlans/MealPlansController.cs`

- `GET /api/v{version:apiVersion}/meal-plans`
- `GET /api/v{version:apiVersion}/meal-plans/{id:guid}`
- `POST /api/v{version:apiVersion}/meal-plans/{id:guid}/adopt`
- `POST /api/v{version:apiVersion}/meal-plans/{id:guid}/shopping-list`

## Boundary Health

- Role: aggregate-owner
- Physical isolation: folder
- Architecture guardrails: graph-only
- Declared owned entities: not yet enumerated
- Public contract files: 9
- Observed external consumer groups: 1
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 9
- Interfaces: 4
- DTO/read-model/projection types: 4
- Enums: 0
- Exported repository-shaped contracts: 4
- Contracts referencing domain entities: 2
- `class MealPlanErrors`
- `interface IMealPlanReadModelRepository`
- `interface IMealPlanReadRepository`
- `interface IMealPlanRepository`
- `interface IMealPlanWriteRepository`
- `record MealPlanDayReadModel`
- `record MealPlanMealReadModel`
- `record MealPlanReadModel`
- `record MealPlanSummaryReadModel`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/MealPlans/MealPlansFeatureTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/MealPlans/MealPlansValidatorTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
