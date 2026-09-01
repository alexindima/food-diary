# Infrastructure Layer Guidelines

## Scope
Rules for `FoodDiary.Infrastructure/`.

## Responsibilities
- EF Core persistence, external integrations, and technical implementations.
- Implement abstractions declared in upper layers.

## Data Access
- Keep `DbContext` and entity configurations here.
- Use Fluent API for mapping and constraints.
- Keep migrations in this project.

## Rules
- Do not move domain rules from aggregates into persistence code.
- Keep dependency direction inward (Infrastructure depends on Application/Domain, not vice versa).
- `FoodDiary.Infrastructure` may reference `FoodDiary.Application.Abstractions`, `FoodDiary.Domain`, and `Shared/FoodDiary.Mediator`; do not reference `FoodDiary.Application`, presentation projects, host projects, or resources.
- Keep shared external provider adapters that are not persistence concerns in `FoodDiary.Integrations`; Notifications-owned web-push adapters live in `Modules/Notifications/Infrastructure`.
- Notifications configurations are registered explicitly from its PersistenceModel assembly; generic outbox processing/claiming/replay remain central and use the shared Outbox.Abstractions contract.
- Keep repository-owned `SaveChangesAsync` and manual transactions inside the architecture-test allowlist. `AiQuotaRepository` is an explicit exception: its short PostgreSQL transactions atomically reserve or reconcile quota independently of the request transaction, and must never contain an external provider call.
- `EfWeeklyGoalTransactionRunner` is an explicit exception: it uses a short advisory-lock transaction to serialize creation for one `(UserId, WeekStartUtc)` key and prevent unique-constraint races; keep notification delivery and other external calls outside that transaction.
- `EfProductMutationTransactionRunner` is an explicit exception: it shares the Recipe-composition advisory lock and keeps the Product row lock, usage check, and mutation in one short transaction.
- `EfRecipeMutationTransactionRunner` is an explicit exception: it shares the Recipe-composition advisory lock and keeps graph/usage checks and mutation in one short transaction.
- Do not reintroduce direct SMTP delivery configuration into the primary API/infrastructure path; MailRelay owns mail delivery runtime configuration.
- Keep retries/logging policies consistent with API composition.

## Commands
- Build: `dotnet build FoodDiary.Infrastructure/FoodDiary.Infrastructure.csproj`
- Tests: `dotnet test tests/FoodDiary.Infrastructure.Tests/FoodDiary.Infrastructure.Tests.csproj`

## EF Core Migrations
- Always commit both files for each migration: `*.cs` and `*.Designer.cs`.
- Add `[ExcludeFromCodeCoverage]` to the migration implementation class and model snapshot so generated EF code does not affect dotCover/code coverage.
- **CRITICAL**: `dotnet ef migrations add` generates Allman-style braces, but the project requires K&R style. After generating, you MUST:
  1. Remove `using System;` (implicit usings are enabled)
  2. Convert ALL `)\n{` and `=>\n{` patterns to `) {` / `=> {` â€” especially `constraints: table =>\n{`
  3. Strip UTF-8 BOM if present
  4. Ensure LF line endings (not CRLF)
- Before commit, run a whitespace formatter/check on migration files. Preferred command: `dotnet format whitespace FoodDiary.Infrastructure/FoodDiary.Infrastructure.csproj`. This specifically avoids CI failures like `WHITESPACE: Fix whitespace formatting` in generated migrations.
- See `docs/backend/BACKEND_MIGRATION_SAFETY.md` for migration safety guidance.

## MealPlanning

MealPlanning repositories and DI live in Modules/MealPlanning/Infrastructure;
all six EF mappings live in its Model project and are applied explicitly by the
shared context. Hosts and repository-resolution fixtures call AddMealPlanningModule
in addition to AddInfrastructure. Central Infrastructure must not reference the
module adapter project. User cleanup, DbContext and migrations remain here.

- Exercises mapping is registered through ApplyExercisesPersistenceModel; its repository and complete DI belong to Modules/Exercises/Infrastructure. Keep the central context, migrations and snapshot here.

- Dashboard projection readers live in Modules/Dashboard/Infrastructure; hosts explicitly call AddDashboardReadServices after AddInfrastructure. Shared context and migration ownership remain here.

## Recipes physical ownership

Recipes use cases, ports, read contracts, persistence model and adapters live under `Modules/Recipes`. Recipe/Steps/Ingredients, IDs/value objects/events remain central Domain because public User/MealItem/Product inverse navigations prohibit a one-way extraction. Shared context/migrations/snapshot and cross-module tests stay central. Hosts compose AddRecipesModule; JobManager uses AddRecipesPersistence without adding application handlers. See `docs/ai/recipes-ownership-inventory.md`; this is not full Domain/database isolation.

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

MealRepository and the four Meals EF mappings live under `Modules/Meals`; the shared
context explicitly applies `ApplyMealsPersistenceModel`. Keep UserConfiguration,
user cleanup, Meals DbSets, migrations and snapshot central. Hosts use AddMealsModule;
JobManager uses AddMealsPersistence only. See `docs/ai/meals-ownership-inventory.md`.

## RecentItems physical ownership

RecentItems repository, post-commit recorder, DI and EF mapping live under `Modules/RecentItems`. The shared context applies its model; migrations/snapshot and post-commit queue/UoW stay central. See `docs/ai/recent-items-ownership-inventory.md`.
