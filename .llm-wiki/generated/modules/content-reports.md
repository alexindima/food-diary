---
id: generated.module.content-reports
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# ContentReports

## Graph

- Origin: module-graph
- Business-module dependencies: Users
- Abstraction-contract dependencies: Users
- Business-module consumers: Admin
- Host/adapter consumers: FoodDiary.Presentation.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/ContentReports`
- `FoodDiary.Application/ContentReports`
- `FoodDiary.Infrastructure/Persistence/Configurations/ContentReports`
- `FoodDiary.Infrastructure/Persistence/ContentReports`
- `FoodDiary.Presentation.Api/Features/ContentReports`

## HTTP Surface

### ContentReportsController

Source: `FoodDiary.Presentation.Api/Features/ContentReports/ContentReportsController.cs`

- `POST /api/v{version:apiVersion}/reports`

## Boundary Health

- Role: aggregate-owner
- Physical isolation: folder
- Architecture guardrails: graph-only
- Declared owned entities: not yet enumerated
- Public contract files: 6
- Observed external consumer groups: 2
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 6
- Interfaces: 4
- DTO/read-model/projection types: 1
- Enums: 0
- Exported repository-shaped contracts: 4
- Contracts referencing domain entities: 2
- `class ContentReportErrors`
- `interface IContentReportReadModelRepository`
- `interface IContentReportReadRepository`
- `interface IContentReportRepository`
- `interface IContentReportWriteRepository`
- `record ContentReportAdminReadModel`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/ContentReports/ContentReportsFeatureTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/ContentReports/ContentReportsValidatorTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
