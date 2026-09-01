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
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Presentation.Api/Features/Hydration`
- `Modules/Hydration/Application`
- `Modules/Hydration/Application/Abstractions`

## HTTP Surface

### HydrationEntriesController

Source: `FoodDiary.Presentation.Api/Features/Hydration/HydrationEntriesController.cs`

- `GET /api/v{version:apiVersion}/hydrations`
- `GET /api/v{version:apiVersion}/hydrations/daily`
- `POST /api/v{version:apiVersion}/hydrations`
- `PUT /api/v{version:apiVersion}/hydrations/{id:guid}`
- `DELETE /api/v{version:apiVersion}/hydrations/{id:guid}`

## Boundary Health

- Role: aggregate-owner
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: HydrationEntry
- Public contract files: 4
- Observed external consumer groups: 3
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 4
- Interfaces: 3
- DTO/read-model/projection types: 1
- Enums: 0
- Exported repository-shaped contracts: 2
- Contracts referencing domain entities: 1
- `interface IHydrationEntryReadModelRepository`
- `interface IHydrationEntryWriteRepository`
- `interface IHydrationGoalService`
- `record HydrationEntryReadModel`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/Hydration/tests/FoodDiary.Modules.Hydration.Application.Tests/Hydration/HydrationEntryReadModelTests.cs`
- [behavioral-or-text-match] `Modules/Hydration/tests/FoodDiary.Modules.Hydration.Application.Tests/Hydration/HydrationFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Hydration/tests/FoodDiary.Modules.Hydration.Application.Tests/Hydration/HydrationValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Hydration/tests/FoodDiary.Modules.Hydration.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/Hydration/tests/FoodDiary.Modules.Hydration.Domain.Tests/Domain/HydrationEntryInvariantTests.cs`
- [integration] `Modules/Hydration/tests/FoodDiary.Modules.Hydration.Infrastructure.Tests/HydrationEntryRepositoryIntegrationTests.cs`
- [behavioral-or-text-match] `Modules/Hydration/tests/FoodDiary.Modules.Hydration.Infrastructure.Tests/PostgresDatabaseCollection.cs`
- [behavioral-or-text-match] `Modules/Hydration/tests/FoodDiary.Modules.Hydration.Infrastructure.Tests/PostgresDatabaseFixture.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/HydrationModuleExtractionTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/HydrationHttpMappingsTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
