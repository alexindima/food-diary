---
id: generated.module.usda
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# Usda

## Graph

- Origin: module-graph
- Business-module dependencies: Consumptions, Users
- Abstraction-contract dependencies: Meals, Users
- Business-module consumers: Products
- Host/adapter consumers: FoodDiary.Integrations, FoodDiary.Presentation.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/Usda`
- `FoodDiary.Application/Usda`
- `FoodDiary.Domain/Entities/Usda`
- `FoodDiary.Infrastructure/Persistence/Configurations/Usda`
- `FoodDiary.Infrastructure/Persistence/Usda`
- `FoodDiary.Presentation.Api/Features/Usda`

## HTTP Surface

### UsdaController

Source: `FoodDiary.Presentation.Api/Features/Usda/UsdaController.cs`

- `GET /api/v{version:apiVersion}/usda/foods`
- `GET /api/v{version:apiVersion}/usda/foods/{fdcId:int}`
- `PUT /api/v{version:apiVersion}/usda/products/{productId:guid}/link`
- `DELETE /api/v{version:apiVersion}/usda/products/{productId:guid}/link`
- `GET /api/v{version:apiVersion}/usda/daily-micronutrients`

## Boundary Health

- Role: adapter
- Physical isolation: folder
- Architecture guardrails: graph-only
- Declared owned entities: not yet enumerated
- Public contract files: 20
- Observed external consumer groups: 3
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 20
- Interfaces: 8
- DTO/read-model/projection types: 11
- Enums: 0
- Exported repository-shaped contracts: 6
- Contracts referencing domain entities: 3
- `class UsdaErrors`
- `interface IUsdaDailyMicronutrientReadService`
- `interface IUsdaFoodReadModelRepository`
- `interface IUsdaFoodReadRepository`
- `interface IUsdaFoodRepository`
- `interface IUsdaFoodSearchService`
- `interface IUsdaProductLinkReadRepository`
- `interface IUsdaProductLinkRepository`
- `interface IUsdaProductLinkWriteRepository`
- `record DailyMicronutrientModel`
- `record DailyMicronutrientSummaryModel`
- `record HealthAreaScoreModel`
- `record HealthAreaScoresModel`
- `record MicronutrientModel`
- `record UsdaDailyReferenceValueReadModel`
- `record UsdaFoodDetailModel`
- `record UsdaFoodModel`
- `record UsdaFoodPortionModel`
- `record UsdaFoodReadModel`
- `record UsdaNutrientReadModel`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Usda/UsdaFeatureTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Usda/UsdaQueryHandlerTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Usda/UsdaValidatorTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Infrastructure.Tests/Persistence/UsdaProductLinkRepositoryTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Infrastructure.Tests/Services/UsdaFoodSearchServiceTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/UsdaHttpMappingsTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
