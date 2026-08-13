---
id: generated.module.gamification
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# Gamification

## Graph

- Origin: extracted-project
- Extracted project: `FoodDiary.Application.Gamification/FoodDiary.Application.Gamification.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Achievements, Dashboard, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Application, FoodDiary.Application.Admin, FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/Achievements`
- `FoodDiary.Application.Gamification`
- `FoodDiary.Infrastructure/Persistence/Achievements`
- `FoodDiary.Infrastructure/Persistence/Configurations/Achievements`
- `FoodDiary.Presentation.Api/Features/Gamification`

## HTTP Surface

### GamificationController

Source: `FoodDiary.Presentation.Api/Features/Gamification/GamificationController.cs`

- `GET /api/v{version:apiVersion}/gamification`

## Boundary Health

- Role: read-composer
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: not yet enumerated
- Public contract files: 8
- Observed external consumer groups: 6
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 8
- Interfaces: 6
- DTO/read-model/projection types: 1
- Enums: 0
- Exported repository-shaped contracts: 2
- Contracts referencing domain entities: 2
- `class AchievementDefinitionErrors`
- `interface IAchievementDefinitionStore`
- `interface IAchievementEvaluationOutbox`
- `interface IAchievementEvaluationOutboxProcessor`
- `interface IAchievementMetricReader`
- `interface IAchievementReconciliationHandler`
- `interface IUserAchievementStore`
- `record AchievementGrantModel`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Gamification/AchievementAwardServiceTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Gamification/AchievementDefinitionAdministrationServiceTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Gamification/AchievementReconciliationHandlerTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Gamification/GamificationCalculatorTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Gamification/GamificationFeatureTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/GamificationModuleExtractionTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/GamificationHttpMappingsTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
