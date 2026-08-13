---
id: generated.module.tdee
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# Tdee

## Graph

- Origin: extracted-project
- Extracted project: `FoodDiary.Application.Tdee/FoodDiary.Application.Tdee.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Dashboard, Users, WeightEntries
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Application, FoodDiary.Application.Dashboard, FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Tdee`
- `FoodDiary.Presentation.Api/Features/Tdee`

## HTTP Surface

### TdeeController

Source: `FoodDiary.Presentation.Api/Features/Tdee/TdeeController.cs`

- `GET /api/v{version:apiVersion}/tdee`

## Boundary Health

- Role: read-composer
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: not yet enumerated
- Public contract files: 0
- Observed external consumer groups: 6
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 0
- Interfaces: 0
- DTO/read-model/projection types: 0
- Enums: 0
- Exported repository-shaped contracts: 0
- Contracts referencing domain entities: 0
- No public declaration was found in the mapped abstraction areas.

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Tdee/TdeeCalculatorTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Tdee/TdeeFeatureTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Tdee/TdeeValidatorTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/TdeeModuleExtractionTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/TdeeControllerTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/TdeeHttpMappingsTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
