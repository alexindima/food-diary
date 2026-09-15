# Remaining composed-read and EF boundaries

## Assessment

The complete EF model deliberately owns cross-module foreign keys in
FoodDiary.Infrastructure/Persistence/Composition. These relationships maintain
database integrity; removing them merely to remove a C# reference would change
delete behavior and permit orphaned data. Keep migration ownership and those
constraints together.

Cross-module relationships do not need CLR navigation properties. Owner Domain
and PersistenceModel assemblies can differ while belonging to the same module:
for example, Notifications outbox records may navigate to their own Notification.
The boundary is module ownership, not assembly equality.

FoodDiary.ReadModel.Composition is an explicit integration layer. Its references
to module Domain assemblies describe EF query inputs through ICompositionReadContext.
They do not authorize returning aggregates or changing them. Removing a direct
reference while retaining the same types through transitive dependencies would
hide the coupling instead of reducing it.

Current composed readers return scalar IDs, summaries and DTOs. Keep SQL-side
filtering, joins, ordering and paging, and preserve the shared connection and
transaction. A second mapped read model would require duplicated mappings and
additional compatibility tests; it is not justified solely by the reference count.

## Guardrails

ComposedReadIsolationTests checks the actual EF model and public reader result
types, including nested DTO properties, fields, generic arguments and arrays:

- Foreign keys between different owners must expose no object navigation.
- Public composed-reader results must contain no mapped EF entity.

These complement the existing dependency matrix, read-capability scanner,
ICompositionReadContext no-tracking checks and model-snapshot compatibility test.
They do not replace PostgreSQL tests for query translation, transaction visibility
and rollback, and they do not make IQueryable a security sandbox.

## Verification

Run ComposedReadIsolationTests in the normal Infrastructure.Tests project after a
coordinated solution build. A check against previously built assemblies proves
only that build's model and public API, not subsequent source changes. PostgreSQL
scenarios must execute against an available database; skipped tests are not proof
of transaction or SQL behavior.

See ADRs 0038, 0042, 0043 and 0044. Further changes should address an observed leak,
query limitation or operational requirement, rather than eliminate intentional
composition dependencies indiscriminately.

FD0018 now enforces the read-only execution surface at compile time, including
EF bulk writes, tracking, raw SQL, explicit context casts and ADO method groups.
The no-tracking facade and compiler checks complement each other; neither is a
database permission boundary. Recipe scalar nutrition is computed by the owner
Domain/Nutrition/RecipeNutritionPolicy, shared by command and projection adapters.
