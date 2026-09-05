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
- Extracted project: `Modules/WeeklyGoals/Application/FoodDiary.Modules.WeeklyGoals.Application.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Meals, Notifications, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/WeeklyGoals/Application`
- `Modules/WeeklyGoals/Application/Abstractions`
- `Modules/WeeklyGoals/Presentation`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: WeeklyGoal
- Public contract files: 2
- Observed external consumer groups: 3
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

- [behavioral-or-text-match] `Modules/WeeklyGoals/tests/FoodDiary.Modules.WeeklyGoals.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/WeeklyGoals/tests/FoodDiary.Modules.WeeklyGoals.Application.Tests/WeeklyGoals/WeeklyGoalFeatureTests.cs`
- [behavioral-or-text-match] `Modules/WeeklyGoals/tests/FoodDiary.Modules.WeeklyGoals.Application.Tests/WeeklyGoals/WeeklyGoalReminderProcessorTests.cs`
- [behavioral-or-text-match] `Modules/WeeklyGoals/tests/FoodDiary.Modules.WeeklyGoals.Domain.Tests/Domain/WeeklyGoalIdInvariantTests.cs`
- [behavioral-or-text-match] `Modules/WeeklyGoals/tests/FoodDiary.Modules.WeeklyGoals.Domain.Tests/Domain/WeeklyGoalInvariantTests.cs`
- [behavioral-or-text-match] `Modules/WeeklyGoals/tests/FoodDiary.Modules.WeeklyGoals.Domain.Tests/WeeklyGoals/WeeklyGoalTests.cs`
- [behavioral-or-text-match] `Modules/WeeklyGoals/tests/FoodDiary.Modules.WeeklyGoals.Infrastructure.Tests/PostgresDatabaseCollection.cs`
- [behavioral-or-text-match] `Modules/WeeklyGoals/tests/FoodDiary.Modules.WeeklyGoals.Infrastructure.Tests/PostgresDatabaseFixture.cs`
- [integration] `Modules/WeeklyGoals/tests/FoodDiary.Modules.WeeklyGoals.Infrastructure.Tests/WeeklyGoalRepositoryIntegrationTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/WeeklyGoalsModuleExtractionTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/WeeklyGoalsControllerTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
