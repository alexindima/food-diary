# Favorites Logical Module Guidelines

## Boundary

- Own favorite meal, product, and recipe aggregates, identifiers, use cases, persistence mappings, repositories, and focused tests.
- Own repository ports, persistence projections, meal/product/recipe source readers and errors in `Application.Abstractions`; expose owner read requests and consumer projections through `Contracts`. Central Application.Abstractions does not export Favorites types or Errors facades.
- Preserve signatures and rebuild all hosts/consumers together after contract assembly relocation. Reference Domain.Contracts directly for foreign IDs; no foreign aggregate capability is exposed.
- Register application behavior through `AddFavoritesApplication`; composition roots use Infrastructure's `AddFavoritesModule` facade.
- Keep the shared `FoodDiaryDbContext`, historical migrations, and model snapshot in central Infrastructure.
- Favorites records are private user-associated data keyed by `UserId`; do not broaden access or logging during boundary changes.

Meal favorites use `RemovedAtUtc` for reversible removal. The owner mapping filters removed records from normal reads, including composition queries and counts. Only the owner-scoped restore lookup bypasses this filter; it must retain the explicit `UserId` predicate. Restore preserves the original ID, name and creation timestamp, checks source access, and refuses to replace another active favorite for the same meal. The unique user/meal index applies only to active rows. Source/user cascade deletion still physically removes the retained records. Apply the `PreserveRemovedMealFavorites` migration before deploying the new API; its rollback discards removed records before reinstating the unfiltered unique index.

## Verification

- `dotnet test Modules/Favorites/tests/FoodDiary.Modules.Favorites.Application.Tests/FoodDiary.Modules.Favorites.Application.Tests.csproj`
- `dotnet test Modules/Favorites/tests/FoodDiary.Modules.Favorites.Domain.Tests/FoodDiary.Modules.Favorites.Domain.Tests.csproj`
- `dotnet test Tooling/tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`

## Error ownership

Feature error factories belong to their existing owner contracts; call them directly.
The corresponding central Errors facades are retired. Preserve exact codes, messages,
kinds and parameter formatting. Reference the owner explicitly; this grants no foreign
repository or aggregate capability. See docs/ai/feature-error-retirement.md.

Favorites owns its source DTOs and Result-returning source ports. Meals, Products
and Recipes implement them and preserve their existing not-found errors.
Meal-favorite read requests and MealFavoriteMealModel belong to Favorites Contracts.
Composition queries join foreign sources with AsNoTracking; repositories track only Favorites rows.

Apply the requested AsTracking/AsNoTracking mode to the owner query after the
composition visibility check. PostgreSQL coverage must verify identity reuse,
persisted updates and absence of foreign tracked entities.

Stable favorite IDs live in Domain.Contracts. Favorites Domain references Meals Domain.Contracts for MealId; this does not expose Meals aggregates.

Favorites and MealPlanning extend scalar model protection to twenty-two assemblies. Central typed composers preserve six Favorites Cascade FKs and four MealPlanning relationships: optional MealPlan User Cascade, MealPlanMeal Recipe Restrict, ShoppingList User Cascade, and optional ShoppingListItem Product SetNull. Same-owner mappings, indexes, converters and source provenance stay local. Central Infrastructure references Products.Domain explicitly; no schema or API change is intended.

FavoritesDbContext owns all three favorite entities and saves through the shared unit of work. Repositories receive owner DbSets. Product/recipe visibility predicates and joined DTOs live in composition behind IFavoriteProductQuery/IFavoriteRecipeQuery, returning only authorized IDs and immutable models. Entity retrieval adds a second owner read; keep user predicates on both reads and apply owner tracking only after authorization. GetOwnedById deliberately permits removal of an inaccessible source's favorite. Preserve source/User Cascade constraints in the central model. PostgreSQL tests must verify same tracked instance, persisted edits, revocation after public-to-private changes, and atomic FK failure.

Use the canonical project name as the namespace root and match folders. Projects are siblings. Public owner use cases are Contracts requests dispatched through ISender; keep outbound source ports and reusable algorithms separate. Preserve authorization, cancellation, wire shapes and persistence semantics.
