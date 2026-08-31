# Meals ownership inventory

Source audit base: `c2412773f66604d4143d170ee50031f1f1f58367`.

| Responsibility | Physical owner and compatibility boundary |
| --- | --- |
| 60 application source files: create/update/delete/repeat, detail/list/overview queries, validation, mappings, nutrition calculation, image cleanup and activity/export reads | `Modules/Meals/Application`; preserve the `FoodDiary.Application.Meals` assembly name, root namespace and existing CLR namespaces |
| Six aggregate repository ports | `Modules/Meals/Application/Abstractions`; they remain implementation-facing Meals ports and are not exported as general consumer contracts |
| Activity/favorite semantic reads, export/product-nutrition reads, query filters and projection records | `Modules/Meals/Contracts`; existing CLR namespaces are preserved. Export, Favorites, Gamification, USDA, WeeklyGoals and WeeklyCheckIn consume only the narrow capability they require. Dashboard retains an application reference because it sends Meals mediator queries. |
| MealRepository and scoped repository aliases | `Modules/Meals/Infrastructure`; query shapes, access predicates, tracking modes, save boundaries and cancellation remain unchanged |
| MealConfiguration, MealItemConfiguration, MealAiSessionConfiguration and MealAiItemConfiguration | `Modules/Meals/Infrastructure/Model`; the shared context invokes `ApplyMealsPersistenceModel`, preserving tables, indexes, conversions, xmin, relationships and delete behavior |
| Meal, MealItem, MealAiSession, MealAiItem, their IDs/enums/value objects and invariants | Central Domain seam. `User.Meals`, `Meal.User`, `Product.MealItems`, `Recipe.MealItems` and the MealItem inverse navigations form public bidirectional CLR graphs. Moving only Meals Domain would create cycles or require a compatibility-breaking redesign of Users, Products and Recipes. |
| UserConfiguration and user cleanup | Central Infrastructure seam. User owns the inverse collection and account deletion workflow; Meals does not duplicate or relocate that configuration. |
| FoodDiaryDbContext, Meals DbSets, migrations and model snapshot | Central Infrastructure seam. The context remains the database composition root and migrations/snapshot remain one ordered history; physical EF configuration ownership does not create a separate database or migration stream. |
| HTTP endpoints, authorization, Swagger contracts and host configuration | Existing Presentation/API owners. No route, payload, status, policy or Swagger-visible behavior changes. API and Initializer compose `AddMealsModule`; JobManager uses `AddMealsPersistence` and does not acquire Meals handlers. |
| Tests | Meals-only application tests, Meal/MealAI invariant tests and the MealRepository PostgreSQL suite move into three nested module test projects. Mixed central Domain, DI, export/provider, Presentation, Web API and cross-module suites remain with their proven owners; no test is duplicated. |

Compatibility means coordinated rebuilding of consumers and executable hosts. The
legacy application assembly identity and CLR namespaces are retained, but relocated
ports and adapters do not promise binary compatibility with old precompiled hosts.
There is no schema, migration, API or data migration in this extraction.

## Source review and rollout

The repository audit covered the application slices and ports, all four aggregate
entities, the four EF mappings, repository SQL/LINQ behavior, host registrations,
Docker build inputs, direct consumers, focused tests, mixed suites and the real
PostgreSQL repository fixture. No evidence supported creating a Meals Domain project:
the central inverse graph above is the governing seam, not unfinished symmetry.

Deployments must use coordinated host builds containing the five new production
project outputs and corresponding Docker COPY paths. Rollback is a rebuild and
redeploy of the previous complete revision; no data rollback is required. Verify
authenticated meal reads/mutations, nutrition composition, private product/recipe
access, AI-session persistence and export/dashboard projections through existing
journeys and normal error/latency signals. Deployment itself is outside this task.
