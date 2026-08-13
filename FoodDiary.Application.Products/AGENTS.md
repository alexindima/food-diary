# Products Application Module Guidelines

## Scope

Rules for `FoodDiary.Application.Products/`.

## Boundary

- Own Product commands, queries, mappings, search suggestions, recent-product orchestration, and Product mutation capabilities.
- Expose cross-module Product reads through stable models and read-service contracts from `FoodDiary.Application.Abstractions`.
- Keep Product aggregate loading and mutation behind Products-owned repository or capability contracts.
- Other modules must not receive or mutate Product aggregates.
- Register through `AddProductsModule`; executable hosts remain composition roots.

## Verification

- Build: `dotnet build FoodDiary.Application.Products/FoodDiary.Application.Products.csproj`
- Focused tests: `dotnet test tests/FoodDiary.Application.Tests/FoodDiary.Application.Tests.csproj --filter FullyQualifiedName~Products`
- Architecture: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
