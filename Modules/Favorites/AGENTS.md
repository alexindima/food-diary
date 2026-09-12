# Favorites Logical Module Guidelines

## Boundary

- Own favorite meal, product, and recipe aggregates, identifiers, use cases, persistence mappings, repositories, and focused tests.
- Preserve legacy CLR namespaces and the `FoodDiary.Application.Favorites` assembly name.
- Own repository ports, persistence projections, meal/product/recipe source readers and errors in `Application/Abstractions`; expose semantic read services and consumer projections through `Contracts`. Central Application.Abstractions does not export Favorites types or Errors facades.
- Preserve signatures and rebuild all hosts/consumers together after contract assembly relocation. Reference Domain.Contracts directly for foreign IDs; no foreign aggregate capability is exposed.
- Register application behavior through `AddFavoritesApplication`; composition roots use Infrastructure's `AddFavoritesModule` facade.
- Keep the shared `FoodDiaryDbContext`, historical migrations, and model snapshot in central Infrastructure.
- Favorites records are private user-associated data keyed by `UserId`; do not broaden access or logging during boundary changes.

## Verification

- `dotnet test Modules/Favorites/tests/FoodDiary.Modules.Favorites.Application.Tests/FoodDiary.Modules.Favorites.Application.Tests.csproj`
- `dotnet test Modules/Favorites/tests/FoodDiary.Modules.Favorites.Domain.Tests/FoodDiary.Modules.Favorites.Domain.Tests.csproj`
- `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`

## Error ownership

Feature error factories belong to their existing owner contracts; call them directly.
The corresponding central Errors facades are retired. Preserve exact codes, messages,
kinds and parameter formatting. Reference the owner explicitly; this grants no foreign
repository or aggregate capability. See docs/ai/feature-error-retirement.md.

Favorites owns its source DTOs and Result-returning source ports. Meals, Products
and Recipes implement them and preserve their existing not-found errors.
IMealFavoriteReadService and MealFavoriteMealModel belong to Favorites Contracts.
Repositories join foreign sources with AsNoTracking and track only Favorites rows.

EF tracking mode affects the complete query. Apply the requested AsTracking/
AsNoTracking mode after correlated foreign access predicates; otherwise nested
AsNoTracking can silently detach the owned Favorite row. PostgreSQL coverage must
verify identity reuse, persisted updates and absence of foreign tracked entities.

Stable favorite IDs live in Domain.Contracts. Favorites Domain references Meals Domain.Contracts for MealId; this does not expose Meals aggregates.

Favorites and MealPlanning extend scalar model protection to twenty-two assemblies. Central typed composers preserve six Favorites Cascade FKs and four MealPlanning relationships: optional MealPlan User Cascade, MealPlanMeal Recipe Restrict, ShoppingList User Cascade, and optional ShoppingListItem Product SetNull. Same-owner mappings, indexes, converters and source provenance stay local. Central Infrastructure references Products.Domain explicitly; no schema or API change is intended.
