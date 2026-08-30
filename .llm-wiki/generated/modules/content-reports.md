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

- Origin: extracted-project
- Extracted project: `Modules/ContentReports/Application/FoodDiary.Modules.ContentReports.Application.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Presentation.Api/Features/ContentReports`
- `Modules/ContentReports/Application`

## HTTP Surface

### ContentReportsController

Source: `FoodDiary.Presentation.Api/Features/ContentReports/ContentReportsController.cs`

- `POST /api/v{version:apiVersion}/reports`

## Boundary Health

- Role: aggregate-owner
- Physical isolation: module-root
- Architecture guardrails: project-reference-matrix
- Declared owned entities: ContentReport
- Public contract files: 0
- Observed external consumer groups: 3
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

- [behavioral-or-text-match] `Modules/ContentReports/tests/FoodDiary.Modules.ContentReports.Application.Tests/ContentReports/ContentReportsFeatureTests.cs`
- [behavioral-or-text-match] `Modules/ContentReports/tests/FoodDiary.Modules.ContentReports.Application.Tests/ContentReports/ContentReportsValidatorTests.cs`
- [behavioral-or-text-match] `Modules/ContentReports/tests/FoodDiary.Modules.ContentReports.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/ContentReports/tests/FoodDiary.Modules.ContentReports.Domain.Tests/ContentReportContractTests.cs`
- [behavioral-or-text-match] `Modules/ContentReports/tests/FoodDiary.Modules.ContentReports.Infrastructure.Tests/ContentReportsInfrastructureTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/ContentReportsModuleExtractionTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
