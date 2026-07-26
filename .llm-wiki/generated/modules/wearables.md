---
id: generated.module.wearables
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# Wearables

## Graph

- Origin: module-graph
- Dependencies: Users
- Consumers: none

## Source Areas

- `FoodDiary.Application.Abstractions/Wearables`
- `FoodDiary.Application/Wearables`
- `FoodDiary.Domain/Entities/Wearables`
- `FoodDiary.Infrastructure/Persistence/Configurations/Wearables`
- `FoodDiary.Infrastructure/Persistence/Wearables`
- `FoodDiary.Integrations/Wearables`
- `FoodDiary.Presentation.Api/Features/Wearables`
- `FoodDiary.Web.Client/src/app/features/wearables`
- `tests/FoodDiary.Application.Tests/Wearables`

## HTTP Surface

### WearablesController

Source: `FoodDiary.Presentation.Api/Features/Wearables/WearablesController.cs`

- `GET /api/v{version:apiVersion}/wearables/connections`
- `GET /api/v{version:apiVersion}/wearables/{provider}/auth-url`
- `POST /api/v{version:apiVersion}/wearables/{provider}/connect`
- `DELETE /api/v{version:apiVersion}/wearables/{provider}/disconnect`
- `POST /api/v{version:apiVersion}/wearables/{provider}/sync`
- `GET /api/v{version:apiVersion}/wearables/daily-summary`

## Focused Tests

- `tests/FoodDiary.Application.Tests/Wearables/WearablesFeatureTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/WearablesControllerTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
