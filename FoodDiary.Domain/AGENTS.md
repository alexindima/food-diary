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

## MealPlanning physical ownership

MealPlans and ShoppingLists aggregates, their IDs, ShoppingLists events and source
enum live in `Modules/MealPlanning/Domain` with stable CLR namespaces. Their
relationships to central User/Product/Recipe types are one-way; do not add a central
`User.ShoppingLists` inverse navigation. Preserve private EF setters.

- Exercises owns ExerciseEntry, ExerciseEntryId and ExerciseType under Modules/Exercises/Domain. Its one-way User navigation requires no central back-reference; internal DomainGuard is accessed through explicit module IVT.

## Recipes physical ownership

Recipes owns Recipe/Steps/Ingredients, recipe-only value objects/events and focused domain tests under `Modules/Recipes/Domain`. Recipe IDs live in dependency-free `Modules/Recipes/Domain.Contracts` with stable namespaces; central Domain references only that seam. User/Product/MealItem expose no Recipe inverse CLR navigations, while EF preserves the relationships unidirectionally. Shared context/migrations/snapshot and mixed cross-module tests stay central.

## Admin physical ownership

Admin owns application slices, billing-report/impersonation/mail-reader ports,
AdminImpersonationSession Domain, its explicit EF model and reporting/session
adapters under Modules/Admin. Legacy application assembly and CLR namespaces
remain stable; compatibility requires coordinated host rebuilds. Email templates
remain Identity-owned and role audit/User capabilities remain Users-owned despite
legacy Admin namespaces. Shared context/migrations/cleanup, SSO store/JWT providers,
HTTP authorization, structured audit and MailInbox client bridge remain central.
Hosts call AddAdminModule; JobManager adds only AddAdminPersistence. See
docs/ai/admin-ownership-inventory.md for current source evidence and test ownership.

## Products physical ownership

Products use cases, ports, consumed contracts, persistence adapters and EF model
live under `Modules/Products`; focused application tests live under its nested
tests folder. Central Product/User/RecipeIngredient/MealItem/USDA CLR navigations,
shared context/migrations and composition lock remain unchanged. Hosts explicitly
compose AddProductsModule; JobManager adds AddProductsPersistence without new
handlers. See `docs/ai/products-ownership-inventory.md` for the boundary and
coordinated-rebuild compatibility promise.

## Meals physical ownership

Meals application, ports/contracts, persistence adapter and EF model live under
`Modules/Meals`. Keep Meal, MealItem, MealAiSession, MealAiItem and their IDs/value
objects central: User.Meals and Product/Recipe MealItems are public inverse graphs.
Do not create a symmetric Meals Domain project without redesigning those seams.
See `docs/ai/meals-ownership-inventory.md`.

## RecentItems physical ownership

RecentItem, RecentItemType and RecentItemId live under `Modules/RecentItems/Domain` with stable CLR namespaces. The one-way User navigation remains; User/UserId and the absence of an inverse navigation are central seams. See `docs/ai/recent-items-ownership-inventory.md`.
