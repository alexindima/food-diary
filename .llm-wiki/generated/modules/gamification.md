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
- Abstraction-contract dependencies: Meals, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/Gamification/Application`
- `Modules/Gamification/Application.Abstractions`
- `Modules/Gamification/Contracts`
- `Modules/Gamification/Presentation`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: module-root
- Architecture guardrails: project-reference-matrix
- Declared owned entities: AchievementDefinition, UserAchievement, AchievementEvaluationOutboxMessage
- Public contract files: 15
- Observed external consumer groups: 3
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 15
- Interfaces: 6
- DTO/read-model/projection types: 2
- Enums: 0
- Exported repository-shaped contracts: 3
- Contracts referencing domain entities: 0
- `class AchievementDefinitionErrors`
- `interface IAchievementDefinitionReadModelRepository`
- `interface IAchievementDefinitionStore`
- `interface IAchievementEvaluationOutbox`
- `interface IAchievementEvaluationOutboxProcessor`
- `interface IAchievementMetricReader`
- `interface IUserAchievementStore`
- `record AchievementDefinitionAdminModel`
- `record AchievementDefinitionCreateInput`
- `record AchievementDefinitionUpdateInput`
- `record AchievementGrantModel`
- `record CreateAchievementDefinitionCommand`
- `record GetAchievementDefinitionsForAdministrationQuery`
- `record ReconcileAchievementsCommand`
- `record UpdateAchievementDefinitionCommand`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/Gamification/tests/FoodDiary.Modules.Gamification.Application.Tests/AchievementAwardServiceTests.cs`
- [behavioral-or-text-match] `Modules/Gamification/tests/FoodDiary.Modules.Gamification.Application.Tests/AchievementDefinitionAdministrationServiceTests.cs`
- [behavioral-or-text-match] `Modules/Gamification/tests/FoodDiary.Modules.Gamification.Application.Tests/GamificationCalculatorTests.cs`
- [behavioral-or-text-match] `Modules/Gamification/tests/FoodDiary.Modules.Gamification.Application.Tests/GamificationFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Gamification/tests/FoodDiary.Modules.Gamification.Application.Tests/ReconcileAchievementsCommandHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Gamification/tests/FoodDiary.Modules.Gamification.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/Gamification/tests/FoodDiary.Modules.Gamification.Domain.Tests/Achievements/AchievementDefinitionTests.cs`
- [behavioral-or-text-match] `Modules/Gamification/tests/FoodDiary.Modules.Gamification.Domain.Tests/Achievements/UserAchievementTests.cs`
- [behavioral-or-text-match] `Modules/Gamification/tests/FoodDiary.Modules.Gamification.Domain.Tests/GamificationIdConversionTests.cs`
- [behavioral-or-text-match] `Modules/Gamification/tests/FoodDiary.Modules.Gamification.Infrastructure.Tests/Persistence/AchievementPersistenceTests.cs`
- [behavioral-or-text-match] `Modules/Gamification/tests/FoodDiary.Modules.Gamification.Infrastructure.Tests/Persistence/OutboxReplayStreamTests.cs`
- [presentation] `Modules/Gamification/tests/FoodDiary.Modules.Gamification.Presentation.Tests/GamificationHttpMappingsTests.cs`
- [architecture-boundary] `Tooling/tests/FoodDiary.ArchitectureTests/GamificationModuleExtractionTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
