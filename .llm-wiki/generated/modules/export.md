---
id: generated.module.export
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# Export

## Graph

- Origin: module-graph
- Business-module dependencies: Consumptions, Cycles, Users
- Abstraction-contract dependencies: Meals, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/Export`
- `FoodDiary.Application/Export`
- `FoodDiary.Presentation.Api/Features/Export`

## HTTP Surface

### ExportController

Source: `FoodDiary.Presentation.Api/Features/Export/ExportController.cs`

- `GET /api/v{version:apiVersion}/export/diary`
- `GET /api/v{version:apiVersion}/export/cycle`

## Boundary Health

- Role: read-composer
- Physical isolation: folder
- Architecture guardrails: graph-only
- Declared owned entities: not yet enumerated
- Public contract files: 5
- Observed external consumer groups: 2
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 5
- Interfaces: 3
- DTO/read-model/projection types: 1
- Enums: 0
- Exported repository-shaped contracts: 0
- Contracts referencing domain entities: 0
- `interface IDiaryPdfGenerator`
- `interface IDiaryPdfReportTextProvider`
- `interface IExportDiaryReadService`
- `record DiaryPdfReportTexts`
- `record ExportDiaryMealsReadModel`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Export/ExportFeatureTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Export/ExportValidatorTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/ExportControllerTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
