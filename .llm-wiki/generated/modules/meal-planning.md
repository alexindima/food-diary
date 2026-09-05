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
- Extracted project: `Modules/MealPlanning/Application/FoodDiary.Application.MealPlanning.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: MealPlans, Products, ShoppingLists, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/MealPlanning/Application`
- `Modules/MealPlanning/Application/Abstractions`
- `Modules/MealPlanning/Application/Abstractions/MealPlans`
- `Modules/MealPlanning/Application/Abstractions/ShoppingLists`
- `Modules/MealPlanning/Domain`
- `Modules/MealPlanning/Infrastructure`
- `Modules/MealPlanning/Infrastructure/Model`
- `Modules/MealPlanning/Presentation`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: module-root
- Architecture guardrails: project-reference-matrix
- Declared owned entities: MealPlan, MealPlanDay, MealPlanMeal, ShoppingList, ShoppingListItem, ShoppingListItemSource
- Public contract files: 18
- Observed external consumer groups: 3
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

- [behavioral-or-text-match] `Modules/MealPlanning/tests/FoodDiary.Modules.MealPlanning.Application.Tests/FeatureErrorContractTests.cs`
- [behavioral-or-text-match] `Modules/MealPlanning/tests/FoodDiary.Modules.MealPlanning.Application.Tests/MealPlans/MealPlansFeatureTests.cs`
- [behavioral-or-text-match] `Modules/MealPlanning/tests/FoodDiary.Modules.MealPlanning.Application.Tests/MealPlans/MealPlansValidatorTests.cs`
- [behavioral-or-text-match] `Modules/MealPlanning/tests/FoodDiary.Modules.MealPlanning.Application.Tests/ShoppingLists/ShoppingListCreationServiceTests.cs`
- [behavioral-or-text-match] `Modules/MealPlanning/tests/FoodDiary.Modules.MealPlanning.Application.Tests/ShoppingLists/ShoppingListsFeatureTests.CreateCommand.cs`
- [behavioral-or-text-match] `Modules/MealPlanning/tests/FoodDiary.Modules.MealPlanning.Application.Tests/ShoppingLists/ShoppingListsFeatureTests.DeleteCommand.cs`
- [behavioral-or-text-match] `Modules/MealPlanning/tests/FoodDiary.Modules.MealPlanning.Application.Tests/ShoppingLists/ShoppingListsFeatureTests.ItemBuilder.cs`
- [behavioral-or-text-match] `Modules/MealPlanning/tests/FoodDiary.Modules.MealPlanning.Application.Tests/ShoppingLists/ShoppingListsFeatureTests.Mapping.cs`
- [behavioral-or-text-match] `Modules/MealPlanning/tests/FoodDiary.Modules.MealPlanning.Application.Tests/ShoppingLists/ShoppingListsFeatureTests.Queries.cs`
- [behavioral-or-text-match] `Modules/MealPlanning/tests/FoodDiary.Modules.MealPlanning.Application.Tests/ShoppingLists/ShoppingListsFeatureTests.UpdateCommand.cs`
- [behavioral-or-text-match] `Modules/MealPlanning/tests/FoodDiary.Modules.MealPlanning.Application.Tests/ShoppingLists/ShoppingListsFeatureTests.cs`
- [behavioral-or-text-match] `Modules/MealPlanning/tests/FoodDiary.Modules.MealPlanning.Application.Tests/ShoppingLists/ShoppingListsValidatorTests.cs`
- [behavioral-or-text-match] `Modules/MealPlanning/tests/FoodDiary.Modules.MealPlanning.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/MealPlanning/tests/FoodDiary.Modules.MealPlanning.Application.Tests/TestProductOverview.cs`
- [behavioral-or-text-match] `Modules/MealPlanning/tests/FoodDiary.Modules.MealPlanning.Domain.Tests/Domain/DietTypeContractTests.cs`
- [behavioral-or-text-match] `Modules/MealPlanning/tests/FoodDiary.Modules.MealPlanning.Domain.Tests/Domain/MealPlanInvariantTests.cs`
- [behavioral-or-text-match] `Modules/MealPlanning/tests/FoodDiary.Modules.MealPlanning.Domain.Tests/Domain/MealPlanningExtractedInvariantTests.cs`
- [behavioral-or-text-match] `Modules/MealPlanning/tests/FoodDiary.Modules.MealPlanning.Domain.Tests/Domain/ShoppingListInvariantTests.cs`
- [behavioral-or-text-match] `Modules/MealPlanning/tests/FoodDiary.Modules.MealPlanning.Domain.Tests/Domain/TrackingAndMealPlanCoverageGapTests.cs`
- [integration] `Modules/MealPlanning/tests/FoodDiary.Modules.MealPlanning.Infrastructure.IntegrationTests/MealPlanningPersistenceCompatibilityTests.cs`
- [integration] `Modules/MealPlanning/tests/FoodDiary.Modules.MealPlanning.Infrastructure.IntegrationTests/PostgresDatabaseCollection.cs`
- [integration] `Modules/MealPlanning/tests/FoodDiary.Modules.MealPlanning.Infrastructure.IntegrationTests/PostgresDatabaseFixture.cs`
- [presentation] `Modules/MealPlanning/tests/FoodDiary.Modules.MealPlanning.Presentation.Tests/MealPlanHttpMappingsTests.cs`
- [presentation] `Modules/MealPlanning/tests/FoodDiary.Modules.MealPlanning.Presentation.Tests/ShoppingListHttpMappingsTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/MealPlanningModuleExtractionTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
