# Owner context factory boundary

Hydration registration consumes IModuleContextFactory from the infrastructure-only FoodDiary.Persistence.Abstractions assembly. The contract references EF Core but no project or provider. FoodDiaryDbContext implements the contract through its unchanged CreateModuleContext method; AddInfrastructure resolves the interface to the same scoped central context.

This pilot removes concrete central context access from Hydration ModuleRegistration. Hydration still references central Infrastructure for its reviewed purge bridge; this change does not claim complete module isolation. Application and Domain must not acquire references to the EF contract assembly. No mappings, migrations, HTTP contracts or database schema change.

The factory preserves provider options, shared connection, command interceptors, participant tracking and save order. Scoped DI owns the returned Hydration context. Unit-of-work, transaction, reset and outbox replay behavior remain in their existing implementations.

Verification covers factory identity, same-scope context reuse, separate-scope isolation, shared connection and participant registration; existing PostgreSQL Hydration tests cover joint save, rollback, cancellation and retry. Architecture tests enforce the exact project graph and the Hydration registration boundary.

Next: migrate the remaining registrations, then extract transaction/reset/connection coordination independently before revisiting purge and audit bridges.
