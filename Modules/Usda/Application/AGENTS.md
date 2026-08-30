# USDA Application Module Guidelines

## Scope

Rules for `Modules/Usda/Application/`.

## Boundary

- Own USDA queries, product-link commands, mappings, and USDA read services.
- Depend on Meal activity only through `IMealActivityReadService`; never load Meal aggregates.
- Keep provider access and persistence behind contracts from `FoodDiary.Application.Abstractions`.
- Expose product suggestions through `IUsdaProductSuggestionReadService` rather than USDA implementation types.
- Register application behavior through `AddUsdaApplication`; Infrastructure's `AddUsdaModule` facade composes persistence. Executable hosts remain composition roots.

## Verification

- Build: `dotnet build Modules/Usda/Application/FoodDiary.Application.Usda.csproj`
- Focused tests: `dotnet test Modules/Usda/tests/FoodDiary.Modules.Usda.Application.Tests/FoodDiary.Modules.Usda.Application.Tests.csproj`
- Architecture: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
