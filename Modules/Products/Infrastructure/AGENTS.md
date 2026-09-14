# Products Infrastructure

ProductSnapshotReadService owns the no-tracking scalar batch read consumed by
Meals and Recipes. Empty input avoids SQL; duplicate IDs collapse; missing IDs
are omitted. Preserve the existing visibility semantics of related-data hydration.

Own Product repositories, cache wrapper, read adapters and complete DI. Delegate Serializable mutations and live transaction access to IModuleTransactionCoordinator. Repositories stage changes; the host coordinator owns saving, retries and reset. Central Infrastructure is still referenced for the purge bridge. Preserve scoped aliases and SQL. Central Infrastructure must never reference this adapter project.

ProductOverviewReadService lives in host read-model composition and is registered through AddReadModelComposition. Keep owner/public visibility, private comment masking, SQL paging and correlated Meals/Recipes usage counts there; owner repositories and transaction locks remain in Products.

ProductRepository delegates usage counting to IProductUsageQuery. Host composition executes that scalar query using the same scoped shared context and the caller mutation transaction. Keep serializable isolation and row-lock order; do not replace this query with a cache or an independent connection.

ProductsDbContext owns the Product runtime entity. ProductRepository uses the owner context; ProductSnapshotReadService receives its narrow DbSet. Synchronize owner reads with the current shared transaction, including after intermediate saves. Shared IUnitOfWork coordinates commits; the serializable mutation runner uses the coordinator contract while the ordered purge bridge retains the shared context. Central migrations and schema remain unchanged.
