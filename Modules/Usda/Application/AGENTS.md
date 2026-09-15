# USDA Application Module Guidelines

## Scope

Rules for `Modules/Usda/Application/`.

## Boundary

- Own USDA queries, product-link commands, mappings, and USDA read services.
- Consume Meals nutrition through USDA-owned `IUsdaMealNutritionReadService`; never load Meal aggregates.
- Keep provider access behind Contracts and persistence behind Application.Abstractions.
- Expose combined food search through SearchUsdaFoodsQuery in Contracts.
- Register application behavior through `AddUsdaApplication`; Infrastructure's `AddUsdaModule` facade composes persistence. Executable hosts remain composition roots.

## Verification

- Build: `dotnet build Modules/Usda/Application/FoodDiary.Modules.Usda.Application.csproj`
- Focused tests: `dotnet test Modules/Usda/tests/FoodDiary.Modules.Usda.Application.Tests/FoodDiary.Modules.Usda.Application.Tests.csproj`
- Architecture: `dotnet test Tooling/tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`

SearchUsdaFoodsQuery belongs to Contracts and is handled here. Products delegates combined local/provider search through ISender. Detail and daily-summary orchestration live in their handlers. IUsdaProductLinkService and IUsdaMealNutritionReadService remain consumer-owned technical ports in Contracts.

Scale USDA daily nutrients by grams / 100, never by Product.BaseAmount. Exclude non-gram quantities until an explicit mass conversion exists, and report them as uncovered products.
