---
id: generated.module.weekly-check-in
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# WeeklyCheckIn

## Graph

- Origin: extracted-project
- Extracted project: `Modules/WeeklyCheckIn/Application/FoodDiary.Modules.WeeklyCheckIn.Application.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Dashboard, Meals, Users, WaistEntries, WeightEntries
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/WeeklyCheckIn/Application`
- `Modules/WeeklyCheckIn/Presentation`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: read-composer
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: not yet enumerated
- Public contract files: 0
- Observed external consumer groups: 2
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

- [behavioral-or-text-match] `Modules/WeeklyCheckIn/tests/FoodDiary.Modules.WeeklyCheckIn.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/WeeklyCheckIn/tests/FoodDiary.Modules.WeeklyCheckIn.Application.Tests/WeeklyCheckIn/WeeklyCheckInCalculatorTests.cs`
- [behavioral-or-text-match] `Modules/WeeklyCheckIn/tests/FoodDiary.Modules.WeeklyCheckIn.Application.Tests/WeeklyCheckIn/WeeklyCheckInFeatureTests.cs`
- [presentation] `Modules/WeeklyCheckIn/tests/FoodDiary.Modules.WeeklyCheckIn.Presentation.Tests/WeeklyCheckInHttpMappingsTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/WeeklyCheckInModuleExtractionTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
