---
id: generated.module.usda
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# Usda

## Graph

- Origin: module-graph
- Dependencies: Consumptions, Users
- Consumers: Products

## Source Areas

- `FoodDiary.Application.Abstractions/Usda`
- `FoodDiary.Application/Usda`
- `FoodDiary.Domain/Entities/Usda`
- `FoodDiary.Infrastructure/Persistence/Configurations/Usda`
- `FoodDiary.Infrastructure/Persistence/Usda`
- `FoodDiary.Presentation.Api/Features/Usda`
- `FoodDiary.Web.Client/src/app/features/usda`
- `tests/FoodDiary.Application.Tests/Usda`

## HTTP Surface

### UsdaController

Source: `FoodDiary.Presentation.Api/Features/Usda/UsdaController.cs`

- `GET /api/v{version:apiVersion}/usda/foods`
- `GET /api/v{version:apiVersion}/usda/foods/{fdcId:int}`
- `PUT /api/v{version:apiVersion}/usda/products/{productId:guid}/link`
- `DELETE /api/v{version:apiVersion}/usda/products/{productId:guid}/link`
- `GET /api/v{version:apiVersion}/usda/daily-micronutrients`

## Focused Tests

- `tests/FoodDiary.Application.Tests/Usda/UsdaFeatureTests.cs`
- `tests/FoodDiary.Application.Tests/Usda/UsdaQueryHandlerTests.cs`
- `tests/FoodDiary.Application.Tests/Usda/UsdaValidatorTests.cs`
- `tests/FoodDiary.Infrastructure.Tests/Persistence/UsdaProductLinkRepositoryTests.cs`
- `tests/FoodDiary.Infrastructure.Tests/Services/UsdaFoodSearchServiceTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/UsdaHttpMappingsTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
