# Scalar persistence model boundaries

The accepted AI pilot now extends to Hydration, RecentItems, Exercises and WeeklyGoals. Each of the four models replaces Users.Domain with the narrow Users.Domain.Contracts assembly for UserId. Owned entities, scalar conversions, indexes and constraints remain in their original PersistenceModel projects.

Each module has a typed relationship composer in FoodDiary.Infrastructure/Persistence/Composition, invoked after all owned models in FoodDiaryDbContext.OnModelCreating. HydrationEntry, RecentItem, ExerciseEntry and WeeklyGoal retain their UserId foreign key and Cascade deletion. Central Infrastructure already references all four owning Domain assemblies and Users.Domain; no new composition dependency or public API is needed.

ScalarPersistenceBoundaryTests protects compiled assembly references for the initial five models, including AI. The exact dependency matrix additionally rejects unused direct foreign Domain references. ModuleAggregateIsolationTests protects migration equivalence and navigation isolation. Existing Hydration PostgreSQL tests cover User cascade and caller transaction ownership; RecentItems covers concurrent upsert and retention; WeeklyGoals covers concurrent creation and reminders; the mixed tracking regression covers Exercises persistence.

No database migration, provider call, transaction change or deployment configuration is introduced. Schema equivalence is a release criterion. This strengthens compile-time boundaries while preserving the shared database and DbContext. Other modules retain their current mapping policy until deliberately migrated. Never use string entity names or reflection in production to conceal a foreign model dependency.

## Images, Cycles, BodyMetrics and Wearables

The same boundary now covers ImageAsset, CycleProfile, WeightEntry, WaistEntry, WearableConnection and WearableSyncEntry. Their six UserId foreign keys retain Cascade deletion in four typed central composers. All four PersistenceModel projects reference Users.Domain.Contracts. Central Infrastructure adds an explicit Wearables.Domain reference because it now consumes those entity types directly.

ScalarPersistenceBoundaryTests covers all nine models. ModuleAggregateIsolationTests retains full relational snapshot equivalence. Focused PostgreSQL regression coverage exercises Images ownership/confirmation, Cycles aggregate deletion, BodyMetrics persistence and Wearables concurrent connection/sync creation. No migration, query, credential protection, provider call or transaction ownership changes are intended.

## ContentReports, Lessons, Gamification and Notifications

Four more models use Users.Domain.Contracts instead of Users.Domain. Four typed
central composers preserve five UserId Cascade foreign keys: ContentReport,
UserLessonProgress, UserAchievement, Notification and WebPushSubscription. Central
Infrastructure already references their Domain assemblies. Same-owner lesson and
notification-outbox relationships remain local. No domain, query, transaction,
provider, API or schema changes are introduced.

ScalarPersistenceBoundaryTests covers all thirteen isolated models. The relational
snapshot-equivalence check and focused PostgreSQL report, lesson-progress,
achievement and notification/subscription scenarios protect persistence behavior.

## Fasting and Billing

Both models replace Users.Domain with Users.Domain.Contracts. Central typed
composers preserve six UserId Cascade foreign keys: FastingPlan, FastingOccurrence,
FastingSession, FastingCheckIn, BillingSubscription and BillingPayment. Existing
central Domain references suffice. Payment-to-subscription SetNull and same-owner
Fasting relationships remain local. No migrations, provider calls, transaction
changes or financial workflow changes are introduced.

The compiled boundary guard covers fifteen models. Relational snapshot equivalence
and focused PostgreSQL Fasting and Billing repository scenarios protect the move.
