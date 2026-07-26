---
id: generated.module.lessons
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# Lessons

## Graph

- Origin: module-graph
- Dependencies: Users
- Consumers: Admin

## Source Areas

- `FoodDiary.Application.Abstractions/Lessons`
- `FoodDiary.Application/Lessons`
- `FoodDiary.Infrastructure/Persistence/Configurations/Lessons`
- `FoodDiary.Presentation.Api/Features/Lessons`
- `FoodDiary.Web.Client/src/app/features/lessons`
- `tests/FoodDiary.Application.Tests/Lessons`

## HTTP Surface

### LessonsController

Source: `FoodDiary.Presentation.Api/Features/Lessons/LessonsController.cs`

- `GET /api/v{version:apiVersion}/lessons`
- `GET /api/v{version:apiVersion}/lessons/{id:guid}`
- `POST /api/v{version:apiVersion}/lessons/{id:guid}/read`

## Focused Tests

- `tests/FoodDiary.Application.Tests/Lessons/LessonsFeatureTests.cs`
- `tests/FoodDiary.Application.Tests/Lessons/LessonsValidatorTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
