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

- Origin: extracted-project
- Extracted project: `FoodDiary.Application.WeeklyGoals/FoodDiary.Application.WeeklyGoals.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Notifications, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/WeeklyGoals`
- `FoodDiary.Application.WeeklyGoals`
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
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: not yet enumerated
- Public contract files: 2
- Observed external consumer groups: 4
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 2
- Interfaces: 2
- DTO/read-model/projection types: 0
- Enums: 0
- Exported repository-shaped contracts: 1
- Contracts referencing domain entities: 1
- `interface IWeeklyGoalRepository`
- `interface IWeeklyGoalTransactionRunner`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/WeeklyGoals/WeeklyGoalFeatureTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/WeeklyGoals/WeeklyGoalReminderProcessorTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/WeeklyGoalsModuleExtractionTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Domain.Tests/WeeklyGoals/WeeklyGoalTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/WeeklyGoalsControllerTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
