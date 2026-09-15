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
- Abstraction-contract dependencies: Authentication, Favorites, Images, OpenFoodFacts, RecentItems, Usda, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/Products/Application`
- `Modules/Products/Application.Abstractions`
- `Modules/Products/Contracts`
- `Modules/Products/PersistenceModel`
- `Modules/Products/Presentation`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: module-root
- Architecture guardrails: project-reference-matrix
- Declared owned entities: Product
- Public contract files: 12
- Observed external consumer groups: 3
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 12
- Interfaces: 8
- DTO/read-model/projection types: 1
- Enums: 0
- Exported repository-shaped contracts: 3
- Contracts referencing domain entities: 0
- `class ProductErrors`
- `interface IProductLookupService`
- `interface IProductMutationTransactionRunner`
- `interface IProductOverviewReadService`
- `interface IProductReadRepository`
- `interface IProductRepository`
- `interface IProductSnapshotReadService`
- `interface IProductUsageQuery`
- `interface IProductWriteRepository`
- `record ProductOverviewReadItem`
- `record ProductQueryFilters`
- `record ProductSnapshotReadModel`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Application.Tests/ApplicationDependencyInjectionTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Application.Tests/CentralRelocated/ProductSearchSuggestionTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Application.Tests/CentralRelocated/ProductsFeatureTests.ReadQueryTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Application.Tests/CentralRelocated/ProductsFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Application.Tests/FeatureErrorContractTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Application.Tests/Nutrition/NutritionMappingCompatibilityTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Application.Tests/ProductRepositoryDefaultMethodTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Application.Tests/ProductSearchSuggestionTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Application.Tests/ProductUsdaLinkServiceTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Application.Tests/ProductsFeatureTests.CreateCommandTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Application.Tests/ProductsFeatureTests.DeleteAndDuplicateCommandTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Application.Tests/ProductsFeatureTests.MappingTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Application.Tests/ProductsFeatureTests.ReadQueryTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Application.Tests/ProductsFeatureTests.UpdateCommandTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Application.Tests/ProductsFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Application.Tests/ProductsValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Application.Tests/Support/AllowImageAssetAccessService.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Application.Tests/Support/RecordingImageAssetAccessService.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Domain.Tests/FoodQualityScoreBoundaryTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Domain.Tests/ProductExtractedInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Domain.Tests/ProductInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Domain.Tests/ProductsIdConversionTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Domain.Tests/ValueObjects/FoodQualityScoreTests.cs`
- [behavioral-or-text-match] `Modules/Products/tests/FoodDiary.Modules.Products.Domain.Tests/ValueObjects/NutritionContractTests.cs`
- [integration] `Modules/Products/tests/FoodDiary.Modules.Products.Infrastructure.IntegrationTests/Integration/ProductRepositoryIntegrationTests.cs`
- [integration] `Modules/Products/tests/FoodDiary.Modules.Products.Infrastructure.IntegrationTests/Integration/ProductSnapshotReadServiceIntegrationTests.cs`
- [presentation] `Modules/Products/tests/FoodDiary.Modules.Products.Presentation.Tests/ProductHttpMappingsTests.cs`
- [presentation] `Modules/Products/tests/FoodDiary.Modules.Products.Presentation.Tests/ProductSuggestionsControllerTests.cs`
- [architecture-boundary] `Tooling/tests/FoodDiary.ArchitectureTests/ProductsModuleExtractionTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
