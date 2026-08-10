---
id: generated.module.weekly-goals
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# WeeklyGoals

## Graph

- Origin: module-graph
- Dependencies: Consumptions, Notifications, Users
- Consumers: none

## Source Areas

- `FoodDiary.Application.Abstractions/WeeklyGoals`
- `FoodDiary.Application/WeeklyGoals`
- `FoodDiary.Domain/Entities/WeeklyGoals`
- `FoodDiary.Infrastructure/Persistence/Configurations/WeeklyGoals`
- `FoodDiary.Infrastructure/Persistence/WeeklyGoals`
- `FoodDiary.Presentation.Api/Features/WeeklyGoals`
- `tests/FoodDiary.Application.Tests/WeeklyGoals`

## HTTP Surface

### WeeklyGoalsController

Source: `FoodDiary.Presentation.Api/Features/WeeklyGoals/WeeklyGoalsController.cs`

- `GET /api/v{version:apiVersion}/weekly-goals`
- `PUT /api/v{version:apiVersion}/weekly-goals`

## Focused Tests

- `tests/FoodDiary.Application.Tests/WeeklyGoals/WeeklyGoalReminderProcessorTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
