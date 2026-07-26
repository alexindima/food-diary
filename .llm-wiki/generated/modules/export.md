---
id: generated.module.export
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# Export

## Graph

- Origin: module-graph
- Dependencies: Consumptions, Cycles, Users
- Consumers: none

## Source Areas

- `FoodDiary.Application.Abstractions/Export`
- `FoodDiary.Application/Export`
- `FoodDiary.Presentation.Api/Features/Export`
- `tests/FoodDiary.Application.Tests/Export`

## HTTP Surface

### ExportController

Source: `FoodDiary.Presentation.Api/Features/Export/ExportController.cs`

- `GET /api/v{version:apiVersion}/export/diary`
- `GET /api/v{version:apiVersion}/export/cycle`

## Focused Tests

- `tests/FoodDiary.Application.Tests/Export/ExportFeatureTests.cs`
- `tests/FoodDiary.Application.Tests/Export/ExportValidatorTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/ExportControllerTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
