---
id: generated.module.cycles
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# Cycles

## Graph

- Origin: extracted-project
- Extracted project: `FoodDiary.Application.Cycles/FoodDiary.Application.Cycles.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Dashboard, Users
- Business-module consumers: Dashboard
- Host/adapter consumers: FoodDiary.Application, FoodDiary.Application.Export, FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/Cycles`
- `FoodDiary.Application.Cycles`
- `FoodDiary.Infrastructure/Persistence/Configurations/Cycles`
- `FoodDiary.Presentation.Api/Features/Cycles`

## HTTP Surface

### CyclesController

Source: `FoodDiary.Presentation.Api/Features/Cycles/CyclesController.cs`

- `GET /api/v{version:apiVersion}/cycles/current`
- `GET /api/v{version:apiVersion}/cycles/current/nutrition-summary`
- `POST /api/v{version:apiVersion}/cycles`
- `PUT /api/v{version:apiVersion}/cycles/{cycleProfileId:guid}/days`
- `DELETE /api/v{version:apiVersion}/cycles/{cycleProfileId:guid}/days`
- `PUT /api/v{version:apiVersion}/cycles/{cycleProfileId:guid}/factors`

## Boundary Health

- Role: aggregate-owner
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: not yet enumerated
- Public contract files: 11
- Observed external consumer groups: 7
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 11
- Interfaces: 4
- DTO/read-model/projection types: 5
- Enums: 0
- Exported repository-shaped contracts: 4
- Contracts referencing domain entities: 2
- `class CycleDayErrors`
- `class CycleErrors`
- `interface ICycleReadModelRepository`
- `interface ICycleReadRepository`
- `interface ICycleRepository`
- `interface ICycleWriteRepository`
- `record BleedingEntryReadModel`
- `record CycleFactorReadModel`
- `record CycleProfileReadModel`
- `record CycleSymptomEntryReadModel`
- `record FertilitySignalReadModel`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Cycles/CyclesFeatureTests.CreateAndRead.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Cycles/CyclesFeatureTests.DayCommands.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Cycles/CyclesFeatureTests.FactorCommands.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Cycles/CyclesFeatureTests.MappingAndPrediction.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Cycles/CyclesFeatureTests.NutritionSummary.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Cycles/CyclesFeatureTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Cycles/CyclesValidatorTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/CyclesModuleExtractionTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/CyclesControllerCoverageTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
