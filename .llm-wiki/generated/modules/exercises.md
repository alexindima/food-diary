---
id: generated.module.exercises
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# Exercises

## Graph

- Origin: extracted-project
- Extracted project: `Modules/Exercises/Application/FoodDiary.Application.Exercises.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/Exercises/Application`
- `Modules/Exercises/Application/Abstractions`
- `Modules/Exercises/Application/Abstractions/Exercises`
- `Modules/Exercises/Contracts`
- `Modules/Exercises/Domain`
- `Modules/Exercises/Infrastructure`
- `Modules/Exercises/Infrastructure/Model`
- `Modules/Exercises/Presentation`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: ExerciseEntry
- Public contract files: 8
- Observed external consumer groups: 3
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 8
- Interfaces: 5
- DTO/read-model/projection types: 2
- Enums: 0
- Exported repository-shaped contracts: 4
- Contracts referencing domain entities: 2
- `class ExerciseErrors`
- `interface IExerciseEntryReadModelRepository`
- `interface IExerciseEntryReadRepository`
- `interface IExerciseEntryReadService`
- `interface IExerciseEntryRepository`
- `interface IExerciseEntryWriteRepository`
- `record ExerciseEntryModel`
- `record ExerciseEntryReadModel`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/Exercises/tests/FoodDiary.Modules.Exercises.Application.Tests/ExerciseErrorContractTests.cs`
- [behavioral-or-text-match] `Modules/Exercises/tests/FoodDiary.Modules.Exercises.Application.Tests/Exercises/ExerciseEntryInputValidationCoverageTests.cs`
- [behavioral-or-text-match] `Modules/Exercises/tests/FoodDiary.Modules.Exercises.Application.Tests/Exercises/ExercisesFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Exercises/tests/FoodDiary.Modules.Exercises.Application.Tests/Exercises/ExercisesValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Exercises/tests/FoodDiary.Modules.Exercises.Domain.Tests/Domain/ExerciseEntryInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Exercises/tests/FoodDiary.Modules.Exercises.Domain.Tests/Domain/ExerciseTrackingInvariantTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/ExercisesModuleExtractionTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/ExercisesControllerTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
