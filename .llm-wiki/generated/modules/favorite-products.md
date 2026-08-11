---
id: generated.module.favorite-products
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# FavoriteProducts

## Graph

- Origin: module-graph
- Business-module dependencies: Users
- Abstraction-contract dependencies: Products, Users
- Business-module consumers: Products
- Host/adapter consumers: FoodDiary.Presentation.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/FavoriteProducts`
- `FoodDiary.Application/FavoriteProducts`
- `FoodDiary.Domain/Entities/FavoriteProducts`
- `FoodDiary.Infrastructure/Persistence/FavoriteProducts`
- `FoodDiary.Presentation.Api/Features/FavoriteProducts`

## HTTP Surface

### FavoriteProductsController

Source: `FoodDiary.Presentation.Api/Features/FavoriteProducts/FavoriteProductsController.cs`

- `GET /api/v{version:apiVersion}/favorite-products`
- `GET /api/v{version:apiVersion}/favorite-products/check/{productId:guid}`
- `POST /api/v{version:apiVersion}/favorite-products`
- `PUT /api/v{version:apiVersion}/favorite-products/{id:guid}`
- `DELETE /api/v{version:apiVersion}/favorite-products/{id:guid}`

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
- `interface IFavoriteProductReadModelRepository`
- `interface IFavoriteProductReadRepository`
- `interface IFavoriteProductRepository`
- `interface IFavoriteProductWriteRepository`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/FavoriteProducts/FavoriteProductsAdditionalFeatureTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/FavoriteProductsControllerTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
