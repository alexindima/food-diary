---
id: generated.module.ai
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# Ai

## Graph

- Origin: extracted-project
- Extracted project: `FoodDiary.Application.Ai/FoodDiary.Application.Ai.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Admin, Images, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Application.Admin, FoodDiary.Initializer, FoodDiary.Integrations, FoodDiary.JobManager, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/Ai`
- `FoodDiary.Application.Ai`
- `FoodDiary.Domain/Entities/Ai`
- `FoodDiary.Infrastructure/Persistence/Ai`
- `FoodDiary.Infrastructure/Persistence/Configurations/Ai`
- `FoodDiary.Presentation.Api/Features/Ai`

## HTTP Surface

### AiFoodController

Source: `FoodDiary.Presentation.Api/Features/Ai/AiFoodController.cs`

- `POST /api/v{version:apiVersion}/ai/food/vision`
- `POST /api/v{version:apiVersion}/ai/food/text`
- `POST /api/v{version:apiVersion}/ai/food/nutrition`

### AiUsageController

Source: `FoodDiary.Presentation.Api/Features/Ai/AiUsageController.cs`

- `GET /api/v{version:apiVersion}/ai/usage/me`

## Boundary Health

- Role: orchestrator
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: not yet enumerated
- Public contract files: 20
- Observed external consumer groups: 6
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 20
- Interfaces: 10
- DTO/read-model/projection types: 7
- Enums: 0
- Exported repository-shaped contracts: 7
- Contracts referencing domain entities: 3
- `class AiErrors`
- `interface IAiPromptProvider`
- `interface IAiPromptTemplateReadModelRepository`
- `interface IAiPromptTemplateReadRepository`
- `interface IAiPromptTemplateRepository`
- `interface IAiPromptTemplateWriteRepository`
- `interface IAiUsageReadRepository`
- `interface IAiUsageRepository`
- `interface IAiUsageWriteRepository`
- `interface IOpenAiFoodClient`
- `interface IOpenAiFoodService`
- `record AiPromptTemplateReadModel`
- `record AiUsageTotals`
- `record FoodNutritionItemModel`
- `record FoodNutritionModel`
- `record FoodVisionItemModel`
- `record FoodVisionModel`
- `record OpenAiFoodClientResponse`
- `record UserAiUsageModel`
- `record struct AiUsageTokens`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Ai/AiValidatorsTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Ai/OpenAiFoodServiceTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Ai/ParseFoodTextValidatorTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Users/AiConsentTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/AiModuleExtractionTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Domain.Tests/Domain/AiPromptTemplateInvariantTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Domain.Tests/Domain/AiUsageInvariantTests.cs`
- [integration] `tests/FoodDiary.Infrastructure.IntegrationTests/Integration/AiUsageRepositoryIntegrationTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Infrastructure.Tests/Services/AiPromptProviderTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/AiFoodControllerTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/AiHttpMappingsTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
