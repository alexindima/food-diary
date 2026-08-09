---
id: generated.module.waist-entries
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# WaistEntries

## Graph

- Origin: module-graph
- Dependencies: Users
- Consumers: Dashboard, Statistics, WeeklyCheckIn

## Source Areas

- `FoodDiary.Application.Abstractions/WaistEntries`
- `FoodDiary.Application/WaistEntries`
- `FoodDiary.Presentation.Api/Features/WaistEntries`
- `tests/FoodDiary.Application.Tests/WaistEntries`

## HTTP Surface

### WaistEntriesController

Source: `FoodDiary.Presentation.Api/Features/WaistEntries/WaistEntriesController.cs`

- `GET /api/v{version:apiVersion}/waist-entries`
- `GET /api/v{version:apiVersion}/waist-entries/latest`
- `GET /api/v{version:apiVersion}/waist-entries/summary`
- `GET /api/v{version:apiVersion}/waist-entries/page-summary`
- `POST /api/v{version:apiVersion}/waist-entries`
- `PUT /api/v{version:apiVersion}/waist-entries/{id:guid}`
- `DELETE /api/v{version:apiVersion}/waist-entries/{id:guid}`

## Focused Tests

- `tests/FoodDiary.Application.Tests/WaistEntries/WaistEntriesFeatureTests.cs`
- `tests/FoodDiary.Application.Tests/WaistEntries/WaistEntriesValidatorTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
