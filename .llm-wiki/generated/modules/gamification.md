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
- Extracted project: `Modules/Gamification/Application/FoodDiary.Modules.Gamification.Application.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Achievements, Dashboard, Meals, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Application.Admin, FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Presentation.Api/Features/Gamification`
- `Modules/Gamification/Application`

## HTTP Surface

### GamificationController

Source: `FoodDiary.Presentation.Api/Features/Gamification/GamificationController.cs`

- `GET /api/v{version:apiVersion}/gamification`

## Boundary Health

- Role: read-composer
- Physical isolation: module-root
- Architecture guardrails: project-reference-matrix
- Declared owned entities: not yet enumerated
- Public contract files: 0
- Observed external consumer groups: 5
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 0
- Interfaces: 0
- DTO/read-model/projection types: 0
- Enums: 0
- Exported repository-shaped contracts: 0
- Contracts referencing domain entities: 0
- No public declaration was found in the mapped abstraction areas.

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/Gamification/tests/FoodDiary.Modules.Gamification.Application.Tests/Gamification/AchievementAwardServiceTests.cs`
- [behavioral-or-text-match] `Modules/Gamification/tests/FoodDiary.Modules.Gamification.Application.Tests/Gamification/AchievementDefinitionAdministrationServiceTests.cs`
- [behavioral-or-text-match] `Modules/Gamification/tests/FoodDiary.Modules.Gamification.Application.Tests/Gamification/AchievementReconciliationHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Gamification/tests/FoodDiary.Modules.Gamification.Application.Tests/Gamification/GamificationCalculatorTests.cs`
- [behavioral-or-text-match] `Modules/Gamification/tests/FoodDiary.Modules.Gamification.Application.Tests/Gamification/GamificationFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Gamification/tests/FoodDiary.Modules.Gamification.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/Gamification/tests/FoodDiary.Modules.Gamification.Domain.Tests/Achievements/AchievementDefinitionTests.cs`
- [behavioral-or-text-match] `Modules/Gamification/tests/FoodDiary.Modules.Gamification.Domain.Tests/Achievements/UserAchievementTests.cs`
- [behavioral-or-text-match] `Modules/Gamification/tests/FoodDiary.Modules.Gamification.Infrastructure.Tests/Persistence/AchievementPersistenceTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/GamificationModuleExtractionTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/GamificationHttpMappingsTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
