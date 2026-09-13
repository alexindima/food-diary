# 0040: Hydration runtime context pilot

Status: Accepted

## Decision

Hydration repositories and interval reads use a scoped HydrationDbContext whose
model contains only HydrationEntry and HydrationOperationReceipt. Reuse the owner
PersistenceModel configurations and existing table names. Do not include User in
this runtime model. PostgreSQL still enforces the existing user cascade constraints.

FoodDiaryDbContext remains the complete migration model, read-composition surface
and account-purge bridge during this pilot. No table, column, FK or migration is
removed. This is runtime ownership isolation, not independent databases or complete
removal of the central hydration mapping.

The host creates the module context through the central context's provider factory.
It shares the same scoped connection and command telemetry, but has its own model
and tracker. Central Infrastructure depends on generic DbContext participants, never
the Hydration implementation. DI owns the module context lifetime.

IUnitOfWork detects pending changes in both contexts and dispatches domain events
before saving. Module writes trigger one coordinated relational transaction. An
existing caller transaction is reused with a savepoint covering both saves; otherwise
the provider execution strategy retries the complete transaction. Change acceptance
and event clearing happen only after successful coordinated persistence. Repository
methods never save independently. The command pipeline flushes post-commit actions
only after the unit of work returns successfully, as before.

Direct central SaveChanges rejects pending module writes instead of silently
committing only the central tracker. Shared top-level transaction guards/reset include
module trackers. Nonrelational test providers support a single pending tracker only;
atomic multi-context persistence requires the production relational provider.

## Consequences and verification

Legacy composition reads and user purge retain the central context deliberately.
Their replacement is a later bounded step; do not migrate other modules merely by
copying registration before reviewing their events, transactions and cross-owner SQL.
Future module domain events that stage additional work require explicit coverage of
event ordering and any newly participating contexts.

PostgreSQL tests verify joint commits, receipt-conflict rollback of both trackers,
caller rollback, provider retries, user cascade, and the two-entity runtime model.
Existing Hydration repository/receipt tests protect mappings and constraints; central
unit-of-work, transaction and API tests protect orchestration. The migration model
must remain unchanged. Deploy/revert all backend assemblies together; no database
migration or production configuration change is required.

EF Core cross-context transactions require a shared connection and transaction:
https://learn.microsoft.com/en-us/ef/core/saving/transactions#cross-context-transaction
## BodyMetrics follow-up

BodyMetrics is the second runtime context using the same shared coordinator.
It maps only WeightEntry and WaistEntry with the existing owned configurations
and table names. Weight/waist goals remain in Users. Repositories receive their
owned DbSet; user predicates, date handling and read projections are unchanged.
The module currently raises no domain events. Central migration mappings,
cross-module reads and the existing purge participant remain in place.

The follow-up adds no migration or coordinator behavior. Cross-module PostgreSQL
tests cover one save across Users, Hydration and BodyMetrics, rollback of earlier
writes after a later module constraint failure, user isolation, updates/deletes
and persisted User cascade constraints. This validates a second owner without
claiming that all modules can be transferred without transaction/event review.

## Exercises follow-up

Exercises is the third runtime owner, mapping only ExerciseEntry to the existing
ExerciseEntries table. Reuse its owned model and supply an owned DbSet to the
repository. UpdateAsync remains a no-op: the handler loads with tracking and
mutates the aggregate before the common UoW saves. No domain events are raised
by the current exercise aggregate. User predicates, date normalization, calories,
ordering and projections are unchanged.

Four-context PostgreSQL coverage verifies shared commits, tracked exercise
updates, deletes, User cascade and rollback of an already-saved Exercises write
when BodyMetrics later fails. Central migrations, cross-module reads and purge
retain the existing model. No schema or API migration is required; deploy or
revert coordinated backend assemblies as for the pilot.

## Cycles follow-up

Cycles is the fourth runtime owner. Its context applies all eight existing owner
configurations: profile, bleeding, symptoms, factors, fertility signals, episodes,
consents and prediction revisions. Only the profile DbSet reaches the repository.
Explicit table mappings, split queries and field-backed child relationships are
unchanged. The current aggregate raises no domain events.

Focused PostgreSQL tests verify nested tracking and projections, user isolation,
User cascade through the profile to children, and atomic rollback of central User
and nested Cycle records after a uniqueness failure. The existing repository suite
also protects all detail projections and owner-only deletion. Central migration,
read and purge bridges remain; no schema or API migration is required.

RecentItems direct SQL/post-commit work and Wearables serialized transaction runner
need a separate transaction-entry review before using a runtime context. The
shared coordinator enlists contexts during saving, which is not evidence that
SQL executed earlier in a use case already shares its transaction.

## Fasting follow-up

Fasting is the fifth runtime owner. Its five existing entity mappings are applied
by FastingDbContext. Repositories receive only their owned DbSet; plan/occurrence
and occurrence/check-in relationships stay inside the model. Sessions retain
tracked updates and the existing detached-state check. The registration uses the
shared connection and unit-of-work coordinator, retaining central migrations,
composed reads and user purge. No schema or API change is required.

Telemetry cleanup remains a standalone batched ExecuteDelete operation in its
existing Hangfire job. It does not wait for SaveChanges and is not moved into the
shared tracked-write transaction. PostgreSQL coverage protects batch boundaries,
shared-save rollback, session updates and User cascade.

## RecipeCommunity and MealPlanning follow-up

RecipeCommunity maps only RecipeComment and RecipeLike in its runtime context;
repositories receive narrow owned sets. MealPlanning maps its six plan/list
entities and owns all repository writes through the shared coordinator.

MealPlanRepository now receives its owned set and IMealPlanCompositionReader.
The host ReadModel.Composition adapter preserves SQL joins and returns only
immutable MealPlanReadModel and recipe snapshots. The module owns the port,
aggregate reads and snapshot attachment. No module references the composition
implementation. ShoppingLists use the owned set for reads and writes. Central
user purge and migration relationships remain unchanged.

PostgreSQL coverage verifies joint User/Recipe/module saves, nested list updates,
composed plan reads, user cleanup and rollback of an earlier planning write when
the unique user/recipe like constraint fails. No relational or HTTP change.

## Lessons follow-up

Lessons is the eighth runtime-context owner. Both NutritionLesson and
UserLessonProgress remain in one context, preserving their navigation and SQL
completion-count subquery. NutritionLessonRepository receives only these two
owned sets; AddLessonsModule registers the context through the existing shared
connection and save coordinator. The context applies the existing owner mappings
and preserves the central table names. User relationships remain in the central
migration model; no relational schema or HTTP contract change is needed.

Shared PostgreSQL coverage protects publication and locale filtering, tracked
publication changes, joint User/lesson/progress saving, User cascade deletion,
and rollback of central and module changes when a progress owner does not exist.

## DailyAdvices follow-up

DailyAdvices is the ninth runtime-context owner. Its repository receives only the
DailyAdvice set and still exposes immutable, no-tracking projections. The shared
connection factory applies the existing one-entity model with the same table
name. Locale normalization, ordering, tags and selection weights are unchanged.
This is read isolation; it does not move initializer seeding or add a write API.
Central migrations retain schema ownership. PostgreSQL coverage verifies shared
connection identity, locale normalization and absence of tracked entities in
both contexts after reading.

## Marketing follow-up

Marketing is the tenth runtime-context owner. MarketingDbContext applies the
existing attribution mapping, including event ID and per-user conversion unique
constraints. Its repository and reporting partial receive only the owned set.
Shared unit-of-work saving keeps central and module writes atomic; PostgreSQL
coverage verifies rollback of central changes for duplicate events and conversions.

Retention remains immediate ExecuteDelete in bounded, cancellation-aware batches,
independent of tracked SaveChanges. The existing job schedule, retention period,
report filters, pagination and data visibility are unchanged. Shared provider tests
also exercise cutoff preservation and range reporting on the owned context.
No schema or HTTP contract change is required.

## OpenFoodFacts follow-up

OpenFoodFacts is the eleventh runtime-context owner. Its cache still uses immediate
parameterized PostgreSQL ON CONFLICT updates; moving them behind SaveChanges
would change concurrency and persistence behavior. The repository uses its own
context and a live Func<DbTransaction?> accessor supplied only by registration.
Before cache reads or writes, UseTransactionAsync synchronizes with the current
shared transaction, including null after completion. The accessor does not begin,
commit or roll back transactions. This explicitly handles operations occurring
before the unit-of-work save coordinator enlists module contexts.

Provider tests cover transactions opened after repository resolution, visibility
before commit, rollback, and subsequent writes after transaction disposal. Existing
PostgreSQL cache tests now execute through the owner context and protect parallel
upserts, deduplication, input validation, wildcard escaping and saturated counters.
Shared migration ownership, SQL text, ranking and HTTP provider behavior remain
unchanged. No schema or HTTP contract migration is required.

## USDA follow-up

USDA is the twelfth runtime-context owner. UsdaDbContext applies the five existing
reference-data mappings. Food/nutrient/portion relationships remain inside the
model; the repository receives only the four sets used by its queries and remains
no-tracking/read-only. Search, ordering, nutrient joins, batch projections and
age/gender reference filtering are unchanged. Central importing, migrations and
historical schema ownership remain in place; HTTP provider behavior is unchanged.

The existing PostgreSQL repository regression now resolves real module DI and
reads through the owned context after central seeding. It verifies all projection
families, missing records, age filtering, shared connection identity and empty
trackers. A model guard checks the exact five entity types and table names.

## ContentReports follow-up

ContentReports is the thirteenth runtime-context owner. Its write repository
receives only DbSet<ContentReport>; shared saving and central User cascade mapping
remain intact. Existing IContentReportReadModelRepository and
IContentReportTargetReadService ports are implemented by ContentReportReadService
in ReadModel.Composition, which the hosts already register. No new ports or module
reference to composition are needed.

The read adapter preserves recipe visibility and comment-parent predicates,
report filters, total count, ordering and pagination, and the two page-bounded
queries for recipe titles and comment excerpts. Only immutable administrative
results and booleans leave the adapter. No authorisation, SQL or API behavior
changes are intended. PostgreSQL coverage retains moderation/visibility tests and
adds shared-save, foreign-key rollback and User cascade verification. Host DI
checks now distinguish the owner writer from the composed read aliases.
