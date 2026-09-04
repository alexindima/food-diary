---
id: generated.module.body-metrics
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# BodyMetrics

## Graph

- Origin: extracted-project
- Extracted project: `Modules/BodyMetrics/Application/FoodDiary.Application.BodyMetrics.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Users, WaistEntries, WeightEntries
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/BodyMetrics/Application`
- `Modules/BodyMetrics/Application/Abstractions`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: module-root
- Architecture guardrails: project-reference-matrix-and-module-boundary-tests
- Declared owned entities: WaistEntry, WeightEntry
- Public contract files: 18
- Observed external consumer groups: 4
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 18
- Interfaces: 10
- DTO/read-model/projection types: 6
- Enums: 0
- Exported repository-shaped contracts: 8
- Contracts referencing domain entities: 4
- `class WaistEntryErrors`
- `class WeightEntryErrors`
- `interface IWaistEntryReadModelRepository`
- `interface IWaistEntryReadRepository`
- `interface IWaistEntryReadService`
- `interface IWaistEntryRepository`
- `interface IWaistEntryWriteRepository`
- `interface IWeightEntryReadModelRepository`
- `interface IWeightEntryReadRepository`
- `interface IWeightEntryReadService`
- `interface IWeightEntryRepository`
- `interface IWeightEntryWriteRepository`
- `record WaistEntryModel`
- `record WaistEntryReadModel`
- `record WaistEntrySummaryModel`
- `record WeightEntryModel`
- `record WeightEntryReadModel`
- `record WeightEntrySummaryModel`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/BodyMetrics/tests/FoodDiary.Modules.BodyMetrics.Application.Tests/MeasurementErrorContractTests.cs`
- [behavioral-or-text-match] `Modules/BodyMetrics/tests/FoodDiary.Modules.BodyMetrics.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/BodyMetrics/tests/FoodDiary.Modules.BodyMetrics.Application.Tests/WaistEntries/WaistEntriesFeatureTests.cs`
- [behavioral-or-text-match] `Modules/BodyMetrics/tests/FoodDiary.Modules.BodyMetrics.Application.Tests/WaistEntries/WaistEntriesValidatorTests.cs`
- [behavioral-or-text-match] `Modules/BodyMetrics/tests/FoodDiary.Modules.BodyMetrics.Application.Tests/WeightEntries/WeightEntriesFeatureTests.cs`
- [behavioral-or-text-match] `Modules/BodyMetrics/tests/FoodDiary.Modules.BodyMetrics.Application.Tests/WeightEntries/WeightEntriesValidatorTests.cs`
- [behavioral-or-text-match] `Modules/BodyMetrics/tests/FoodDiary.Modules.BodyMetrics.Domain.Tests/Domain/WaistEntryInvariantTests.cs`
- [behavioral-or-text-match] `Modules/BodyMetrics/tests/FoodDiary.Modules.BodyMetrics.Domain.Tests/Domain/WeightEntryInvariantTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/BodyMetricsModuleExtractionTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
