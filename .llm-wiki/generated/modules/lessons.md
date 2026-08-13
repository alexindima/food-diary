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
- Extracted project: `FoodDiary.Application.Lessons/FoodDiary.Application.Lessons.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Achievements, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Application, FoodDiary.Application.Admin, FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/Lessons`
- `FoodDiary.Application.Lessons`
- `FoodDiary.Domain/Entities/Content`
- `FoodDiary.Infrastructure/Persistence/Configurations/Lessons`
- `FoodDiary.Presentation.Api/Features/Lessons`

## HTTP Surface

### LessonsController

Source: `FoodDiary.Presentation.Api/Features/Lessons/LessonsController.cs`

- `GET /api/v{version:apiVersion}/lessons`
- `GET /api/v{version:apiVersion}/lessons/{id:guid}`
- `POST /api/v{version:apiVersion}/lessons/{id:guid}/read`

## Boundary Health

- Role: aggregate-owner
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: not yet enumerated
- Public contract files: 10
- Observed external consumer groups: 6
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 10
- Interfaces: 4
- DTO/read-model/projection types: 4
- Enums: 1
- Exported repository-shaped contracts: 4
- Contracts referencing domain entities: 2
- `class LessonErrors`
- `enum LessonSortOption`
- `interface INutritionLessonReadModelRepository`
- `interface INutritionLessonReadRepository`
- `interface INutritionLessonRepository`
- `interface INutritionLessonWriteRepository`
- `record LessonAdminReadModel`
- `record LessonDetailReadModel`
- `record LessonSummaryPageReadModel`
- `record LessonSummaryReadModel`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Lessons/LessonsFeatureTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Lessons/LessonsValidatorTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/LessonsModuleExtractionTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
