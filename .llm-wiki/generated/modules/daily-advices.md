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

- Origin: module-graph
- Business-module dependencies: Users
- Abstraction-contract dependencies: Users
- Business-module consumers: Dashboard
- Host/adapter consumers: FoodDiary.Presentation.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/DailyAdvices`
- `FoodDiary.Application/DailyAdvices`
- `FoodDiary.Infrastructure/Persistence/Configurations/DailyAdvices`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: folder
- Architecture guardrails: graph-only
- Declared owned entities: not yet enumerated
- Public contract files: 3
- Observed external consumer groups: 2
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 3
- Exported repository-shaped contracts: 3
- `interface IDailyAdviceReadModelRepository`
- `interface IDailyAdviceReadRepository`
- `interface IDailyAdviceRepository`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/DailyAdvices/DailyAdvicesFeatureTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
