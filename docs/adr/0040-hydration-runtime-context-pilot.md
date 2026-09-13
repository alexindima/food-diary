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

## Admin follow-up

Admin now owns its runtime AdminDbContext for AdminImpersonationSession and
BugAcknowledgementReceipt. The session writer keeps its narrow entity set and
existing composed query port. AddAdminPersistence registers the shared-connection
context for both Web API and JobManager.

Receipt recording still saves immediately after durable enqueue. It uses the
shared IUnitOfWork so pending session and central changes commit atomically.
Duplicate receipt failures detach the attempted receipt and verify its existence;
unexpected persistence failures propagate. No outbound delivery behavior changes.
The Admin user purge participant deliberately retains the central context so its
immediate deletion participates in the existing user-purge transaction before
Restrict user foreign keys are removed. Cross-module reads, mappings and migration
ownership remain unchanged. PostgreSQL tests cover shared save, purge, receipt
durability, duplicate recovery and rollback on an invalid session target.

## RecentItems follow-up

RecentItemsDbContext owns the RecentItem runtime model. The existing SQL upsert
and retention operations still execute immediately; they are not deferred until
SaveChanges. Like OpenFoodFacts, the repository receives a live caller-transaction
accessor and synchronizes it before SQL or reads, including transactions started
after DI resolution and clearing the transaction after completion. SQL predicates,
conflict handling, saturated counters, timestamps and per-type limits are unchanged.
PostCommitRecentItemUsageRecorder still queues captured IDs, saves only pending
tracked changes and runs after commit. Central user purge and migrations remain
unchanged. Tests cover transaction rollback/commit/reuse, post-commit flush/discard,
concurrent upserts, retention and the InMemory fallback.

## Outbox engine preparation

The generic processing engine and claimer accept DbContext rather than requiring
FoodDiaryDbContext. All existing adapters keep their current registrations. The
clean-entry guard checks local pending changes and an existing transaction for any
context, and also checks registered module participants when the supplied context
is FoodDiaryDbContext. Claim SQL, table allowlist, leases, concurrency fencing,
retry and finalization semantics are unchanged. Dedicated-context PostgreSQL tests
cover completion, retry, pending changes, nested transactions and the retained
shared-participant guard. This prerequisite does not yet migrate Notifications,
Images or Gamification runtime persistence or their replay coordination.

## Notifications, Images and Gamification follow-up

Three runtime contexts now own notification/subscription/web-push outbox state,
image/deletion-outbox state, and achievement definition/grant/evaluation-outbox
state. Each shares the central connection and participates in the existing UnitOfWork.
Normal outbox processors use the owner context and sets, with a callback that
preserves the shared-scope clean-entry guard before each claim. Lease SQL, fencing,
retry, revision release and payload delivery remain unchanged.

Gamification SQL stores receive a live caller-transaction accessor, synchronized
before immediate inserts and reads. The existing Lesson metric port is implemented
by ReadModel.Composition. Images usage checks and SQL-limited orphan candidate IDs
also live there; the owner repository loads the bounded candidate assets. This adds
one bounded read for orphan listing. Restrict image FKs remain the final deletion
race protection and keep deletion plus outbox insertion atomic.

Replay adapters intentionally keep the central context for audit/reset transactions.
User purge and image reassignment also keep shared transaction access. User cleanup
now saves through IUnitOfWork, including image-deletion outbox entries in the owner
context. Historical migrations and central read model stay unchanged.

## WeeklyGoals runtime context

WeeklyGoalsDbContext owns WeeklyGoal tracking and saves through the shared unit of work. The repository follows the current shared transaction before each read, including late transaction creation and reuse after rollback. EfWeeklyGoalTransactionRunner retains its user/week PostgreSQL advisory lock, shared retry/reset boundary and commit responsibility. Reminder ordering and pagination, external delivery timing and the central User Cascade FK remain unchanged. Provider tests use real DI to cover concurrent creation and rollback after an owner save followed by reuse.

## Billing runtime context

BillingDbContext owns the three Billing entities and their local mappings. Narrow repositories receive owner sets plus a live shared-transaction synchronization callback, invoked before reads. The Billing transaction runner retains its advisory locks, execution strategy, clean-entry/reset and commit boundary. It saves all participants through IUnitOfWork and translates the existing payment/webhook unique constraints using the failing EF entries, including entries tracked in the owner context. Checkout leases, provider HTTP and authenticity checks, inbox retries, central User FKs and migrations do not change. Real-DI PostgreSQL coverage verifies mixed saves, both duplicate translations with shared rollback and reuse, intermediate save/read rollback, and serialized concurrent webhook creation.

## Wearables runtime context

WearablesDbContext owns WearableConnection and WearableSyncEntry; repositories receive only their own entity sets. The serialized runner retains a separate session advisory lease and executes its callback once, outside database transactions. IUnitOfWork saves owner changes with the existing persistence retry strategy. Intermediate saves remain durable on later failure; the shared reset discards remaining owner tracking. Real-DI PostgreSQL tests cover connection/sync races, one callback despite a transient save failure, and durable intermediate save followed by failure and scope reuse. Provider clients, token protection, central User Cascade FKs and migrations are unchanged.

## Favorites runtime context

FavoritesDbContext owns all three favorite entities. Composition queries return authorized product/recipe favorite IDs and existing immutable DTOs, preserving SQL visibility predicates, comment masking, ordering and collection limits. Owner entity retrieval performs one additional read by those IDs and UserId; tracking remains entirely local. GetOwnedById keeps its deliberate ability to remove a favorite after the source becomes inaccessible. Shared IUnitOfWork preserves atomic writes and the central User/source Cascade FKs remain unchanged. Real-DI tests verify tracking identity, edits, visibility revocation, removal and missing-source rollback.

## Dietologist runtime context

Dietologist owns a six-entity runtime context. Joined DTO reads remain in host
composition; repositories use owner sets and the invitation repository retains
owner-local Entry reconciliation. The central model, ten User FKs, migration
history and purge orchestration remain unchanged.

The shared save coordinator always runs central interception first, even with a
clean central tracker. The Dietologist-owned collaboration interceptor reads its
registered owner tracker and stages central AuditEntry rows in the same transaction.
Its pending generated entries are reused across failed attempts to avoid duplicate
audit on retry. Non-relational coordinated audit fails before persistence because
it cannot guarantee atomic storage across contexts. PostgreSQL tests cover mixed
saves, owner-only audit, FK rollback, retry and no-op saves; existing synchronous
and asynchronous audit rule tests remain authoritative.

Meals now uses MealsDbContext for its five runtime entity types. Recognition creation flushes the owner tracker through the shared unit of work and captures xmin; receipt creation and undo remain in the same user-serialized transaction. Owner reads rejoin the live transaction after intermediate saves. Composed foreign snapshots, migrations, and the ordered purge bridge keep their existing ownership.

Products now uses ProductsDbContext for runtime product persistence and related-data snapshots. Shared serializable mutation transactions retain product row locks, xmin concurrency, retry/reset behavior and composed usage counting. Owner queries rejoin the current shared transaction after intermediate unit-of-work saves; migrations and ordered purge remain central.

Recipes now owns a three-entity RecipesDbContext for Recipe, RecipeStep and RecipeIngredient. Existing mappings and central migrations remain unchanged. Its repository joins the live shared transaction before owner operations; the existing Serializable mutation runner and shared UOW preserve retry/reset, row locking, intermediate flush and atomic rollback. Cross-module usage and overview SQL remain in read composition. SharedRecipesContextIntegrationTests covers graph/snapshot reads, atomic save, rollback and independent row locking.

Identity now owns IdentityDbContext: email templates and owned revisions, refresh sessions, login events, consumed Telegram assertions, login tickets and operation journals. Owner repositories use narrow DbSets and Telegram stores use the typed context, synchronizing the caller transaction before SQL. Shared UOW still commits Users and Identity atomically; central migrations and composed login reporting remain unchanged. The primary HTTP test fixture uses the existing PostgreSQL fixture because registration now writes multiple contexts; the fake-auth HTTP fixture uses the same PostgreSQL base because its admin scenarios also register users.

### Ai usage-read preparation

Ai usage reporting now uses the owner IAiUsageQuery port implemented by ReadModel.Composition. The Users display join and all SQL aggregates preserve user filtering and half-open date ranges. AiUsageRepository stages owned usage writes only. Ai still uses the shared runtime context; quota and recognition-job transactions are unchanged pending their separate context extraction.

### Ai runtime context

AiDbContext applies the five existing root mappings and owned prompt revisions. Scoped usage and template repositories participate in the common UnitOfWork and synchronize its live transaction. AiQuotaRepository and FoodRecognitionJobStore retain fresh contexts and independent short transactions from copied provider options; their retry strategy, locks, idempotency and commit order remain unchanged. Prompt caching resolves the owner context in its own scope. Central migration mappings and the coordinated user-purge bridge remain unchanged; no schema migration is needed.

### Users measurement-read preparation

Current weight and waist reads moved from Users Infrastructure to host ReadModel.Composition using their unchanged Users consumer ports. They retain scalar BodyMetrics projections, requested-user filtering, date/creation ordering and nullable results. This removes foreign measurement access before Users runtime-context extraction; no schema or HTTP changes are involved.

### Users runtime context

UsersDbContext completes the 29 runtime-context extractions. It maps the six owned user, role, audit and goal types; current measurements remain composed scalar reads. Adapters receive narrow owner sets and synchronize the live shared transaction before queries or role SQL. The shared coordinator uses stable save priorities: Users -100, central 0, other modules 100. This preserves new-user FK inserts even when Identity is resolved first, without coupling the coordinator to the Users implementation. Transaction binding and release exclude the central context by identity rather than array position. Domain-event discovery retains resolution order.

The owner explicitly retains TelegramIdentityConflictInterceptor to translate uniqueness failures without leaking provider details. PostgreSQL regressions cover both resolution orders, rollback after dependent failure, role changes after an intermediate save inside an outer transaction, and Telegram conflict translation. Central migrations, composed reads, cross-module FK configuration and the ordered user-cleanup bridge remain; no schema or HTTP change is intended.
