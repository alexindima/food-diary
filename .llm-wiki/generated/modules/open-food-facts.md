---
id: generated.module.open-food-facts
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# OpenFoodFacts

## Graph

- Origin: module-graph
- Dependencies: none
- Consumers: Products

## Source Areas

- `FoodDiary.Application.Abstractions/OpenFoodFacts`
- `FoodDiary.Application/OpenFoodFacts`
- `FoodDiary.Domain/Entities/OpenFoodFacts`
- `FoodDiary.Infrastructure/Persistence/Configurations/OpenFoodFacts`
- `FoodDiary.Infrastructure/Persistence/OpenFoodFacts`
- `FoodDiary.Presentation.Api/Features/OpenFoodFacts`
- `tests/FoodDiary.Application.Tests/OpenFoodFacts`

## HTTP Surface

### OpenFoodFactsController

Source: `FoodDiary.Presentation.Api/Features/OpenFoodFacts/OpenFoodFactsController.cs`

- `GET /api/v{version:apiVersion}/open-food-facts/products/{barcode}`
- `GET /api/v{version:apiVersion}/open-food-facts/products`

## Focused Tests

- `tests/FoodDiary.Application.Tests/OpenFoodFacts/OpenFoodFactsFeatureTests.cs`
- `tests/FoodDiary.Application.Tests/OpenFoodFacts/OpenFoodFactsValidatorTests.cs`
- `tests/FoodDiary.Domain.Tests/Domain/OpenFoodFactsProductTests.cs`
- `tests/FoodDiary.Infrastructure.Tests/Services/OpenFoodFactsServiceTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/OpenFoodFactsControllerTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/OpenFoodFactsHttpMappingsTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
