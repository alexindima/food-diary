---
id: generated.module.marketing
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# Marketing

## Graph

- Origin: extracted-project
- Extracted project: `FoodDiary.Application.Marketing/FoodDiary.Application.Marketing.csproj`
- Dependencies: none
- Consumers: none

## Source Areas

- `FoodDiary.Application.Abstractions/Marketing`
- `FoodDiary.Application/Marketing`
- `FoodDiary.Infrastructure/Persistence/Configurations/Marketing`
- `FoodDiary.Presentation.Api/Features/Marketing`
- `FoodDiary.Web.Client/src/app/shared/marketing`
- `tests/FoodDiary.Application.Tests/Marketing`

## HTTP Surface

### MarketingAttributionController

Source: `FoodDiary.Presentation.Api/Features/Marketing/MarketingAttributionController.cs`

- `POST /api/v{version:apiVersion}/marketing/attribution-events`

## Focused Tests

- `tests/FoodDiary.Application.Tests/Marketing/MarketingConversionRecorderTests.cs`
- `tests/FoodDiary.Application.Tests/Marketing/MarketingDependencyInjectionTests.cs`
- `tests/FoodDiary.ArchitectureTests/MarketingModuleExtractionTests.cs`
- `tests/FoodDiary.Domain.Tests/Domain/MarketingAttributionEventInvariantTests.cs`
- `tests/FoodDiary.Infrastructure.IntegrationTests/Integration/MarketingAttributionEventRepositoryIntegrationTests.cs`
- `tests/FoodDiary.JobManager.Tests/MarketingAttributionCleanupJobTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/MarketingAttributionTests.cs`
- `tests/FoodDiary.Web.Api.IntegrationTests/MarketingAttributionIntegrationTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
