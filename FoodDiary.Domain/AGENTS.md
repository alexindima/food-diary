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
- No entity definitions remain here. User belongs to Users Domain; UserId belongs to Users Domain.Contracts. This residual shared-domain project references only shared primitives.
- BleedingType, CycleSymptomCategory, and OvulationTestResult belong to Cycles Domain. Do not re-export them or add a reverse dependency on Cycles Domain.
- ReportStatus and ReportTargetType belong to ContentReports Domain. Do not re-export them or add a reverse dependency on ContentReports Domain.

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
relationships to module-owned User/Product/Recipe types are one-way; do not add a central
`User.ShoppingLists` inverse navigation. Preserve private EF setters.

- Exercises owns ExerciseEntry, ExerciseEntryId and ExerciseType under Modules/Exercises/Domain. Its one-way User navigation requires no central back-reference; internal DomainGuard is accessed through explicit module IVT.

## Recipes physical ownership

Recipes owns Recipe/Steps/Ingredients, recipe-only value objects/events and focused domain tests under `Modules/Recipes/Domain`. Recipe IDs live in dependency-free `Modules/Recipes/Domain.Contracts` with stable namespaces; consumers reference that seam directly. User/Product/MealItem expose no Recipe inverse CLR navigations, while EF preserves the relationships unidirectionally. Shared context/migrations/snapshot and mixed cross-module tests stay central.

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

Products owns Product, product-only value objects, ProductId contracts, use cases,
ports, persistence adapters, EF model and focused tests under `Modules/Products`.
Consumers reference Products Domain.Contracts for ProductId; User and MealItem have no Product inverse
navigation. Shared context/migrations and composition lock remain central. Hosts explicitly
compose AddProductsModule; JobManager adds AddProductsPersistence without new
handlers. See `docs/ai/products-ownership-inventory.md` for the boundary and
coordinated-rebuild compatibility promise.

## Meals physical ownership

Meals owns Meal, MealItem, MealAiSession, MealAiItem, their IDs, meal-only states,
nutrition event and AI item/session enums under `Modules/Meals/Domain`, with stable
CLR namespaces. User has no inverse Meals collection; Meal.User remains a one-way
relationship with the same required FK and cascade. Shared User/UserId, enums,
DbContext, migrations and snapshot remain with their existing owners. Product,
Recipe and Image links remain ID-based with unchanged batch snapshot fallbacks.
No extra Domain.Contracts project is needed by the current acyclic graph.
See `docs/ai/meals-ownership-inventory.md` for source evidence and remaining seams.

## RecentItems physical ownership

RecentItem, RecentItemType and RecentItemId live under `Modules/RecentItems/Domain` with stable CLR namespaces. The one-way User navigation remains; User/UserId belong to Users Domain/Domain.Contracts; no inverse navigation is introduced. See `docs/ai/recent-items-ownership-inventory.md`.

## Users physical ownership

Users owns the complete User aggregate, all credential/security partials, roles,
role audit and weight/waist goals under `Modules/Users/Domain`. `UserId` lives in
`Modules/Users/Domain.Contracts`, depending only on shared primitives. Consumers
reference the exact owner; central Domain retains shared guards and values without
an aggregate re-export. Authentication flows/providers, combined UserRepository,
DbContext, migrations and snapshot retain their existing owners. CLR namespaces,
security behavior and EF/HTTP contracts are unchanged. See
`docs/ai/users-domain-extraction.md` for residual seams and verification evidence.
