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

- Origin: module-graph
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Users
- Business-module consumers: Dashboard, Tdee
- Host/adapter consumers: FoodDiary.Presentation.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/Exercises`
- `FoodDiary.Application/Exercises`
- `FoodDiary.Infrastructure/Persistence/Configurations/Exercises`
- `FoodDiary.Presentation.Api/Features/Exercises`

## HTTP Surface

### ExercisesController

Source: `FoodDiary.Presentation.Api/Features/Exercises/ExercisesController.cs`

- `GET /api/v{version:apiVersion}/exercises`
- `POST /api/v{version:apiVersion}/exercises`
- `PUT /api/v{version:apiVersion}/exercises/{id:guid}`
- `DELETE /api/v{version:apiVersion}/exercises/{id:guid}`

## Boundary Health

- Role: aggregate-owner
- Physical isolation: folder
- Architecture guardrails: graph-only
- Declared owned entities: ExerciseEntry
- Public contract files: 6
- Observed external consumer groups: 3
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 6
- Interfaces: 4
- DTO/read-model/projection types: 1
- Enums: 0
- Exported repository-shaped contracts: 4
- Contracts referencing domain entities: 2
- `class ExerciseErrors`
- `interface IExerciseEntryReadModelRepository`
- `interface IExerciseEntryReadRepository`
- `interface IExerciseEntryRepository`
- `interface IExerciseEntryWriteRepository`
- `record ExerciseEntryReadModel`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Exercises/ExercisesFeatureTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Exercises/ExercisesValidatorTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/ExercisesControllerTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
