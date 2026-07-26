---
id: generated.module.favorite-products
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# FavoriteProducts

## Graph

- Origin: module-graph
- Dependencies: Users
- Consumers: Products

## Source Areas

- `FoodDiary.Application.Abstractions/FavoriteProducts`
- `FoodDiary.Application/FavoriteProducts`
- `FoodDiary.Domain/Entities/FavoriteProducts`
- `FoodDiary.Infrastructure/Persistence/FavoriteProducts`
- `FoodDiary.Presentation.Api/Features/FavoriteProducts`
- `tests/FoodDiary.Application.Tests/FavoriteProducts`

## HTTP Surface

### FavoriteProductsController

Source: `FoodDiary.Presentation.Api/Features/FavoriteProducts/FavoriteProductsController.cs`

- `GET /api/v{version:apiVersion}/favorite-products`
- `GET /api/v{version:apiVersion}/favorite-products/check/{productId:guid}`
- `POST /api/v{version:apiVersion}/favorite-products`
- `PUT /api/v{version:apiVersion}/favorite-products/{id:guid}`
- `DELETE /api/v{version:apiVersion}/favorite-products/{id:guid}`

## Focused Tests

- `tests/FoodDiary.Application.Tests/FavoriteProducts/FavoriteProductsAdditionalFeatureTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/FavoriteProductsControllerTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
