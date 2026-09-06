# USDA Application Module Guidelines

## Scope

Rules for `Modules/Usda/Application/`.

## Boundary

- Own USDA queries, product-link commands, mappings, and USDA read services.
- Consume Meals nutrition through USDA-owned `IUsdaMealNutritionReadService`; never load Meal aggregates.
- Keep provider access and persistence behind contracts from `FoodDiary.Application.Abstractions`.
- Expose product suggestions through `IUsdaProductSuggestionReadService` rather than USDA implementation types.
- Register application behavior through `AddUsdaApplication`; Infrastructure's `AddUsdaModule` facade composes persistence. Executable hosts remain composition roots.

## Verification

- Build: `dotnet build Modules/Usda/Application/FoodDiary.Application.Usda.csproj`
- Focused tests: `dotnet test Modules/Usda/tests/FoodDiary.Modules.Usda.Application.Tests/FoodDiary.Modules.Usda.Application.Tests.csproj`
- Architecture: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`

USDA owns IUsdaProductLinkService in Application/Abstractions. Products implements
that port and returns its original Result errors; USDA propagates them without
referencing Products application contracts. Suggestion readers also live in USDA
Application/Abstractions, so Products never references the USDA implementation.
