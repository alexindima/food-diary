# Meals ownership inventory

Source audit base: `da09f60fa3fa3c6cf116396a01fc9c9be7e15c45`.

| Responsibility | Physical owner and compatibility boundary |
| --- | --- |
| 60 application source files: create/update/delete/repeat, detail/list/overview queries, validation, mappings, nutrition calculation, image cleanup and activity/export reads | `Modules/Meals/Application`; preserve the `FoodDiary.Application.Meals` assembly name, root namespace and existing CLR namespaces |
| Six aggregate repository ports | `Modules/Meals/Application/Abstractions`; they remain implementation-facing Meals ports and are not exported as general consumer contracts |
| Activity/favorite semantic reads, export/product-nutrition reads, query filters and projection records | `Modules/Meals/Contracts`; existing CLR namespaces are preserved. Export, Favorites, Gamification, USDA, WeeklyGoals and WeeklyCheckIn consume only the narrow capability they require. Dashboard retains an application reference because it sends Meals mediator queries. |
| MealRepository and scoped repository aliases | `Modules/Meals/Infrastructure`; query shapes, access predicates, tracking modes, save boundaries and cancellation remain unchanged |
| MealConfiguration, MealItemConfiguration, MealAiSessionConfiguration and MealAiItemConfiguration | `Modules/Meals/Infrastructure/Model`; the shared context invokes `ApplyMealsPersistenceModel`, preserving tables, indexes, conversions, xmin, relationships and delete behavior |
| Meal, MealItem, MealAiSession, MealAiItem, MealAiItemData; four Meal IDs; MealDetailsState, MealNutritionState, MealNutritionUpdate, MealAiItemState; MealNutritionAppliedDomainEvent; MealItemOrigin, MealAiSessionStatus, MealAiItemResolution | `Modules/Meals/Domain`, with unchanged CLR namespaces and invariants |
| UserConfiguration and user cleanup | `Modules/Users/Infrastructure`; remove only the obsolete User.Meals field-access mapping. Cleanup already uses explicit set-based Meals deletion. User/UserId and goals stay central. |
| FoodDiaryDbContext, Meals DbSets, migrations and model snapshot | Central Infrastructure seam. The context remains the database composition root and migrations/snapshot remain one ordered history; physical EF configuration ownership does not create a separate database or migration stream. |
| HTTP endpoints, authorization, Swagger contracts and host configuration | Existing Presentation/API owners. No route, payload, status, policy or Swagger-visible behavior changes. API and Initializer compose `AddMealsModule`; JobManager uses `AddMealsPersistence` and does not acquire Meals handlers. |
| Tests | Meals-only application tests, Meal/MealAI invariant tests and the MealRepository PostgreSQL suite move into three nested module test projects. Mixed central Domain, DI, export/provider, Presentation, Web API and cross-module suites remain with their proven owners; no test is duplicated. |

Compatibility means coordinated rebuilding of consumers and executable hosts. The
legacy application assembly identity and CLR namespaces are retained, but relocated
ports and adapters do not promise binary compatibility with old precompiled hosts.
There is no schema, migration, API or data migration in this extraction.

## Source review and rollout

The remaining User.Meals collection had no behavior or query consumers: only its declaration, EF field-access configuration and a read-only collection test referenced it. Removing that inverse CLR navigation permits one-way Meals Domain -> central Domain; Meal.User uses schema-equivalent WithMany() and retains its required UserId FK/cascade. Favorites Domain references Meals Domain for FavoriteMeal.Meal. Existing Meals Contracts already references Favorites Domain and now references Meals Domain explicitly; no reverse dependency or additional Domain.Contracts project is necessary.

Deployments must use coordinated host builds containing the Meals production
project outputs and corresponding Docker COPY paths. Rollback is a rebuild and
redeploy of the previous complete revision; no data rollback is required. Verify
authenticated meal reads/mutations, nutrition composition, private product/recipe
access, AI-session persistence and export/dashboard projections through existing
journeys and normal error/latency signals. Deployment itself is outside this task.

## Remaining shared seams and query compatibility

MealType and AiRecognitionSource belong to Meals Domain with unchanged
FoodDiary.Domain.Enums namespaces, names and numeric values. MealType is also
used by MealPlanning, Dashboard, Export and Favorites; actual enum consumers
reference Meals Domain directly. String-only DTO consumers retain their existing
contract references. These enum moves require coordinated consumer rebuilds.
MeasurementUnit and Visibility are reused across product/recipe workflows.
User belongs to Users Domain; UserId and ActivityLevel belong to Users
Domain.Contracts. DomainGuard and common constants remain central; explicit IVT
permits the extracted domain to use DomainGuard.
ProductId, RecipeId and ImageAssetId retain their existing owner contract projects.
No Product, Recipe or Image CLR navigation is reintroduced.

MealRepository batch/left-join projections and application snapshot mappings are
unchanged, including pre-snapshot rows, current ProductType and image URL values.
The shared DbContext, ordered migrations and snapshot remain unchanged. Runtime,
HTTP and relational compatibility require a coordinated rebuild, not compatibility
with previously compiled assemblies. All three Docker hosts copy Meals Domain.

Fourteen Meals-only donor test methods move to MealExtractedInvariantTests;
the twenty reflection-discovered ID cases now run against the Meals assembly in
MealIdInvariantTests, preserving coverage after the central assembly loses those IDs.
mixed aggregate/event/recipe coverage remains central. The module PostgreSQL
regression checks one-way User loading, soft-delete/restore retention, database
cascade through items/sessions/AI items and isolation of another user's rows.

## Wiki findings

The adaptive start inferred completed Application-extraction acceptance from the
module name. Before implementation, the task replaced it with explicit Domain
criteria using acceptance-init and delivery-replan. Evidence is in the named
`.artifacts/llm-wiki/tasks/meals-domain-extraction` workspace; the original inferred
matrix is retained in `.artifacts/meals-domain-evidence`. No generator/ranking
special case was added. Verification results are recorded separately from plans.
