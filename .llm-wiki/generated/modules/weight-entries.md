---
id: generated.module.weight-entries
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# WeightEntries

## Graph

- Origin: module-graph
- Dependencies: Users
- Consumers: Dashboard, Statistics, Tdee, WeeklyCheckIn

## Source Areas

- `FoodDiary.Application.Abstractions/WeightEntries`
- `FoodDiary.Application/WeightEntries`
- `FoodDiary.Presentation.Api/Features/WeightEntries`
- `tests/FoodDiary.Application.Tests/WeightEntries`

## HTTP Surface

### WeightEntriesController

Source: `FoodDiary.Presentation.Api/Features/WeightEntries/WeightEntriesController.cs`

- `GET /api/v{version:apiVersion}/weight-entries`
- `GET /api/v{version:apiVersion}/weight-entries/latest`
- `GET /api/v{version:apiVersion}/weight-entries/summary`
- `GET /api/v{version:apiVersion}/weight-entries/page-summary`
- `POST /api/v{version:apiVersion}/weight-entries`
- `PUT /api/v{version:apiVersion}/weight-entries/{id:guid}`
- `DELETE /api/v{version:apiVersion}/weight-entries/{id:guid}`

## Focused Tests

- `tests/FoodDiary.Application.Tests/WeightEntries/WeightEntriesFeatureTests.cs`
- `tests/FoodDiary.Application.Tests/WeightEntries/WeightEntriesValidatorTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
