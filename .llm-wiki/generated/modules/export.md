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

- Origin: extracted-project
- Extracted project: `Modules/Export/Application/FoodDiary.Modules.Export.Application.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Meals
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Presentation.Api/Features/Export`
- `Modules/Export/Application/Abstractions`
- `Modules/Export/Infrastructure`

## HTTP Surface

### ExportController

Source: `FoodDiary.Presentation.Api/Features/Export/ExportController.cs`

- `GET /api/v{version:apiVersion}/export/diary`
- `GET /api/v{version:apiVersion}/export/cycle`
- `POST /api/v{version:apiVersion}/export/cycle/sensitive`

## Boundary Health

- Role: read-composer
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: not yet enumerated
- Public contract files: 0
- Observed external consumer groups: 4
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

- [behavioral-or-text-match] `Modules/Export/tests/FoodDiary.Modules.Export.Application.Tests/Export/CsvFieldEscaperTests.cs`
- [behavioral-or-text-match] `Modules/Export/tests/FoodDiary.Modules.Export.Application.Tests/Export/ExportFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Export/tests/FoodDiary.Modules.Export.Application.Tests/Export/ExportValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Export/tests/FoodDiary.Modules.Export.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/Export/tests/FoodDiary.Modules.Export.Infrastructure.Tests/ModuleRegistrationTests.cs`
- [behavioral-or-text-match] `Modules/Export/tests/FoodDiary.Modules.Export.Infrastructure.Tests/Services/DiaryPdfGeneratorTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/ExportModuleExtractionTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/ExportControllerTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
