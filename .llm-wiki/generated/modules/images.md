---
id: generated.module.images
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# Images

## Graph

- Origin: extracted-project
- Extracted project: `Modules/Images/Application/FoodDiary.Application.Images.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: none observed
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Application.Meals, FoodDiary.Application.Products, FoodDiary.Application.Recipes, FoodDiary.Initializer, FoodDiary.Integrations, FoodDiary.JobManager, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Images`
- `FoodDiary.Domain/Entities/Assets`
- `FoodDiary.Presentation.Api/Features/Images`

## HTTP Surface

### ImagesController

Source: `FoodDiary.Presentation.Api/Features/Images/ImagesController.cs`

- `POST /api/v{version:apiVersion}/images/upload-url`
- `POST /api/v{version:apiVersion}/images/{assetId:guid}/confirm`
- `DELETE /api/v{version:apiVersion}/images/{assetId:guid}`

## Boundary Health

- Role: aggregate-owner
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: ImageAsset, ImageObjectDeletionOutboxMessage
- Public contract files: 0
- Observed external consumer groups: 8
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

- [behavioral-or-text-match] `Modules/Images/tests/FoodDiary.Modules.Images.Application.Tests/Images/ImagesFeatureTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/ImagesModuleExtractionTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
