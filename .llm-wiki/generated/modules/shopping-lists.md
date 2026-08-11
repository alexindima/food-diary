---
id: generated.module.shopping-lists
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# ShoppingLists

## Graph

- Origin: module-graph
- Business-module dependencies: Users
- Abstraction-contract dependencies: Products, Users
- Business-module consumers: MealPlans
- Host/adapter consumers: FoodDiary.Presentation.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/ShoppingLists`
- `FoodDiary.Application/ShoppingLists`
- `FoodDiary.Infrastructure/Persistence/Configurations/ShoppingLists`
- `FoodDiary.Infrastructure/Persistence/ShoppingLists`
- `FoodDiary.Presentation.Api/Features/ShoppingLists`

## HTTP Surface

### ShoppingListsController

Source: `FoodDiary.Presentation.Api/Features/ShoppingLists/ShoppingListsController.cs`

- `GET /api/v{version:apiVersion}/shopping-lists/current`
- `GET /api/v{version:apiVersion}/shopping-lists`
- `GET /api/v{version:apiVersion}/shopping-lists/{id:guid}`
- `POST /api/v{version:apiVersion}/shopping-lists`
- `PATCH /api/v{version:apiVersion}/shopping-lists/{id:guid}`
- `DELETE /api/v{version:apiVersion}/shopping-lists/{id:guid}`

## Boundary Health

- Role: aggregate-owner
- Physical isolation: folder
- Architecture guardrails: graph-only
- Declared owned entities: not yet enumerated
- Public contract files: 4
- Observed external consumer groups: 2
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 4
- Exported repository-shaped contracts: 4
- `interface IShoppingListReadModelRepository`
- `interface IShoppingListReadRepository`
- `interface IShoppingListRepository`
- `interface IShoppingListWriteRepository`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/ShoppingLists/ShoppingListCreationServiceTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/ShoppingLists/ShoppingListsFeatureTests.CreateCommand.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/ShoppingLists/ShoppingListsFeatureTests.DeleteCommand.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/ShoppingLists/ShoppingListsFeatureTests.ItemBuilder.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/ShoppingLists/ShoppingListsFeatureTests.Mapping.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/ShoppingLists/ShoppingListsFeatureTests.Queries.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/ShoppingLists/ShoppingListsFeatureTests.UpdateCommand.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/ShoppingLists/ShoppingListsFeatureTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/ShoppingLists/ShoppingListsValidatorTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
