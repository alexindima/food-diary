# USDA Application Module Guidelines

## Scope

Rules for `FoodDiary.Application.Usda/`.

## Boundary

- Own USDA queries, product-link commands, mappings, and USDA read services.
- Depend on Meal activity only through `IMealActivityReadService`; never load Meal aggregates.
- Keep provider access and persistence behind contracts from `FoodDiary.Application.Abstractions`.
- Expose product suggestions through `IUsdaProductSuggestionReadService` rather than USDA implementation types.
- Register through `AddUsdaModule`; executable hosts remain composition roots.

## Verification

- Build: `dotnet build FoodDiary.Application.Usda/FoodDiary.Application.Usda.csproj`
- Focused tests: `dotnet test tests/FoodDiary.Application.Tests/FoodDiary.Application.Tests.csproj --filter FullyQualifiedName~Usda`
- Architecture: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
