# Products Application Module Guidelines

## Scope

Rules for `Modules/Products/Application/`.

## Boundary

- Own Product commands, queries, mappings, search suggestions, recent-product orchestration, and Product mutation capabilities.
- Expose cross-module Product reads through stable models and read-service contracts from `Modules/Products/Contracts`.
- Keep Product aggregate loading and mutation behind Products-owned repository or capability contracts.
- Other modules must not receive or mutate Product aggregates.
- Register application services through `AddProductsApplication`; module Infrastructure exposes `AddProductsModule`; executable hosts remain composition roots.

## Verification

- Build: `dotnet build Modules/Products/Application/FoodDiary.Modules.Products.Application.csproj`
- Focused tests: `dotnet test Modules/Products/tests/FoodDiary.Modules.Products.Application.Tests/FoodDiary.Modules.Products.Application.Tests.csproj`
- Architecture: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
