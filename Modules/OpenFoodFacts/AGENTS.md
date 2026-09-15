# OpenFoodFacts Logical Module Guidelines

## Scope

Rules for `Modules/OpenFoodFacts/`.

## Boundaries

- Own public OpenFoodFacts queries, durable cache orchestration, module contracts, provider and persistence ports, cache entity, repository implementation, and EF mapping.
- Own the Open Food Facts HTTP adapter, options and transport-specific resilience in Infrastructure/Providers behind IOpenFoodFactsService. Hosts call AddOpenFoodFactsProvider explicitly; common HTTP/URI/telemetry helpers remain once in Integrations.
- Expose cached product search to Products only through the Contracts project; Products must not reference the Application implementation.
- Use `FoodDiary.Modules.OpenFoodFacts.<Project>[.<Folder>]` namespaces.
- Use the canonical application assembly identity. Preserve historical migration metadata and the database schema.
- Keep the shared `FoodDiaryDbContext`, historical migrations, and model snapshot in central Infrastructure.
- Register application and persistence behavior through Infrastructure's `AddOpenFoodFactsModule` composition facade.

## Verification

- Application tests: `dotnet test Modules/OpenFoodFacts/tests/FoodDiary.Modules.OpenFoodFacts.Application.Tests/FoodDiary.Modules.OpenFoodFacts.Application.Tests.csproj`
- Domain tests: `dotnet test Modules/OpenFoodFacts/tests/FoodDiary.Modules.OpenFoodFacts.Domain.Tests/FoodDiary.Modules.OpenFoodFacts.Domain.Tests.csproj`
- Infrastructure tests: `dotnet test Modules/OpenFoodFacts/tests/FoodDiary.Modules.OpenFoodFacts.Infrastructure.Tests/FoodDiary.Modules.OpenFoodFacts.Infrastructure.Tests.csproj`
- Provider adapter tests run in the module Infrastructure.Tests project above; retain its non-parallel shared-cache collection.
- Architecture: `dotnet test Tooling/tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`

OpenFoodFacts owns its cache runtime context. Immediate SQL upsert and cache reads
synchronize with the live shared transaction supplied by registration, including
transactions opened after repository resolution. Central migrations remain;
provider calls, ranking and cache counter semantics are unchanged (ADR 0040).

SearchOpenFoodFactsQuery belongs to Contracts and is used by HTTP and Products through ISender. The owner handler combines provider/cache reads and preserves the explicit cache SaveChanges. Provider and cache interfaces remain outbound ports in Application.Abstractions.
