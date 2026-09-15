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

- Origin: extracted-project
- Extracted project: `Modules/RecentItems/Application/FoodDiary.Modules.RecentItems.Application.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: none observed
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/RecentItems/Application`
- `Modules/RecentItems/Application.Abstractions`
- `Modules/RecentItems/Contracts`
- `Modules/RecentItems/PersistenceModel`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: RecentItem
- Public contract files: 8
- Observed external consumer groups: 3
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 8
- Interfaces: 4
- DTO/read-model/projection types: 0
- Enums: 0
- Exported repository-shaped contracts: 3
- Contracts referencing domain entities: 0
- `interface IRecentItemReadRepository`
- `interface IRecentItemRepository`
- `interface IRecentItemUsageRecorder`
- `interface IRecentItemWriteRepository`
- `record ReadRecentProductsQuery`
- `record ReadRecentRecipesQuery`
- `record RecentProductUsage`
- `record RecentRecipeUsage`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/RecentItems/tests/FoodDiary.Modules.RecentItems.Domain.Tests/RecentItemInvariantTests.cs`
- [behavioral-or-text-match] `Modules/RecentItems/tests/FoodDiary.Modules.RecentItems.Domain.Tests/RecentItemsIdConversionTests.cs`
- [integration] `Modules/RecentItems/tests/FoodDiary.Modules.RecentItems.Infrastructure.IntegrationTests/Integration/RecentItemRepositoryIntegrationTests.cs`
- [behavioral-or-text-match] `Modules/RecentItems/tests/FoodDiary.Modules.RecentItems.Infrastructure.Tests/Persistence/PostCommitRecentItemUsageRecorderTests.cs`
- [behavioral-or-text-match] `Modules/RecentItems/tests/FoodDiary.Modules.RecentItems.Infrastructure.Tests/Persistence/RecentItemRepositoryTests.cs`
- [architecture-boundary] `Tooling/tests/FoodDiary.ArchitectureTests/RecentItemsModuleExtractionTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
