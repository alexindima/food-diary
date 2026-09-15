# 0043: Persistence runtime assembly and composed read facade

Status: Accepted

## Decision

Continue ADR 0042 by moving its generic runtime into FoodDiary.Persistence.Runtime.
The new assembly registers its own shared context options and references only
shared contracts/models, technical infrastructure and the scalar Users UserId
contract required by audit. It has no dependency on the full model or module
implementations. Runtime types use namespaces matching their new assembly/folders.

FoodDiary.Infrastructure composes the runtime registration and lazily registers
FoodDiaryDbContext through IModuleContextFactory. It retains the complete model,
cross-module EF relationships, design-time factory, migration classes and snapshot.
The existing full context identity and migration assembly stay unchanged. The
existing non-persistence authentication wiring is outside this extraction.

Composed readers receive ICompositionReadContext, a finite set of IQueryable
properties with no SaveChanges, Database, ChangeTracker, generic Set or mutation
methods. FoodDiaryDbContext implements these properties explicitly using
AsNoTracking. Registration aliases the same scoped full context; it does not
create a second connection or change the SQL projections.

This facade reduces accidental write capabilities, but IQueryable is not a security
sandbox: EF extensions can still express bulk updates or opt into tracking.
Architecture capability scanning continues to forbid such use in composition and
rejects direct writable-context dependencies outside its DI registration.

## Invariants and verification

Keep the scoped connection, participant order, early/late transaction enlistment,
visibility after intermediate saves, outer rollback, execution strategy and owner
interceptor ordering from ADR 0042. Consumers continue using the same owner ports.
No HTTP contract, configuration key, data schema or migration change is required.
Deploy or revert all affected backend assemblies together; EF tooling still selects
FoodDiaryDbContext explicitly.

Dependency-closure tests reject module implementations and the complete model in
the runtime. Registration tests resolve it without full-model options, verify the
three-record runtime model, and check that composed reads expose only no-tracking
queries on the same instance. PostgreSQL tests cover transaction visibility,
rollback, atomic shared/owner writes and migration model compatibility.
