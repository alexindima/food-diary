---
id: generated.module.images
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# Images

## Graph

- Origin: module-graph
- Dependencies: none
- Consumers: Consumptions, Products, Recipes, Users

## Source Areas

- `FoodDiary.Application.Abstractions/Images`
- `FoodDiary.Application/Images`
- `FoodDiary.Infrastructure/Persistence/Configurations/Images`
- `FoodDiary.Infrastructure/Persistence/Images`
- `FoodDiary.Presentation.Api/Features/Images`
- `FoodDiary.Web.Client/assets/images`
- `tests/FoodDiary.Application.Tests/Images`

## HTTP Surface

### ImagesController

Source: `FoodDiary.Presentation.Api/Features/Images/ImagesController.cs`

- `POST /api/v{version:apiVersion}/images/upload-url`
- `DELETE /api/v{version:apiVersion}/images/{assetId:guid}`

## Focused Tests

- `tests/FoodDiary.Application.Tests/Images/ImagesFeatureTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
