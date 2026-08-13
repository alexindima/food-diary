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
- Extracted project: `FoodDiary.Application.Hydration/FoodDiary.Application.Hydration.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Users
- Business-module consumers: Dashboard
- Host/adapter consumers: FoodDiary.Application, FoodDiary.Application.WeeklyCheckIn, FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/Hydration`
- `FoodDiary.Application.Hydration`
- `FoodDiary.Infrastructure/Persistence/Configurations/Hydration`
- `FoodDiary.Presentation.Api/Features/Hydration`

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
- Public contract files: 7
- Observed external consumer groups: 7
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 7
- Interfaces: 5
- DTO/read-model/projection types: 1
- Enums: 0
- Exported repository-shaped contracts: 4
- Contracts referencing domain entities: 2
- `class HydrationEntryErrors`
- `interface IHydrationEntryReadModelRepository`
- `interface IHydrationEntryReadRepository`
- `interface IHydrationEntryRepository`
- `interface IHydrationEntryWriteRepository`
- `interface IHydrationGoalService`
- `record HydrationEntryReadModel`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Hydration/HydrationEntryReadModelTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Hydration/HydrationFeatureTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Hydration/HydrationValidatorTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/HydrationModuleExtractionTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Domain.Tests/Domain/HydrationEntryInvariantTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/HydrationHttpMappingsTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
