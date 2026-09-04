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
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.Integrations, FoodDiary.JobManager, FoodDiary.Modules.Meals.Application, FoodDiary.Modules.Products.Application, FoodDiary.Modules.Recipes.Application, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Infrastructure/Persistence/Configurations/Images`
- `FoodDiary.Presentation.Api/Features/Images`
- `Modules/Images/Application`
- `Modules/Images/Application/Abstractions`
- `Modules/Images/Contracts`
- `Modules/Images/Domain`
- `Modules/Images/Infrastructure`
- `Modules/Images/Infrastructure/Model`
- `Modules/Images/Infrastructure/Providers`

## HTTP Surface

### ImagesController

Source: `FoodDiary.Presentation.Api/Features/Images/ImagesController.cs`

- `POST /api/v{version:apiVersion}/images/upload-url`
- `POST /api/v{version:apiVersion}/images/{assetId:guid}/confirm`
- `DELETE /api/v{version:apiVersion}/images/{assetId:guid}`

## Boundary Health

- Role: aggregate-owner
- Physical isolation: module-root
- Architecture guardrails: project-reference-matrix
- Declared owned entities: ImageAsset, ImageObjectDeletionOutboxMessage
- Public contract files: 13
- Observed external consumer groups: 8
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 13
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
- `record struct ImageAssetId`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/Images/tests/FoodDiary.Modules.Images.Application.Tests/FeatureErrorContractTests.cs`
- [behavioral-or-text-match] `Modules/Images/tests/FoodDiary.Modules.Images.Application.Tests/Images/ImagesFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Images/tests/FoodDiary.Modules.Images.Domain.Tests/Domain/ImageAssetInvariantTests.cs`
- [integration] `Modules/Images/tests/FoodDiary.Modules.Images.Infrastructure.IntegrationTests/Integration/ImageAssetRepositoryIntegrationTests.cs`
- [integration] `Modules/Images/tests/FoodDiary.Modules.Images.Infrastructure.IntegrationTests/Integration/ImageObjectDeletionOutboxMessageTests.cs`
- [integration] `Modules/Images/tests/FoodDiary.Modules.Images.Infrastructure.IntegrationTests/Integration/ImageOutboxModelIntegrationTests.cs`
- [integration] `Modules/Images/tests/FoodDiary.Modules.Images.Infrastructure.IntegrationTests/Integration/OutboxReplayStreamTests.cs`
- [behavioral-or-text-match] `Modules/Images/tests/FoodDiary.Modules.Images.Infrastructure.Tests/Integrations/ProviderOptionsTests.cs`
- [behavioral-or-text-match] `Modules/Images/tests/FoodDiary.Modules.Images.Infrastructure.Tests/Integrations/ProviderRegistrationTests.cs`
- [behavioral-or-text-match] `Modules/Images/tests/FoodDiary.Modules.Images.Infrastructure.Tests/Services/S3ImageStorageServiceTests.cs`
- [behavioral-or-text-match] `Modules/Images/tests/FoodDiary.Modules.Images.Infrastructure.Tests/Services/S3ObjectStorageClientTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/ImagesModuleExtractionTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
