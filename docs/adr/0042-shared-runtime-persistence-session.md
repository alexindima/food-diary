# 0042: Shared runtime persistence session

Status: Accepted

## Decision

Separate the shared runtime model from the complete database model inside the
existing Infrastructure assembly. This continues ADR 0040 without relocating
migration classes, changing migration IDs, or changing the database schema.

`SharedRuntimeDbContext` maps only audit entries, email outbox messages and replay
audit records through their existing Shared PersistenceModel registrations.
`SharedPersistenceDbContext` is its common abstract base and exposes the shared
record sets and coordinated-save guard. The complete `FoodDiaryDbContext` derives
from that base but applies all owner models and cross-module relationships. It
remains the context for migrations, initialization and composed SQL reads.

`PersistenceSession` owns the scoped participant registry, context creation and
save ordering. Runtime unit-of-work and transaction services depend on the narrow
shared base; the multi-context save algorithm takes the session and generic
DbContext participants. Modules continue to request IModuleContextFactory. The
base's factory method delegates to the session for directly constructed contexts.
Neither the session nor the save algorithm knows the complete model type.

The complete context is resolved lazily as a participant on the runtime connection.
The shared persistence runtime does not require that model; a use case which also
resolves a composed reader can still instantiate it. Composed readers retain their SQL projections and full-context constructor
contracts. Initializer retains its full-context migration and seed operations.
The API database health check uses the narrow runtime context.

## Transaction and lifetime invariants

DI owns context disposal. The runtime owns the connection; participant contexts
borrow it. Existing provider options, command interceptors and owner save order
remain in force, with the complete context ordered before ordinary owner writes
when initialization or a test explicitly tracks data through it.

Top-level coordinator operations enlist already-created participants, and the
factory enlists participants created inside that managed operation. Intermediate
saves restore prior enlistment while the session owns that participation scope.
The transaction wrapper detaches participants on exit. Composed queries therefore
see intermediate writes in the same transaction whether resolved early or late.
Failed attempts still reset all tracking and pending post-commit actions according
to the existing command/item/session policy. Provider callbacks in the session
coordinator are not replayed.

Other explicit caller transactions, including replay coordination, retain the
previous save-time enlist/detach policy. They do not inherit the coordinator's
participation lifetime or retain a completed transaction on owner contexts.

Generic email processing retains its clean-scope guard. Domain events and
Dietologist audit interception still run on coordinated runtime saves. Shared and
owner writes commit together; a later failure must roll back both.

## Compatibility and verification

The full context CLR identity, migration assembly, historical migrations, snapshot,
table names and relationships remain unchanged. A separate migrations assembly is
not introduced: the full model is also required by composed reads, and moving
historical metadata would add deployment work without further runtime isolation.
Deploy or revert the backend assemblies together. No new configuration or data
migration is required.

When invoking EF tooling directly, select `--context FoodDiaryDbContext` explicitly;
the assembly now also contains the narrow runtime context. Initializer already
selects the complete context by type and retains its existing command interface.

Existing tests which explicitly supply a complete context as the transaction root
also register it as SharedPersistenceDbContext. New production-registration tests
must exercise the actual narrow root, not that compatibility fixture arrangement.
Coverage includes the three-record model, lazy full-model resolution, joint commit,
early/late composed reads after an intermediate save, outer rollback with provider
retries enabled, and an unchanged migration model. Existing PostgreSQL and API
checks continue to cover owner operations, replay, audit and initialization.
