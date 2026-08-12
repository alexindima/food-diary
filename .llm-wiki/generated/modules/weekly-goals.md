---
id: generated.module.weekly-goals
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# WeeklyGoals

## Graph

- Origin: module-graph
- Business-module dependencies: Consumptions
- Abstraction-contract dependencies: Notifications, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.JobManager, FoodDiary.Presentation.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/WeeklyGoals`
- `FoodDiary.Application/WeeklyGoals`
- `FoodDiary.Domain/Entities/WeeklyGoals`
- `FoodDiary.Infrastructure/Persistence/Configurations/WeeklyGoals`
- `FoodDiary.Infrastructure/Persistence/WeeklyGoals`
- `FoodDiary.Presentation.Api/Features/WeeklyGoals`

## HTTP Surface

### WeeklyGoalsController

Source: `FoodDiary.Presentation.Api/Features/WeeklyGoals/WeeklyGoalsController.cs`

- `GET /api/v{version:apiVersion}/weekly-goals`
- `PUT /api/v{version:apiVersion}/weekly-goals`

## Boundary Health

- Role: aggregate-owner
- Physical isolation: folder
- Architecture guardrails: graph-only
- Declared owned entities: not yet enumerated
- Public contract files: 1
- Observed external consumer groups: 2
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 1
- Interfaces: 1
- DTO/read-model/projection types: 0
- Enums: 0
- Exported repository-shaped contracts: 1
- Contracts referencing domain entities: 1
- `interface IWeeklyGoalRepository`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/WeeklyGoals/WeeklyGoalFeatureTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/WeeklyGoals/WeeklyGoalReminderProcessorTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Domain.Tests/WeeklyGoals/WeeklyGoalTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/WeeklyGoalsControllerTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
