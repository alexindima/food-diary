---
id: generated.module.daily-advices
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# DailyAdvices

## Graph

- Origin: module-graph
- Dependencies: Users
- Consumers: Dashboard

## Source Areas

- `FoodDiary.Application.Abstractions/DailyAdvices`
- `FoodDiary.Application/DailyAdvices`
- `FoodDiary.Infrastructure/Persistence/Configurations/DailyAdvices`
- `tests/FoodDiary.Application.Tests/DailyAdvices`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Focused Tests

- `tests/FoodDiary.Application.Tests/DailyAdvices/DailyAdvicesFeatureTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
