---
id: generated.module.products
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# Products

## Graph

- Origin: module-graph
- Business-module dependencies: Images, OpenFoodFacts, Usda
- Abstraction-contract dependencies: FavoriteProducts, Images, OpenFoodFacts, RecentItems, Usda, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Presentation.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/Products`
- `FoodDiary.Application/Products`
- `FoodDiary.Domain/Entities/Products`
- `FoodDiary.Infrastructure/Persistence/Configurations/Products`
- `FoodDiary.Infrastructure/Persistence/Products`
- `FoodDiary.Presentation.Api/Features/Products`

## HTTP Surface

### ProductSuggestionsController

Source: `FoodDiary.Presentation.Api/Features/Products/ProductSuggestionsController.cs`

- `GET /api/v{version:apiVersion}/products/suggestions`

### ProductsController

Source: `FoodDiary.Presentation.Api/Features/Products/ProductsController.cs`

- `GET /api/v{version:apiVersion}/products`
- `GET /api/v{version:apiVersion}/products/overview`
- `GET /api/v{version:apiVersion}/products/recent`
- `GET /api/v{version:apiVersion}/products/{id:guid}`
- `POST /api/v{version:apiVersion}/products`
- `PATCH /api/v{version:apiVersion}/products/{id:guid}`
- `DELETE /api/v{version:apiVersion}/products/{id:guid}`
- `POST /api/v{version:apiVersion}/products/{id:guid}/duplicate`

## Boundary Health

- Role: aggregate-owner
- Physical isolation: folder
- Architecture guardrails: explicit-boundary-tests
- Declared owned entities: Product
- Public contract files: 9
- Observed external consumer groups: 1
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 9
- Interfaces: 6
- DTO/read-model/projection types: 0
- Enums: 0
- Exported repository-shaped contracts: 3
- Contracts referencing domain entities: 3
- `class ProductErrors`
- `interface IProductLookupService`
- `interface IProductOverviewReadService`
- `interface IProductReadRepository`
- `interface IProductRepository`
- `interface IProductUsdaLinkService`
- `interface IProductWriteRepository`
- `record ProductOverviewReadItem`
- `record ProductQueryFilters`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Products/ProductSearchSuggestionTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Products/ProductUsdaLinkServiceTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Products/ProductsFeatureTests.CreateCommandTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Products/ProductsFeatureTests.DeleteAndDuplicateCommandTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Products/ProductsFeatureTests.MappingTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Products/ProductsFeatureTests.ReadQueryTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Products/ProductsFeatureTests.UpdateCommandTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Products/ProductsFeatureTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Products/ProductsValidatorTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/ProductSuggestionsControllerTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
