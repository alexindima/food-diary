---
id: generated.module.ai
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# Ai

## Graph

- Origin: module-graph
- Dependencies: Users
- Consumers: Admin

## Source Areas

- `FoodDiary.Application.Abstractions/Ai`
- `FoodDiary.Application/Ai`
- `FoodDiary.Domain/Entities/Ai`
- `FoodDiary.Infrastructure/Persistence/Ai`
- `FoodDiary.Infrastructure/Persistence/Configurations/Ai`
- `FoodDiary.Presentation.Api/Features/Ai`
- `FoodDiary.Web.Client/src/app/features/meals/lib/ai`
- `tests/FoodDiary.Application.Tests/Ai`

## HTTP Surface

### AiFoodController

Source: `FoodDiary.Presentation.Api/Features/Ai/AiFoodController.cs`

- `POST /api/v{version:apiVersion}/ai/food/vision`
- `POST /api/v{version:apiVersion}/ai/food/text`
- `POST /api/v{version:apiVersion}/ai/food/nutrition`

### AiUsageController

Source: `FoodDiary.Presentation.Api/Features/Ai/AiUsageController.cs`

- `GET /api/v{version:apiVersion}/ai/usage/me`

## Focused Tests

- `tests/FoodDiary.Application.Tests/Ai/AiValidatorsTests.cs`
- `tests/FoodDiary.Application.Tests/Ai/OpenAiFoodServiceTests.cs`
- `tests/FoodDiary.Application.Tests/Ai/ParseFoodTextValidatorTests.cs`
- `tests/FoodDiary.Application.Tests/Users/AiConsentTests.cs`
- `tests/FoodDiary.Domain.Tests/Domain/AiPromptTemplateInvariantTests.cs`
- `tests/FoodDiary.Domain.Tests/Domain/AiUsageInvariantTests.cs`
- `tests/FoodDiary.Infrastructure.IntegrationTests/Integration/AiUsageRepositoryIntegrationTests.cs`
- `tests/FoodDiary.Infrastructure.Tests/Services/AiPromptProviderTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/AiFoodControllerTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/AiHttpMappingsTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
