---
id: generated.module.gamification
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# Gamification

## Graph

- Origin: module-graph
- Dependencies: Consumptions, Users
- Consumers: none

## Source Areas

- `FoodDiary.Application/Gamification`
- `FoodDiary.Presentation.Api/Features/Gamification`
- `FoodDiary.Web.Client/src/app/features/gamification`
- `tests/FoodDiary.Application.Tests/Gamification`

## HTTP Surface

### GamificationController

Source: `FoodDiary.Presentation.Api/Features/Gamification/GamificationController.cs`

- `GET /api/v{version:apiVersion}/gamification`

## Focused Tests

- `tests/FoodDiary.Application.Tests/Gamification/GamificationCalculatorTests.cs`
- `tests/FoodDiary.Application.Tests/Gamification/GamificationFeatureTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/GamificationHttpMappingsTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
