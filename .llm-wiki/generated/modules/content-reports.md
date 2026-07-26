---
id: generated.module.content-reports
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# ContentReports

## Graph

- Origin: module-graph
- Dependencies: Users
- Consumers: Admin

## Source Areas

- `FoodDiary.Application.Abstractions/ContentReports`
- `FoodDiary.Application/ContentReports`
- `FoodDiary.Infrastructure/Persistence/Configurations/ContentReports`
- `FoodDiary.Infrastructure/Persistence/ContentReports`
- `FoodDiary.Presentation.Api/Features/ContentReports`
- `tests/FoodDiary.Application.Tests/ContentReports`

## HTTP Surface

### ContentReportsController

Source: `FoodDiary.Presentation.Api/Features/ContentReports/ContentReportsController.cs`

- `POST /api/v{version:apiVersion}/reports`

## Focused Tests

- `tests/FoodDiary.Application.Tests/ContentReports/ContentReportsFeatureTests.cs`
- `tests/FoodDiary.Application.Tests/ContentReports/ContentReportsValidatorTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
