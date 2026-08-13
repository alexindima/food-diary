---
id: generated.module.identity
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# Identity

## Graph

- Origin: extracted-project
- Extracted project: `FoodDiary.Application.Identity/FoodDiary.Application.Identity.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Admin, Authentication, Email, Notifications, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Identity`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: project
- Architecture guardrails: assembly-isolated
- Declared owned entities: not yet enumerated
- Public contract files: 0
- Observed external consumer groups: 4
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 0
- Interfaces: 0
- DTO/read-model/projection types: 0
- Enums: 0
- Exported repository-shaped contracts: 0
- Contracts referencing domain entities: 0
- No public declaration was found in the mapped abstraction areas.

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/IdentityModuleExtractionTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
