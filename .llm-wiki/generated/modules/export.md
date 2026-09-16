---
id: generated.module.export
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# Export

## Graph

- Origin: extracted-project
- Extracted project: `Modules/Export/Application/FoodDiary.Modules.Export.Application.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Authentication, Cycles, Meals, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/Export/Application`
- `Modules/Export/Application.Abstractions`
- `Modules/Export/Infrastructure`
- `Modules/Export/Presentation`

## HTTP Surface

### MailInboxExportController

Source: `Services/MailInbox/FoodDiary.MailInbox.Presentation/Features/Export/MailInboxExportController.cs`

- `GET /api/mail-inbox/export`
- `GET /api/mail-inbox/export/{id:guid}/mime`

## Boundary Health

- Role: read-composer
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: not yet enumerated
- Public contract files: 5
- Observed external consumer groups: 4
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 5
- Interfaces: 2
- DTO/read-model/projection types: 1
- Enums: 0
- Exported repository-shaped contracts: 0
- Contracts referencing domain entities: 0
- `class ExportInputLimits`
- `interface IDiaryPdfGenerator`
- `interface IDiaryPdfReportTextProvider`
- `record DiaryPdfReportTexts`
- `record ExportDiaryMealsReadModel`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/Export/tests/FoodDiary.Modules.Export.Application.Tests/Authentication/SecretInputLimitValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Export/tests/FoodDiary.Modules.Export.Application.Tests/CsvFieldEscaperTests.cs`
- [behavioral-or-text-match] `Modules/Export/tests/FoodDiary.Modules.Export.Application.Tests/CycleExportFailureTests.cs`
- [behavioral-or-text-match] `Modules/Export/tests/FoodDiary.Modules.Export.Application.Tests/ExportFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Export/tests/FoodDiary.Modules.Export.Application.Tests/ExportValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Export/tests/FoodDiary.Modules.Export.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/Export/tests/FoodDiary.Modules.Export.Application.Tests/Time/UtcDateNormalizerTests.cs`
- [behavioral-or-text-match] `Modules/Export/tests/FoodDiary.Modules.Export.Infrastructure.Tests/ModuleRegistrationTests.cs`
- [behavioral-or-text-match] `Modules/Export/tests/FoodDiary.Modules.Export.Infrastructure.Tests/Resources/DiaryPdfReportResourceTextProviderTests.cs`
- [behavioral-or-text-match] `Modules/Export/tests/FoodDiary.Modules.Export.Infrastructure.Tests/Resources/ResourceContractTests.cs`
- [behavioral-or-text-match] `Modules/Export/tests/FoodDiary.Modules.Export.Infrastructure.Tests/Services/DiaryPdfGeneratorTests.cs`
- [presentation] `Modules/Export/tests/FoodDiary.Modules.Export.Presentation.Tests/ExportControllerTests.cs`
- [architecture-boundary] `Tooling/tests/FoodDiary.ArchitectureTests/ExportModuleExtractionTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
