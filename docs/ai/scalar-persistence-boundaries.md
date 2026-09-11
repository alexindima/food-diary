# Scalar persistence model boundaries

The accepted AI pilot now extends to Hydration, RecentItems, Exercises and WeeklyGoals. Each of the four models replaces Users.Domain with the narrow Users.Domain.Contracts assembly for UserId. Owned entities, scalar conversions, indexes and constraints remain in their original PersistenceModel projects.

Each module has a typed relationship composer in FoodDiary.Infrastructure/Persistence/Composition, invoked after all owned models in FoodDiaryDbContext.OnModelCreating. HydrationEntry, RecentItem, ExerciseEntry and WeeklyGoal retain their UserId foreign key and Cascade deletion. Central Infrastructure already references all four owning Domain assemblies and Users.Domain; no new composition dependency or public API is needed.

ScalarPersistenceBoundaryTests protects compiled assembly references for all five models, including AI. The exact dependency matrix additionally rejects unused direct foreign Domain references. ModuleAggregateIsolationTests protects migration equivalence and navigation isolation. Existing Hydration PostgreSQL tests cover User cascade and caller transaction ownership; RecentItems covers concurrent upsert and retention; WeeklyGoals covers concurrent creation and reminders; the mixed tracking regression covers Exercises persistence.

No database migration, provider call, transaction change or deployment configuration is introduced. Schema equivalence is a release criterion. This strengthens compile-time boundaries while preserving the shared database and DbContext. Other modules retain their current mapping policy until deliberately migrated. Never use string entity names or reflection in production to conceal a foreign model dependency.
