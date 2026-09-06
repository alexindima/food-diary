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
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Modules.Meals.Application, FoodDiary.Modules.Products.Application, FoodDiary.Modules.Recipes.Application, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/Images/Application`
- `Modules/Images/Application/Abstractions`
- `Modules/Images/Contracts`
- `Modules/Images/Domain`
- `Modules/Images/Infrastructure`
- `Modules/Images/Infrastructure/Model`
- `Modules/Images/Infrastructure/Providers`
- `Modules/Images/Presentation`
- `Modules/Images/Service.Contracts`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: module-root
- Architecture guardrails: project-reference-matrix
- Declared owned entities: ImageAsset, ImageObjectDeletionOutboxMessage
- Public contract files: 16
- Observed external consumer groups: 6
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 16
- Interfaces: 10
- DTO/read-model/projection types: 1
- Enums: 0
- Exported repository-shaped contracts: 3
- Contracts referencing domain entities: 2
- `class ImageErrors`
- `interface IImageAssetAccessService`
- `interface IImageAssetCleanupBatch`
- `interface IImageAssetCleanupService`
- `interface IImageAssetOwnershipService`
- `interface IImageAssetReadRepository`
- `interface IImageAssetRepository`
- `interface IImageAssetWriteRepository`
- `interface IImageObjectDeletionOutbox`
- `interface IImageObjectDeletionOutboxProcessor`
- `interface IImageStorageService`
- `record DeleteImageAssetResult`
- `record ImageAssetReadModel`
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
- [presentation] `Modules/Images/tests/FoodDiary.Modules.Images.Presentation.Tests/ImageHttpMappingsTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/ImagesModuleExtractionTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
