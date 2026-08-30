# USDA Logical Module Guidelines

## Boundary

- Own USDA application flows, provider/persistence ports, cross-module models, EF mappings, repository adapter, and focused application tests.
- Keep USDA HTTP requests, provider DTO mapping, timeout, cancellation, in-memory detail cache, and API-key options in `FoodDiary.Integrations`.
- Keep USDA entities in central Domain because `Product` owns the EF navigation seam; preserve their CLR and EF identity.
- Keep `FoodDiaryDbContext`, migrations, and snapshot central. Register mappings explicitly with `ApplyUsdaPersistenceModel`.
- Preserve the `FoodDiary.Application.Usda` assembly and all legacy CLR namespaces.

## Verification

- `dotnet test Modules/Usda/tests/FoodDiary.Modules.Usda.Application.Tests/FoodDiary.Modules.Usda.Application.Tests.csproj`
- `dotnet test tests/FoodDiary.Infrastructure.Tests/FoodDiary.Infrastructure.Tests.csproj --filter FullyQualifiedName~UsdaFoodSearchServiceTests`
- `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
