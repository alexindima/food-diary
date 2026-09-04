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

- Origin: extracted-project
- Extracted project: `Modules/Products/Application/FoodDiary.Modules.Products.Application.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: FavoriteProducts, Images, OpenFoodFacts, RecentItems, Usda, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Presentation.Api/Features/Products`
- `Modules/Products/Application`
- `Modules/Products/Application/Abstractions`
- `Modules/Products/Contracts`

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
- Physical isolation: module-root
- Architecture guardrails: project-reference-matrix
- Declared owned entities: Product
- Public contract files: 10
- Observed external consumer groups: 4
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 10
- Interfaces: 7
- DTO/read-model/projection types: 0
- Enums: 0
- Exported repository-shaped contracts: 3
- Contracts referencing domain entities: 3
- `class ProductErrors`
- `interface IProductLookupService`
- `interface IProductMutationTransactionRunner`
- `interface IProductOverviewReadService`
- `interface IProductReadRepository`
- `interface IProductRepository`
- `interface IProductUsdaLinkService`
- `interface IProductWriteRepository`
- `record ProductOverviewReadItem`
- `record ProductQueryFilters`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Application.Tests/FeatureErrorContractTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Application.Tests/Products/ProductSearchSuggestionTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Application.Tests/Products/ProductUsdaLinkServiceTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Application.Tests/Products/ProductsFeatureTests.CreateCommandTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Application.Tests/Products/ProductsFeatureTests.DeleteAndDuplicateCommandTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Application.Tests/Products/ProductsFeatureTests.MappingTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Application.Tests/Products/ProductsFeatureTests.ReadQueryTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Application.Tests/Products/ProductsFeatureTests.UpdateCommandTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Application.Tests/Products/ProductsFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Application.Tests/Products/ProductsValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Application.Tests/Support/AllowImageAssetAccessService.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Application.Tests/Support/RecordingImageAssetAccessService.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Domain.Tests/Domain/ProductExtractedInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Domain.Tests/Domain/ProductInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Domain.Tests/ValueObjects/FoodQualityScoreTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Domain.Tests/ValueObjects/NutritionContractTests.cs`
- [integration] `Modules/Products/tests/FoodDiary.Modules.Products.Infrastructure.IntegrationTests/Integration/ProductRepositoryIntegrationTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Products/ProductSearchSuggestionTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Products/ProductsFeatureTests.ReadQueryTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Products/ProductsFeatureTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/ProductsModuleExtractionTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/ProductSuggestionsControllerTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
