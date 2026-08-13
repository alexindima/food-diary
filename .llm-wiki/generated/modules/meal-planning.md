---
id: generated.module.meal-planning
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# MealPlanning

## Graph

- Origin: extracted-project
- Extracted project: `FoodDiary.Application.MealPlanning/FoodDiary.Application.MealPlanning.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: MealPlans, Products, ShoppingLists, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/MealPlans`
- `FoodDiary.Application.Abstractions/ShoppingLists`
- `FoodDiary.Application.MealPlanning`
- `FoodDiary.Domain/Entities/MealPlans`
- `FoodDiary.Domain/Entities/Shopping`
- `FoodDiary.Infrastructure/Persistence/Configurations/MealPlans`
- `FoodDiary.Infrastructure/Persistence/Configurations/ShoppingLists`
- `FoodDiary.Infrastructure/Persistence/MealPlans`
- `FoodDiary.Infrastructure/Persistence/ShoppingLists`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: MealPlan, MealPlanDay, MealPlanMeal, ShoppingList, ShoppingListItem, ShoppingListItemSource
- Public contract files: 18
- Observed external consumer groups: 4
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 18
- Interfaces: 8
- DTO/read-model/projection types: 8
- Enums: 0
- Exported repository-shaped contracts: 8
- Contracts referencing domain entities: 4
- `class MealPlanErrors`
- `class ShoppingListErrors`
- `interface IMealPlanReadModelRepository`
- `interface IMealPlanReadRepository`
- `interface IMealPlanRepository`
- `interface IMealPlanWriteRepository`
- `interface IShoppingListReadModelRepository`
- `interface IShoppingListReadRepository`
- `interface IShoppingListRepository`
- `interface IShoppingListWriteRepository`
- `record MealPlanDayReadModel`
- `record MealPlanMealReadModel`
- `record MealPlanReadModel`
- `record MealPlanSummaryReadModel`
- `record ShoppingListItemReadModel`
- `record ShoppingListItemSourceReadModel`
- `record ShoppingListReadModel`
- `record ShoppingListSummaryReadModel`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/MealPlanningModuleExtractionTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
