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

- Origin: extracted-project
- Extracted project: `Modules/OpenFoodFacts/Application/FoodDiary.Modules.OpenFoodFacts.Application.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: none observed
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Presentation.Api/Features/OpenFoodFacts`
- `Modules/OpenFoodFacts/Application`
- `Modules/OpenFoodFacts/Application/Abstractions`
- `Modules/OpenFoodFacts/Domain`
- `Modules/OpenFoodFacts/Infrastructure`
- `Modules/OpenFoodFacts/Infrastructure/Model`
- `Modules/OpenFoodFacts/Infrastructure/Providers`

## HTTP Surface

### OpenFoodFactsController

Source: `FoodDiary.Presentation.Api/Features/OpenFoodFacts/OpenFoodFactsController.cs`

- `GET /api/v{version:apiVersion}/open-food-facts/products/{barcode}`
- `GET /api/v{version:apiVersion}/open-food-facts/products`

## Boundary Health

- Role: aggregate-owner
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: OpenFoodFactsProduct
- Public contract files: 4
- Observed external consumer groups: 4
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 4
- Interfaces: 4
- DTO/read-model/projection types: 0
- Enums: 0
- Exported repository-shaped contracts: 3
- Contracts referencing domain entities: 0
- `interface IOpenFoodFactsProductCacheReadRepository`
- `interface IOpenFoodFactsProductCacheRepository`
- `interface IOpenFoodFactsProductCacheWriteRepository`
- `interface IOpenFoodFactsService`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/OpenFoodFacts/tests/FoodDiary.Modules.OpenFoodFacts.Application.Tests/OpenFoodFacts/OpenFoodFactsFeatureTests.cs`
- [behavioral-or-text-match] `Modules/OpenFoodFacts/tests/FoodDiary.Modules.OpenFoodFacts.Application.Tests/OpenFoodFacts/OpenFoodFactsValidatorTests.cs`
- [behavioral-or-text-match] `Modules/OpenFoodFacts/tests/FoodDiary.Modules.OpenFoodFacts.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/OpenFoodFacts/tests/FoodDiary.Modules.OpenFoodFacts.Domain.Tests/Domain/OpenFoodFactsProductTests.cs`
- [behavioral-or-text-match] `Modules/OpenFoodFacts/tests/FoodDiary.Modules.OpenFoodFacts.Infrastructure.Tests/Integrations/OpenFoodFactsExternalFoodServiceTests.cs`
- [behavioral-or-text-match] `Modules/OpenFoodFacts/tests/FoodDiary.Modules.OpenFoodFacts.Infrastructure.Tests/Integrations/ProviderOptionsTests.cs`
- [behavioral-or-text-match] `Modules/OpenFoodFacts/tests/FoodDiary.Modules.OpenFoodFacts.Infrastructure.Tests/Integrations/ProviderRegistrationTests.cs`
- [behavioral-or-text-match] `Modules/OpenFoodFacts/tests/FoodDiary.Modules.OpenFoodFacts.Infrastructure.Tests/ModuleRegistrationTests.cs`
- [behavioral-or-text-match] `Modules/OpenFoodFacts/tests/FoodDiary.Modules.OpenFoodFacts.Infrastructure.Tests/OpenFoodFactsTestCollection.cs`
- [behavioral-or-text-match] `Modules/OpenFoodFacts/tests/FoodDiary.Modules.OpenFoodFacts.Infrastructure.Tests/Services/OpenFoodFactsServiceTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/OpenFoodFactsModuleExtractionTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/OpenFoodFactsControllerTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/OpenFoodFactsHttpMappingsTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
