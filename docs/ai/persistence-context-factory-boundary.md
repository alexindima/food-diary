# Owner context factory boundary

All 29 owner context registrations consume IModuleContextFactory from the infrastructure-only FoodDiary.Persistence.Abstractions assembly. The contract references EF Core but no project or provider. FoodDiaryDbContext implements the contract through its unchanged CreateModuleContext method; AddInfrastructure resolves the interface to the same scoped central context.

Factory calls no longer resolve the concrete central context in any module registration. Hydration still references central Infrastructure for its reviewed purge bridge; this change does not claim complete module isolation. Application and Domain must not acquire references to the EF contract assembly. No mappings, migrations, HTTP contracts or database schema change.

The factory preserves provider options, shared connection, command interceptors, participant tracking and save order. Scoped DI owns the returned module contexts. Unit-of-work, transaction, reset and outbox replay behavior remain in their existing implementations.

Verification covers factory identity, same-scope context reuse, separate-scope isolation, shared connection and participant registration; existing PostgreSQL Hydration tests cover joint save, rollback, cancellation and retry. Architecture tests enforce the exact project graph and the factory boundary for all module registrations.

Next: extract transaction/reset/connection coordination independently before revisiting purge and audit bridges. Ai independent quota/job options remain separate from scoped owner contexts; Users retains its interceptor and saveOrder -100.

An all-module DI test resolves each owner context twice within a scope and in a second scope, proving shared connection/coordinator membership and scope isolation for all 29 owners. Standalone test containers register their supplied central context as IModuleContextFactory explicitly.

DailyAdvices, ContentReports, Favorites, Exercises, RecipeCommunity, Fasting, Usda, Marketing and Lessons no longer reference FoodDiary.Infrastructure, directly or transitively. ModuleContextFactoryBoundaryTests protects this closed dependency graph. Their contexts still join the host-provided scoped factory and unit of work; central migrations and composed relationships remain unchanged. Legacy FoodDiary.Infrastructure namespaces inside owner assemblies do not imply a central assembly dependency.

WeeklyGoals now also has no direct or transitive central Infrastructure reference. It consumes IModuleTransactionCoordinator for the live transaction and coordinated execution. The central implementation preserves the existing unit of work, default isolation, clean-entry/retry/reset policy and Result rollback semantics; WeeklyGoals keeps the unchanged advisory-lock SQL and user/week key. Neither factory nor transaction contract owns independent saves.
