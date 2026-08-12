---
id: generated.module.waist-entries
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# WaistEntries

## Graph

- Origin: module-graph
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Users
- Business-module consumers: Dashboard, Statistics, WeeklyCheckIn
- Host/adapter consumers: FoodDiary.Presentation.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/WaistEntries`
- `FoodDiary.Application/WaistEntries`
- `FoodDiary.Infrastructure/Persistence/Configurations/BodyMetrics`
- `FoodDiary.Presentation.Api/Features/WaistEntries`

## HTTP Surface

### WaistEntriesController

Source: `FoodDiary.Presentation.Api/Features/WaistEntries/WaistEntriesController.cs`

- `GET /api/v{version:apiVersion}/waist-entries`
- `GET /api/v{version:apiVersion}/waist-entries/latest`
- `GET /api/v{version:apiVersion}/waist-entries/summary`
- `GET /api/v{version:apiVersion}/waist-entries/page-summary`
- `POST /api/v{version:apiVersion}/waist-entries`
- `PUT /api/v{version:apiVersion}/waist-entries/{id:guid}`
- `DELETE /api/v{version:apiVersion}/waist-entries/{id:guid}`

## Boundary Health

- Role: aggregate-owner
- Physical isolation: folder
- Architecture guardrails: graph-only
- Declared owned entities: WaistEntry
- Public contract files: 6
- Observed external consumer groups: 4
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 6
- Interfaces: 4
- DTO/read-model/projection types: 1
- Enums: 0
- Exported repository-shaped contracts: 4
- Contracts referencing domain entities: 2
- `class WaistEntryErrors`
- `interface IWaistEntryReadModelRepository`
- `interface IWaistEntryReadRepository`
- `interface IWaistEntryRepository`
- `interface IWaistEntryWriteRepository`
- `record WaistEntryReadModel`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/WaistEntries/WaistEntriesFeatureTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/WaistEntries/WaistEntriesValidatorTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
