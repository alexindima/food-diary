---
id: generated.module.hydration
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# Hydration

## Graph

- Origin: module-graph
- Dependencies: Users
- Consumers: Dashboard, WeeklyCheckIn

## Source Areas

- `FoodDiary.Application.Abstractions/Hydration`
- `FoodDiary.Application/Hydration`
- `FoodDiary.Infrastructure/Persistence/Configurations/Hydration`
- `FoodDiary.Presentation.Api/Features/Hydration`
- `FoodDiary.Web.Client/src/app/features/hydration`
- `tests/FoodDiary.Application.Tests/Hydration`

## HTTP Surface

### HydrationEntriesController

Source: `FoodDiary.Presentation.Api/Features/Hydration/HydrationEntriesController.cs`

- `GET /api/v{version:apiVersion}/hydrations`
- `GET /api/v{version:apiVersion}/hydrations/daily`
- `POST /api/v{version:apiVersion}/hydrations`
- `PUT /api/v{version:apiVersion}/hydrations/{id:guid}`
- `DELETE /api/v{version:apiVersion}/hydrations/{id:guid}`

## Focused Tests

- `tests/FoodDiary.Application.Tests/Hydration/HydrationEntryReadModelTests.cs`
- `tests/FoodDiary.Application.Tests/Hydration/HydrationFeatureTests.cs`
- `tests/FoodDiary.Application.Tests/Hydration/HydrationValidatorTests.cs`
- `tests/FoodDiary.Domain.Tests/Domain/HydrationEntryInvariantTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/HydrationHttpMappingsTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
