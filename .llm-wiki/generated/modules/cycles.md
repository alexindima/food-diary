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

- Origin: module-graph
- Business-module dependencies: Users
- Abstraction-contract dependencies: Dashboard, Users
- Business-module consumers: Dashboard, Export
- Host/adapter consumers: FoodDiary.Presentation.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/Cycles`
- `FoodDiary.Application/Cycles`
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
- Physical isolation: folder
- Architecture guardrails: graph-only
- Declared owned entities: not yet enumerated
- Public contract files: 4
- Observed external consumer groups: 3
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 4
- Exported repository-shaped contracts: 4
- `interface ICycleReadModelRepository`
- `interface ICycleReadRepository`
- `interface ICycleRepository`
- `interface ICycleWriteRepository`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Cycles/CyclesFeatureTests.CreateAndRead.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Cycles/CyclesFeatureTests.DayCommands.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Cycles/CyclesFeatureTests.FactorCommands.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Cycles/CyclesFeatureTests.MappingAndPrediction.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Cycles/CyclesFeatureTests.NutritionSummary.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Cycles/CyclesFeatureTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Cycles/CyclesValidatorTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/CyclesControllerCoverageTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
