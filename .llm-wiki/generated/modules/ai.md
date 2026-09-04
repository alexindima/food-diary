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
- Extracted project: `Modules/Ai/Application/FoodDiary.Modules.Ai.Application.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Admin, Images, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.Integrations, FoodDiary.JobManager, FoodDiary.Modules.Admin.Application, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Integrations/Services/OpenAi`
- `FoodDiary.Presentation.Api/Features/Ai`
- `Modules/Ai/Application`
- `Modules/Ai/Application/Abstractions`
- `Modules/Ai/Domain`
- `Modules/Ai/Infrastructure`
- `Modules/Ai/Infrastructure/Model`

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

- Role: aggregate-owner
- Physical isolation: module-root
- Architecture guardrails: assembly-isolated
- Declared owned entities: AiUsage, AiPromptTemplate, AiQuotaPeriod, AiQuotaReservation
- Public contract files: 29
- Observed external consumer groups: 6
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 29
- Interfaces: 11
- DTO/read-model/projection types: 8
- Enums: 1
- Exported repository-shaped contracts: 8
- Contracts referencing domain entities: 3
- `class AiErrors`
- `enum AiQuotaReservationStatus`
- `interface IAiPromptProvider`
- `interface IAiPromptTemplateReadModelRepository`
- `interface IAiPromptTemplateReadRepository`
- `interface IAiPromptTemplateRepository`
- `interface IAiPromptTemplateWriteRepository`
- `interface IAiQuotaRepository`
- `interface IAiUsageReadRepository`
- `interface IAiUsageRepository`
- `interface IAiUsageWriteRepository`
- `interface IOpenAiFoodClient`
- `interface IOpenAiFoodService`
- `record AiPromptTemplateReadModel`
- `record AiProviderTokenBudget`
- `record AiQuotaReservationRequest`
- `record AiQuotaUsage`
- `record AiUsageBreakdown`
- `record AiUsageDailySummary`
- `record AiUsageSummary`
- `record AiUsageTotals`
- `record AiUsageUserSummary`
- `record FoodNutritionItemModel`
- `record FoodNutritionModel`
- `record FoodVisionItemModel`
- `record FoodVisionModel`
- `record OpenAiFoodClientResponse`
- `record UserAiUsageModel`
- `record struct AiUsageTokens`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/Ai/tests/FoodDiary.Modules.Ai.Application.Tests/Ai/AiValidatorsTests.cs`
- [behavioral-or-text-match] `Modules/Ai/tests/FoodDiary.Modules.Ai.Application.Tests/Ai/OpenAiFoodServiceTests.cs`
- [behavioral-or-text-match] `Modules/Ai/tests/FoodDiary.Modules.Ai.Application.Tests/Ai/ParseFoodTextValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Ai/tests/FoodDiary.Modules.Ai.Application.Tests/FeatureErrorContractTests.cs`
- [behavioral-or-text-match] `Modules/Ai/tests/FoodDiary.Modules.Ai.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/Ai/tests/FoodDiary.Modules.Ai.Domain.Tests/Domain/AiPromptTemplateInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Ai/tests/FoodDiary.Modules.Ai.Domain.Tests/Domain/AiUsageInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Ai/tests/FoodDiary.Modules.Ai.Infrastructure.Tests/Persistence/AiQuotaRepositoryTests.cs`
- [behavioral-or-text-match] `Modules/Ai/tests/FoodDiary.Modules.Ai.Infrastructure.Tests/Services/AiPromptProviderTests.cs`
- [behavioral-or-text-match] `Modules/Ai/tests/FoodDiary.Modules.Ai.Infrastructure.Tests/Services/OpenAiFoodServiceTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/Users/AiConsentTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/AiModuleExtractionTests.cs`
- [integration] `tests/FoodDiary.Infrastructure.IntegrationTests/Integration/AiQuotaRepositoryIntegrationTests.cs`
- [integration] `tests/FoodDiary.Infrastructure.IntegrationTests/Integration/AiUsageRepositoryIntegrationTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/AiFoodControllerTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/AiHttpMappingsTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
