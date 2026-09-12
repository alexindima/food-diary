# Products Infrastructure

ProductSnapshotReadService owns the no-tracking scalar batch read consumed by
Meals and Recipes. Empty input avoids SQL; duplicate IDs collapse; missing IDs
are omitted. Preserve the existing visibility semantics of related-data hydration.

Own Product repositories, cache wrapper, read adapters and complete DI. Depend on central Infrastructure for shared context and transaction-attempt reset. Do not add SaveChanges outside the existing transaction runner. Preserve scoped aliases and SQL. Central Infrastructure must never reference this adapter project.
