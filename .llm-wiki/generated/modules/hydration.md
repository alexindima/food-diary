---
id: generated.module.hydration
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# Hydration

## Graph

- Origin: extracted-project
- Extracted project: `Modules/Hydration/Application/FoodDiary.Modules.Hydration.Application.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/Hydration/Application`
- `Modules/Hydration/Application.Abstractions`
- `Modules/Hydration/Presentation`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: HydrationEntry
- Public contract files: 7
- Observed external consumer groups: 2
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 7
- Interfaces: 5
- DTO/read-model/projection types: 1
- Enums: 0
- Exported repository-shaped contracts: 4
- Contracts referencing domain entities: 0
- `class HydrationEntryErrors`
- `interface IHydrationEntryReadModelRepository`
- `interface IHydrationEntryWriteRepository`
- `interface IHydrationIntervalReadModelRepository`
- `interface IHydrationOperationReceiptRepository`
- `interface IHydrationOperationTransactionRunner`
- `record HydrationEntryReadModel`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/Hydration/tests/FoodDiary.Modules.Hydration.Application.Tests/CreateHydrationFromOperationTests.cs`
- [behavioral-or-text-match] `Modules/Hydration/tests/FoodDiary.Modules.Hydration.Application.Tests/HydrationEntryReadModelTests.cs`
- [behavioral-or-text-match] `Modules/Hydration/tests/FoodDiary.Modules.Hydration.Application.Tests/HydrationErrorContractTests.cs`
- [behavioral-or-text-match] `Modules/Hydration/tests/FoodDiary.Modules.Hydration.Application.Tests/HydrationFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Hydration/tests/FoodDiary.Modules.Hydration.Application.Tests/HydrationValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Hydration/tests/FoodDiary.Modules.Hydration.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/Hydration/tests/FoodDiary.Modules.Hydration.Domain.Tests/HydrationEntryInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Hydration/tests/FoodDiary.Modules.Hydration.Domain.Tests/HydrationIdConversionTests.cs`
- [behavioral-or-text-match] `Modules/Hydration/tests/FoodDiary.Modules.Hydration.Domain.Tests/HydrationOperationReceiptTests.cs`
- [integration] `Modules/Hydration/tests/FoodDiary.Modules.Hydration.Infrastructure.Tests/HydrationDbContextIntegrationTests.cs`
- [integration] `Modules/Hydration/tests/FoodDiary.Modules.Hydration.Infrastructure.Tests/HydrationEntryRepositoryIntegrationTests.cs`
- [integration] `Modules/Hydration/tests/FoodDiary.Modules.Hydration.Infrastructure.Tests/HydrationIntervalReadServiceIntegrationTests.cs`
- [integration] `Modules/Hydration/tests/FoodDiary.Modules.Hydration.Infrastructure.Tests/HydrationOperationReceiptIntegrationTests.cs`
- [behavioral-or-text-match] `Modules/Hydration/tests/FoodDiary.Modules.Hydration.Infrastructure.Tests/PostgresDatabaseCollection.cs`
- [behavioral-or-text-match] `Modules/Hydration/tests/FoodDiary.Modules.Hydration.Infrastructure.Tests/PostgresDatabaseFixture.cs`
- [presentation] `Modules/Hydration/tests/FoodDiary.Modules.Hydration.Presentation.Tests/HydrationHttpMappingsTests.cs`
- [presentation] `Modules/Hydration/tests/FoodDiary.Modules.Hydration.Presentation.Tests/HydrationOperationControllerTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/HydrationModuleExtractionTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
