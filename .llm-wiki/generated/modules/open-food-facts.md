---
id: generated.module.open-food-facts
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# OpenFoodFacts

## Graph

- Origin: module-graph
- Business-module dependencies: none observed
- Abstraction-contract dependencies: none observed
- Business-module consumers: Products
- Host/adapter consumers: FoodDiary.Integrations, FoodDiary.Presentation.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/OpenFoodFacts`
- `FoodDiary.Application/OpenFoodFacts`
- `FoodDiary.Domain/Entities/OpenFoodFacts`
- `FoodDiary.Infrastructure/Persistence/Configurations/OpenFoodFacts`
- `FoodDiary.Infrastructure/Persistence/OpenFoodFacts`
- `FoodDiary.Presentation.Api/Features/OpenFoodFacts`

## HTTP Surface

### OpenFoodFactsController

Source: `FoodDiary.Presentation.Api/Features/OpenFoodFacts/OpenFoodFactsController.cs`

- `GET /api/v{version:apiVersion}/open-food-facts/products/{barcode}`
- `GET /api/v{version:apiVersion}/open-food-facts/products`

## Boundary Health

- Role: adapter
- Physical isolation: folder
- Architecture guardrails: graph-only
- Declared owned entities: not yet enumerated
- Public contract files: 4
- Observed external consumer groups: 3
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 4
- Exported repository-shaped contracts: 3
- `interface IOpenFoodFactsProductCacheReadRepository`
- `interface IOpenFoodFactsProductCacheRepository`
- `interface IOpenFoodFactsProductCacheWriteRepository`
- `interface IOpenFoodFactsService`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/OpenFoodFacts/OpenFoodFactsFeatureTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/OpenFoodFacts/OpenFoodFactsValidatorTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Domain.Tests/Domain/OpenFoodFactsProductTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Infrastructure.Tests/Services/OpenFoodFactsServiceTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/OpenFoodFactsControllerTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/OpenFoodFactsHttpMappingsTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
