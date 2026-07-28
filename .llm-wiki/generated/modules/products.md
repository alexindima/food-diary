---
id: generated.module.products
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# Products

## Graph

- Origin: module-graph
- Dependencies: FavoriteProducts, Images, OpenFoodFacts, RecentItems, Usda, Users
- Consumers: none

## Source Areas

- `FoodDiary.Application.Abstractions/Products`
- `FoodDiary.Application/Products`
- `FoodDiary.Domain/Entities/Products`
- `FoodDiary.Infrastructure/Persistence/Configurations/Products`
- `FoodDiary.Infrastructure/Persistence/Products`
- `FoodDiary.Mobile/android/app/build/intermediates/assets/debug/mergeDebugAssets/public/assets/images/stubs/products`
- `FoodDiary.Mobile/android/app/build/intermediates/compressed_assets/debug/compressDebugAssets/out/assets/public/assets/images/stubs/products`
- `FoodDiary.Mobile/android/app/src/main/assets/public/assets/images/stubs/products`
- `FoodDiary.Presentation.Api/Features/Products`
- `FoodDiary.Web.Client/assets/images/stubs/products`
- `FoodDiary.Web.Client/dist-admin/assets/images/stubs/products`
- `FoodDiary.Web.Client/dist-storybook/images/stubs/products`
- `FoodDiary.Web.Client/src/app/features/products`
- `tests/FoodDiary.Application.Tests/Products`

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

## Focused Tests

- `tests/FoodDiary.Application.Tests/Products/ProductSearchSuggestionTests.cs`
- `tests/FoodDiary.Application.Tests/Products/ProductsFeatureTests.CreateCommandTests.cs`
- `tests/FoodDiary.Application.Tests/Products/ProductsFeatureTests.DeleteAndDuplicateCommandTests.cs`
- `tests/FoodDiary.Application.Tests/Products/ProductsFeatureTests.MappingTests.cs`
- `tests/FoodDiary.Application.Tests/Products/ProductsFeatureTests.ReadQueryTests.cs`
- `tests/FoodDiary.Application.Tests/Products/ProductsFeatureTests.UpdateCommandTests.cs`
- `tests/FoodDiary.Application.Tests/Products/ProductsFeatureTests.cs`
- `tests/FoodDiary.Application.Tests/Products/ProductsValidatorTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/ProductSuggestionsControllerTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
