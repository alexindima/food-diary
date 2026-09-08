---
id: generated.module.marketing
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# Marketing

## Graph

- Origin: extracted-project
- Extracted project: `Modules/Marketing/Application/FoodDiary.Application.Marketing.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Billing
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/Marketing/Application`
- `Modules/Marketing/Application/Abstractions`
- `Modules/Marketing/Contracts`
- `Modules/Marketing/Presentation`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: module-root
- Architecture guardrails: project-reference-matrix-and-module-boundary-tests
- Declared owned entities: MarketingAttributionEvent
- Public contract files: 17
- Observed external consumer groups: 3
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 17
- Interfaces: 4
- DTO/read-model/projection types: 5
- Enums: 0
- Exported repository-shaped contracts: 4
- Contracts referencing domain entities: 0
- `interface IMarketingAttributionEventReadRepository`
- `interface IMarketingAttributionEventRepository`
- `interface IMarketingAttributionEventWriteRepository`
- `interface IMarketingAttributionRangeReadRepository`
- `record GetMarketingAttributionRangeQuery`
- `record GetMarketingAttributionSummaryQuery`
- `record MarketingAttributionBreakdownModel`
- `record MarketingAttributionBreakdownRecord`
- `record MarketingAttributionDayModel`
- `record MarketingAttributionDayRecord`
- `record MarketingAttributionEventRecord`
- `record MarketingAttributionRangeFilter`
- `record MarketingAttributionRangeModel`
- `record MarketingAttributionRangeRecord`
- `record MarketingAttributionRecentEventModel`
- `record MarketingAttributionSummaryModel`
- `record MarketingAttributionSummaryRecord`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/Marketing/tests/FoodDiary.Modules.Marketing.Application.Tests/Marketing/MarketingAttributionCoverageTests.cs`
- [behavioral-or-text-match] `Modules/Marketing/tests/FoodDiary.Modules.Marketing.Application.Tests/Marketing/MarketingConversionRecorderTests.cs`
- [behavioral-or-text-match] `Modules/Marketing/tests/FoodDiary.Modules.Marketing.Application.Tests/Marketing/MarketingDependencyInjectionTests.cs`
- [behavioral-or-text-match] `Modules/Marketing/tests/FoodDiary.Modules.Marketing.Domain.Tests/Domain/MarketingAttributionEventInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Marketing/tests/FoodDiary.Modules.Marketing.Domain.Tests/Domain/MarketingIdConversionTests.cs`
- [integration] `Modules/Marketing/tests/FoodDiary.Modules.Marketing.Infrastructure.IntegrationTests/Integration/MarketingAttributionEventRepositoryIntegrationTests.cs`
- [integration] `Modules/Marketing/tests/FoodDiary.Modules.Marketing.Infrastructure.IntegrationTests/MarketingModuleRegistrationTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/MarketingModuleExtractionTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.JobManager.Tests/MarketingAttributionCleanupJobTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/MarketingAttributionTests.cs`
- [integration] `tests/FoodDiary.Web.Api.IntegrationTests/MarketingAttributionIntegrationTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
