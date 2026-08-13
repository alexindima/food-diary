---
id: generated.module.recent-items
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# RecentItems

## Graph

- Origin: module-graph
- Business-module dependencies: none observed
- Abstraction-contract dependencies: none observed
- Business-module consumers: Recipes
- Host/adapter consumers: none observed
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/RecentItems`
- `FoodDiary.Application/RecentItems`
- `FoodDiary.Infrastructure/Persistence/Configurations/RecentItems`
- `FoodDiary.Infrastructure/Persistence/RecentItems`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: folder
- Architecture guardrails: graph-only
- Declared owned entities: RecentItem
- Public contract files: 7
- Observed external consumer groups: 1
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 7
- Interfaces: 5
- DTO/read-model/projection types: 0
- Enums: 0
- Exported repository-shaped contracts: 3
- Contracts referencing domain entities: 0
- `interface IRecentItemReadRepository`
- `interface IRecentItemRepository`
- `interface IRecentItemUsageReadService`
- `interface IRecentItemUsageRecorder`
- `interface IRecentItemWriteRepository`
- `record RecentProductUsage`
- `record RecentRecipeUsage`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

No test file with an exact module path/name match was found.

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
