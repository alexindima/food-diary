# OpenFoodFacts Logical Module Guidelines

## Scope

Rules for `Modules/OpenFoodFacts/`.

## Boundaries

- Own public OpenFoodFacts queries, durable cache orchestration, module contracts, provider and persistence ports, cache entity, repository implementation, and EF mapping.
- Own the Open Food Facts HTTP adapter, options and transport-specific resilience in Infrastructure/Providers behind IOpenFoodFactsService. Hosts call AddOpenFoodFactsProvider explicitly; common HTTP/URI/telemetry helpers remain once in Integrations.
- Expose cached product search to Products only through the Contracts project; Products must not reference the Application implementation.
- Preserve legacy `FoodDiary.Application.OpenFoodFacts.*`, `FoodDiary.Application.Abstractions.OpenFoodFacts.*`, and `FoodDiary.Domain.Entities.OpenFoodFacts.*` CLR namespaces.
- Preserve the legacy `FoodDiary.Application.OpenFoodFacts` application assembly name and EF model identity.
- Keep the shared `FoodDiaryDbContext`, historical migrations, and model snapshot in central Infrastructure.
- Register application and persistence behavior through Infrastructure's `AddOpenFoodFactsModule` composition facade.

## Verification

- Application tests: `dotnet test Modules/OpenFoodFacts/tests/FoodDiary.Modules.OpenFoodFacts.Application.Tests/FoodDiary.Modules.OpenFoodFacts.Application.Tests.csproj`
- Domain tests: `dotnet test Modules/OpenFoodFacts/tests/FoodDiary.Modules.OpenFoodFacts.Domain.Tests/FoodDiary.Modules.OpenFoodFacts.Domain.Tests.csproj`
- Infrastructure tests: `dotnet test Modules/OpenFoodFacts/tests/FoodDiary.Modules.OpenFoodFacts.Infrastructure.Tests/FoodDiary.Modules.OpenFoodFacts.Infrastructure.Tests.csproj`
- Provider adapter tests run in the module Infrastructure.Tests project above; retain its non-parallel shared-cache collection.
- Architecture: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
