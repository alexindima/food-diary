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

- Origin: extracted-project
- Extracted project: `Modules/Usda/Application/FoodDiary.Application.Usda.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Meals, Products, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.Integrations, FoodDiary.JobManager, FoodDiary.Modules.Products.Application, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Presentation.Api/Features/Usda`
- `Modules/Usda/Application`
- `Modules/Usda/Application/Abstractions`
- `Modules/Usda/Contracts`
- `Modules/Usda/Domain`
- `Modules/Usda/Infrastructure/Providers`

## HTTP Surface

### UsdaController

Source: `FoodDiary.Presentation.Api/Features/Usda/UsdaController.cs`

- `GET /api/v{version:apiVersion}/usda/foods`
- `GET /api/v{version:apiVersion}/usda/foods/{fdcId:int}`
- `PUT /api/v{version:apiVersion}/usda/products/{productId:guid}/link`
- `DELETE /api/v{version:apiVersion}/usda/products/{productId:guid}/link`
- `GET /api/v{version:apiVersion}/usda/daily-micronutrients`

## Boundary Health

- Role: reference-data-owner
- Physical isolation: module-root
- Architecture guardrails: project-reference-matrix-and-module-boundary-tests
- Declared owned entities: DailyReferenceValue, UsdaFood, UsdaFoodNutrient, UsdaFoodPortion, UsdaNutrient
- Public contract files: 17
- Observed external consumer groups: 6
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 17
- Interfaces: 5
- DTO/read-model/projection types: 11
- Enums: 0
- Exported repository-shaped contracts: 3
- Contracts referencing domain entities: 1
- `class UsdaErrors`
- `interface IUsdaDailyMicronutrientReadService`
- `interface IUsdaFoodReadModelRepository`
- `interface IUsdaFoodReadRepository`
- `interface IUsdaFoodRepository`
- `interface IUsdaFoodSearchService`
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

- [behavioral-or-text-match] `Modules/Usda/tests/FoodDiary.Modules.Usda.Application.Tests/FeatureErrorContractTests.cs`
- [behavioral-or-text-match] `Modules/Usda/tests/FoodDiary.Modules.Usda.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/Usda/tests/FoodDiary.Modules.Usda.Application.Tests/Usda/UsdaFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Usda/tests/FoodDiary.Modules.Usda.Application.Tests/Usda/UsdaQueryHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Usda/tests/FoodDiary.Modules.Usda.Application.Tests/Usda/UsdaValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Usda/tests/FoodDiary.Modules.Usda.Domain.Tests/Domain/ReferenceDataInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Usda/tests/FoodDiary.Modules.Usda.Domain.Tests/ValueObjects/AdditionalValueObjectsInvariantTestsHealthAreaTests.cs`
- [behavioral-or-text-match] `Modules/Usda/tests/FoodDiary.Modules.Usda.Domain.Tests/ValueObjects/SecondPassDomainHardeningTestsHealthAreaTests.cs`
- [behavioral-or-text-match] `Modules/Usda/tests/FoodDiary.Modules.Usda.Infrastructure.Tests/Integrations/ProviderOptionsTests.cs`
- [behavioral-or-text-match] `Modules/Usda/tests/FoodDiary.Modules.Usda.Infrastructure.Tests/Integrations/ProviderRegistrationTests.cs`
- [behavioral-or-text-match] `Modules/Usda/tests/FoodDiary.Modules.Usda.Infrastructure.Tests/Integrations/UsdaExternalFoodServiceTests.cs`
- [behavioral-or-text-match] `Modules/Usda/tests/FoodDiary.Modules.Usda.Infrastructure.Tests/Services/UsdaFoodSearchServiceTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/UsdaModuleExtractionTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/UsdaHttpMappingsTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
