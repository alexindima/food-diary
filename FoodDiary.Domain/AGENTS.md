# Domain Layer Guidelines

## Scope
Rules for `FoodDiary.Domain/`.

## Responsibilities
- Keep pure domain model: entities, value objects, domain services, domain events.
- Enforce invariants inside aggregates.
- Prefer strongly typed IDs/value objects on public surfaces.

## Boundaries
- No infrastructure concerns (EF, HTTP, external services).
- No UI/API contracts.
- Only shared domain primitives belong as a project reference.
- Shared `User` and `UserId` remain central compatibility types; extracted module Domain projects may depend on them one-way without moving module aggregates back into this project.

## Design Rules
- Prefer factory/static creation methods when invariants are non-trivial.
- Keep behavior close to data (rich domain where useful).
- Keep namespaces aligned with folder structure.
- Keep `FoodDiary.Domain.csproj` minimal and inherit shared settings from root `Directory.Build.props` unless domain-specific settings are strictly required.
- Normalize date/time inputs consistently:
  - For timestamps: convert to UTC with `ToUniversalTime()`.
  - For date-only domain fields: store UTC date (`utc.Date` with `DateTimeKind.Utc`).
- Prefer explicit validation errors over silent correction for invalid domain input ranges.
- Canonicalize user-facing codes on write (e.g. language/gender codes) and trim profile text input.
- Enforce invariants in link entities as well (reject `Empty` IDs in constructors/factories).
- For `double`-based value objects, validate finite values (`!NaN` and `!Infinity`) in addition to range checks.
- Keep strongly typed IDs in `ValueObjects/Ids` with one ID type per file.

## Commands
- Build: `dotnet build FoodDiary.Domain/FoodDiary.Domain.csproj`

## MealPlanning compatibility seam

Keep User.ShoppingLists, ShoppingList/items/sources, their IDs/events/source enum,
and MealPlanId/MealPlanMealId central. The bidirectional public User relationship
and source IDs prevent a one-way extraction of this graph. MealPlan entities and
MealPlanDayId now live in Modules/MealPlanning/Domain. Preserve private EF setters.

- Exercises owns ExerciseEntry, ExerciseEntryId and ExerciseType under Modules/Exercises/Domain. Its one-way User navigation requires no central back-reference; internal DomainGuard is accessed through explicit module IVT.

## Recipes physical ownership

Recipes use cases, ports, read contracts, persistence model and adapters live under `Modules/Recipes`. Recipe/Steps/Ingredients, IDs/value objects/events remain central Domain because public User/MealItem/Product inverse navigations prohibit a one-way extraction. Shared context/migrations/snapshot and cross-module tests stay central. Hosts compose AddRecipesModule; JobManager uses AddRecipesPersistence without adding application handlers. See `docs/ai/recipes-ownership-inventory.md`; this is not full Domain/database isolation.
