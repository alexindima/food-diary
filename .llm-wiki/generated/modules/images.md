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
- Extracted project: `FoodDiary.Application.Images/FoodDiary.Application.Images.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: none observed
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Application, FoodDiary.Application.Meals, FoodDiary.Application.Products, FoodDiary.Application.Recipes, FoodDiary.Initializer, FoodDiary.Integrations, FoodDiary.JobManager, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/Images`
- `FoodDiary.Application.Images`
- `FoodDiary.Domain/Entities/Assets`
- `FoodDiary.Infrastructure/Persistence/Configurations/Images`
- `FoodDiary.Infrastructure/Persistence/Images`
- `FoodDiary.Presentation.Api/Features/Images`

## HTTP Surface

### ImagesController

Source: `FoodDiary.Presentation.Api/Features/Images/ImagesController.cs`

- `POST /api/v{version:apiVersion}/images/upload-url`
- `DELETE /api/v{version:apiVersion}/images/{assetId:guid}`

## Boundary Health

- Role: aggregate-owner
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: ImageAsset, ImageObjectDeletionOutboxMessage
- Public contract files: 12
- Observed external consumer groups: 9
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 12
- Interfaces: 8
- DTO/read-model/projection types: 0
- Enums: 0
- Exported repository-shaped contracts: 3
- Contracts referencing domain entities: 3
- `class ImageErrors`
- `interface IImageAssetAccessService`
- `interface IImageAssetCleanupService`
- `interface IImageAssetReadRepository`
- `interface IImageAssetRepository`
- `interface IImageAssetWriteRepository`
- `interface IImageObjectDeletionOutbox`
- `interface IImageObjectDeletionOutboxProcessor`
- `interface IImageStorageService`
- `record DeleteImageAssetResult`
- `record ImageObjectValidationResult`
- `record PresignedUpload`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Images/ImagesFeatureTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/ImagesModuleExtractionTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
