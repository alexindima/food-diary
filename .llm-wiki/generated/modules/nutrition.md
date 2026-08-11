---
id: generated.module.nutrition
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# Nutrition

## Graph

- Origin: module-graph
- Business-module dependencies: none observed
- Abstraction-contract dependencies: none observed
- Business-module consumers: Consumptions, Recipes
- Host/adapter consumers: none observed
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application/Nutrition`
- `FoodDiary.Infrastructure/Persistence/Configurations/Nutrition`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: domain-service
- Physical isolation: folder
- Architecture guardrails: graph-only
- Declared owned entities: not yet enumerated
- Public contract files: 0
- Observed external consumer groups: 2
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 0
- Exported repository-shaped contracts: 0
- No public declaration was found in the mapped abstraction areas.

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

No test file with an exact module path/name match was found.

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
