---
id: generated.module.shopping-lists
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# ShoppingLists

## Graph

- Origin: module-graph
- Dependencies: Users
- Consumers: MealPlans

## Source Areas

- `FoodDiary.Application.Abstractions/ShoppingLists`
- `FoodDiary.Application/ShoppingLists`
- `FoodDiary.Infrastructure/Persistence/Configurations/ShoppingLists`
- `FoodDiary.Infrastructure/Persistence/ShoppingLists`
- `FoodDiary.Presentation.Api/Features/ShoppingLists`
- `tests/FoodDiary.Application.Tests/ShoppingLists`

## HTTP Surface

### ShoppingListsController

Source: `FoodDiary.Presentation.Api/Features/ShoppingLists/ShoppingListsController.cs`

- `GET /api/v{version:apiVersion}/shopping-lists/current`
- `GET /api/v{version:apiVersion}/shopping-lists`
- `GET /api/v{version:apiVersion}/shopping-lists/{id:guid}`
- `POST /api/v{version:apiVersion}/shopping-lists`
- `PATCH /api/v{version:apiVersion}/shopping-lists/{id:guid}`
- `DELETE /api/v{version:apiVersion}/shopping-lists/{id:guid}`

## Focused Tests

- `tests/FoodDiary.Application.Tests/ShoppingLists/ShoppingListCreationServiceTests.cs`
- `tests/FoodDiary.Application.Tests/ShoppingLists/ShoppingListsFeatureTests.CreateCommand.cs`
- `tests/FoodDiary.Application.Tests/ShoppingLists/ShoppingListsFeatureTests.cs`
- `tests/FoodDiary.Application.Tests/ShoppingLists/ShoppingListsFeatureTests.DeleteCommand.cs`
- `tests/FoodDiary.Application.Tests/ShoppingLists/ShoppingListsFeatureTests.ItemBuilder.cs`
- `tests/FoodDiary.Application.Tests/ShoppingLists/ShoppingListsFeatureTests.Mapping.cs`
- `tests/FoodDiary.Application.Tests/ShoppingLists/ShoppingListsFeatureTests.Queries.cs`
- `tests/FoodDiary.Application.Tests/ShoppingLists/ShoppingListsFeatureTests.UpdateCommand.cs`
- `tests/FoodDiary.Application.Tests/ShoppingLists/ShoppingListsValidatorTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
