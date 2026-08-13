---
id: generated.module.daily-advices
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# DailyAdvices

## Graph

- Origin: extracted-project
- Extracted project: `FoodDiary.Application.DailyAdvices/FoodDiary.Application.DailyAdvices.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Users
- Business-module consumers: Dashboard
- Host/adapter consumers: FoodDiary.Application, FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/DailyAdvices`
- `FoodDiary.Application.DailyAdvices`
- `FoodDiary.Infrastructure/Persistence/Configurations/DailyAdvices`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: read-composer
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: not yet enumerated
- Public contract files: 3
- Observed external consumer groups: 6
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 3
- Interfaces: 1
- DTO/read-model/projection types: 1
- Enums: 0
- Exported repository-shaped contracts: 1
- Contracts referencing domain entities: 0
- `class DailyAdviceErrors`
- `interface IDailyAdviceReadModelRepository`
- `record DailyAdviceReadModel`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/DailyAdvices/DailyAdvicesFeatureTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/DailyAdvicesModuleExtractionTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
