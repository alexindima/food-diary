---
id: generated.module.lessons
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# Lessons

## Graph

- Origin: extracted-project
- Extracted project: `Modules/Lessons/Application/FoodDiary.Modules.Lessons.Application.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Gamification, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/Lessons/Application`
- `Modules/Lessons/Application.Abstractions`
- `Modules/Lessons/Contracts`
- `Modules/Lessons/Domain`
- `Modules/Lessons/Infrastructure`
- `Modules/Lessons/PersistenceModel`
- `Modules/Lessons/Presentation`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: module-root
- Architecture guardrails: project-reference-matrix-and-module-boundary-tests
- Declared owned entities: NutritionLesson, UserLessonProgress
- Public contract files: 7
- Observed external consumer groups: 2
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 7
- Interfaces: 0
- DTO/read-model/projection types: 1
- Enums: 0
- Exported repository-shaped contracts: 0
- Contracts referencing domain entities: 0
- `record CreateLessonCommand`
- `record DeleteLessonCommand`
- `record GetLessonsForAdministrationQuery`
- `record ImportLessonsCommand`
- `record LessonAdministrationItem`
- `record LessonAdminReadModel`
- `record UpdateLessonCommand`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/Lessons/tests/FoodDiary.Modules.Lessons.Application.Tests/FeatureErrorContractTests.cs`
- [behavioral-or-text-match] `Modules/Lessons/tests/FoodDiary.Modules.Lessons.Application.Tests/LessonsFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Lessons/tests/FoodDiary.Modules.Lessons.Application.Tests/LessonsValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Lessons/tests/FoodDiary.Modules.Lessons.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/Lessons/tests/FoodDiary.Modules.Lessons.Domain.Tests/LessonsDomainInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Lessons/tests/FoodDiary.Modules.Lessons.Infrastructure.Tests/ModuleRegistrationTests.cs`
- [presentation] `Modules/Lessons/tests/FoodDiary.Modules.Lessons.Presentation.Tests/LessonHttpMappingsTests.cs`
- [architecture-boundary] `Tooling/tests/FoodDiary.ArchitectureTests/LessonsModuleExtractionTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
