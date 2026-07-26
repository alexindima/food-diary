---
id: generated.module.exercises
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# Exercises

## Graph

- Origin: module-graph
- Dependencies: Users
- Consumers: Dashboard, Tdee

## Source Areas

- `FoodDiary.Application.Abstractions/Exercises`
- `FoodDiary.Application/Exercises`
- `FoodDiary.Infrastructure/Persistence/Configurations/Exercises`
- `FoodDiary.Presentation.Api/Features/Exercises`
- `tests/FoodDiary.Application.Tests/Exercises`

## HTTP Surface

### ExercisesController

Source: `FoodDiary.Presentation.Api/Features/Exercises/ExercisesController.cs`

- `GET /api/v{version:apiVersion}/exercises`
- `POST /api/v{version:apiVersion}/exercises`
- `PUT /api/v{version:apiVersion}/exercises/{id:guid}`
- `DELETE /api/v{version:apiVersion}/exercises/{id:guid}`

## Focused Tests

- `tests/FoodDiary.Application.Tests/Exercises/ExercisesFeatureTests.cs`
- `tests/FoodDiary.Application.Tests/Exercises/ExercisesValidatorTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/ExercisesControllerTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
