# Shared persistence boundaries

Current implementation uses owner runtime contexts registered through
`IModuleContextFactory`. `PersistenceSession` in
`Shared/FoodDiary.Persistence.Runtime` coordinates their connection, transaction,
ordered saves, late enlistment and reset. Module Infrastructure projects do not
require the complete `FoodDiary.Infrastructure` implementation.

## Remaining shared responsibilities

- `FoodDiary.Infrastructure` owns the complete EF migration model, historical
  migrations, snapshot and typed cross-module FK composers.
- `FoodDiary.ReadModel.Composition` implements cross-module SQL projections through
  `ICompositionReadContext`. The facade exposes no-tracking queries, not database
  authorization. FD0018 additionally rejects writes, tracking, raw SQL and ADO
  capabilities during compilation.
- `Shared/FoodDiary.Persistence.Runtime` owns the shared audit/email/replay model,
  coordinated unit of work, transaction runners and replay orchestration.
- `Shared/FoodDiary.Outbox.Infrastructure` owns generic claiming, retry policy and
  processing. Stream records and adapters remain with Email or their owner module.

## Owner integrations

Users cleanup uses `UsersDbContext`, ordered `IUserDataPurgeParticipant` providers,
`IModuleTransactionCoordinator` and `IModuleScopeGuard`. It does not acquire the
complete FoodDiaryDbContext. Participants delete their own records within the
owner-purge transaction; rollback clears all coordinated trackers.

Dietologist auditing uses EF's `ISaveChangesInterceptor` and
`IModuleChangeTrackerSource` to inspect Dietologist entries. It stages internal
AuditEntry rows through its explicit friend-assembly exception so auditing remains
atomic. It no longer requires FoodDiaryDbContext. This reviewed EF capability is
not a general foreign-write authorization.

Owner registration and SQL adapters may still need live transaction handles.
`persistence-capabilities.json` and `persistence-technical-sources.txt` inventory
these exceptions; FD0015 protects entity ownership and FD0016 requires reviewed
fingerprints for EF, coordinator and direct ADO technical operations. SQL semantics
still require source review and PostgreSQL tests.

## Atomic top-level commands

Ordinary `ITransactionalCommand` saves after its handler; it does not begin a
transaction before that handler. `IAtomicCommand` explicitly requests handler-plus-
save atomicity through the database-free `IAtomicCommandExecutor`. Retries repeat
the complete callback and discard failed-attempt tracking and post-commit actions.
External side effects and nested top-level transactions are prohibited.

Meals Create/Repeat use this opt-in so evaluation outbox SQL cannot commit ahead of
the meal. Recognition creation retains its own serialized transaction and invokes
the same local handler directly. Post-commit callbacks run only after successful
commit. Direct Gamification enqueue consumers retain their existing ambient-
transaction/autocommit behavior.

See ADR 0048 and the atomic command PostgreSQL integration scenarios. The shared
schema and cross-module foreign keys remain intentional monolith coupling; separate
assemblies and contexts do not imply separately deployable or independently
committing services.
