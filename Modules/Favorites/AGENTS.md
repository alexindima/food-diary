# Favorites Logical Module Guidelines

## Boundary

- Own favorite meal, product, and recipe aggregates, identifiers, use cases, persistence mappings, repositories, and focused tests.
- Preserve legacy CLR namespaces and the `FoodDiary.Application.Favorites` assembly name.
- Own repository ports, persistence projections, source-meal reader and errors in `Application/Abstractions`; expose semantic read services and consumer projections through `Contracts`. Central Application.Abstractions keeps only delegating Errors facades, not duplicate Favorites types.
- Preserve signatures and rebuild all hosts/consumers together after contract assembly relocation. Existing Favorites/Meals Domain references provide strongly typed IDs; no foreign aggregate mutation capability is exposed.
- Register application behavior through `AddFavoritesApplication`; composition roots use Infrastructure's `AddFavoritesModule` facade.
- Keep the shared `FoodDiaryDbContext`, historical migrations, and model snapshot in central Infrastructure.
- Favorites records are private user-associated data keyed by `UserId`; do not broaden access or logging during boundary changes.

## Verification

- `dotnet test Modules/Favorites/tests/FoodDiary.Modules.Favorites.Application.Tests/FoodDiary.Modules.Favorites.Application.Tests.csproj`
- `dotnet test Modules/Favorites/tests/FoodDiary.Modules.Favorites.Domain.Tests/FoodDiary.Modules.Favorites.Domain.Tests.csproj`
- `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
