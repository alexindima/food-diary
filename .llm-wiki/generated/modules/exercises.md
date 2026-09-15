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
- Extracted project: `Modules/Exercises/Application/FoodDiary.Modules.Exercises.Application.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/Exercises/Application`
- `Modules/Exercises/Application.Abstractions`
- `Modules/Exercises/Contracts`
- `Modules/Exercises/Domain`
- `Modules/Exercises/Infrastructure`
- `Modules/Exercises/PersistenceModel`
- `Modules/Exercises/Presentation`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: ExerciseEntry
- Public contract files: 9
- Observed external consumer groups: 3
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 9
- Interfaces: 4
- DTO/read-model/projection types: 2
- Enums: 0
- Exported repository-shaped contracts: 4
- Contracts referencing domain entities: 0
- `class ExerciseErrors`
- `interface IExerciseEntryReadModelRepository`
- `interface IExerciseEntryReadRepository`
- `interface IExerciseEntryRepository`
- `interface IExerciseEntryWriteRepository`
- `record ExerciseEntryModel`
- `record ExerciseEntryReadModel`
- `record ReadExerciseCaloriesQuery`
- `record ReadExerciseEntriesQuery`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/Exercises/tests/FoodDiary.Modules.Exercises.Application.Tests/ExerciseEntryInputValidationCoverageTests.cs`
- [behavioral-or-text-match] `Modules/Exercises/tests/FoodDiary.Modules.Exercises.Application.Tests/ExerciseErrorContractTests.cs`
- [behavioral-or-text-match] `Modules/Exercises/tests/FoodDiary.Modules.Exercises.Application.Tests/ExercisesFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Exercises/tests/FoodDiary.Modules.Exercises.Application.Tests/ExercisesValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Exercises/tests/FoodDiary.Modules.Exercises.Domain.Tests/ExerciseEntryInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Exercises/tests/FoodDiary.Modules.Exercises.Domain.Tests/ExerciseTrackingInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Exercises/tests/FoodDiary.Modules.Exercises.Domain.Tests/ExercisesIdConversionTests.cs`
- [presentation] `Modules/Exercises/tests/FoodDiary.Modules.Exercises.Presentation.Tests/ExerciseHttpMappingsTests.cs`
- [presentation] `Modules/Exercises/tests/FoodDiary.Modules.Exercises.Presentation.Tests/ExercisesControllerTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/ExercisesModuleExtractionTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
