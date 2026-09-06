# Images Application Module Guidelines

## Scope

Rules for `Modules/Images/Application/`.

## Boundary

- Own image-asset commands, resolution, access, and cleanup policies.
- Expose cross-module capabilities through `FoodDiary.Application.Abstractions/Images`.
- Keep storage providers and persistence implementations outside this project.
- Do not reference the legacy `FoodDiary.Application` project.

## Commands

- Build: `dotnet build Modules/Images/Application/FoodDiary.Application.Images.csproj`
- Tests: `dotnet test Modules/Images/tests/FoodDiary.Modules.Images.Application.Tests/FoodDiary.Modules.Images.Application.Tests.csproj`

Cross-module image access returns ImageAssetReadModel from Service.Contracts, never ImageAsset. Batch cleanup delegates each candidate to IImageAssetCleanupBatch for an isolated save and recheck.
