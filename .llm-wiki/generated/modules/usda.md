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
- Extracted project: `Modules/Usda/Application/FoodDiary.Modules.Usda.Application.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/Usda/Application`
- `Modules/Usda/Application.Abstractions`
- `Modules/Usda/Contracts`
- `Modules/Usda/Domain`
- `Modules/Usda/Infrastructure/Providers`
- `Modules/Usda/PersistenceModel`
- `Modules/Usda/Presentation`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: reference-data-owner
- Physical isolation: module-root
- Architecture guardrails: project-reference-matrix-and-module-boundary-tests
- Declared owned entities: DailyReferenceValue, UsdaFood, UsdaFoodNutrient, UsdaFoodPortion, UsdaNutrient
- Public contract files: 20
- Observed external consumer groups: 3
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 20
- Interfaces: 6
- DTO/read-model/projection types: 12
- Enums: 0
- Exported repository-shaped contracts: 3
- Contracts referencing domain entities: 0
- `class UsdaErrors`
- `interface IUsdaFoodReadModelRepository`
- `interface IUsdaFoodReadRepository`
- `interface IUsdaFoodRepository`
- `interface IUsdaFoodSearchService`
- `interface IUsdaMealNutritionReadService`
- `interface IUsdaProductLinkService`
- `record DailyMicronutrientModel`
- `record DailyMicronutrientSummaryModel`
- `record HealthAreaScoreModel`
- `record HealthAreaScoresModel`
- `record MicronutrientModel`
- `record SearchUsdaFoodsQuery`
- `record UsdaDailyReferenceValueReadModel`
- `record UsdaFoodDetailModel`
- `record UsdaFoodModel`
- `record UsdaFoodPortionModel`
- `record UsdaFoodReadModel`
- `record UsdaMealProductNutritionReadModel`
- `record UsdaNutrientReadModel`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/Usda/tests/FoodDiary.Modules.Usda.Application.Tests/FeatureErrorContractTests.cs`
- [behavioral-or-text-match] `Modules/Usda/tests/FoodDiary.Modules.Usda.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/Usda/tests/FoodDiary.Modules.Usda.Application.Tests/UsdaFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Usda/tests/FoodDiary.Modules.Usda.Application.Tests/UsdaQueryHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Usda/tests/FoodDiary.Modules.Usda.Application.Tests/UsdaValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Usda/tests/FoodDiary.Modules.Usda.Domain.Tests/HealthAreaScoreBoundaryTests.cs`
- [behavioral-or-text-match] `Modules/Usda/tests/FoodDiary.Modules.Usda.Domain.Tests/ReferenceDataInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Usda/tests/FoodDiary.Modules.Usda.Domain.Tests/UsdaFoodPersistenceShapeTests.cs`
- [behavioral-or-text-match] `Modules/Usda/tests/FoodDiary.Modules.Usda.Domain.Tests/ValueObjects/AdditionalValueObjectsInvariantTestsHealthAreaTests.cs`
- [behavioral-or-text-match] `Modules/Usda/tests/FoodDiary.Modules.Usda.Domain.Tests/ValueObjects/SecondPassDomainHardeningTestsHealthAreaTests.cs`
- [behavioral-or-text-match] `Modules/Usda/tests/FoodDiary.Modules.Usda.Infrastructure.Tests/Integrations/ProviderOptionsTests.cs`
- [behavioral-or-text-match] `Modules/Usda/tests/FoodDiary.Modules.Usda.Infrastructure.Tests/Integrations/ProviderRegistrationTests.cs`
- [behavioral-or-text-match] `Modules/Usda/tests/FoodDiary.Modules.Usda.Infrastructure.Tests/Integrations/UsdaExternalFoodServiceTests.cs`
- [behavioral-or-text-match] `Modules/Usda/tests/FoodDiary.Modules.Usda.Infrastructure.Tests/Services/UsdaFoodDetailCacheTests.cs`
- [behavioral-or-text-match] `Modules/Usda/tests/FoodDiary.Modules.Usda.Infrastructure.Tests/Services/UsdaFoodSearchServiceTests.cs`
- [presentation] `Modules/Usda/tests/FoodDiary.Modules.Usda.Presentation.Tests/UsdaHttpMappingsTests.cs`
- [architecture-boundary] `Tooling/tests/FoodDiary.ArchitectureTests/UsdaModuleExtractionTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
