# Products ownership inventory

Current ownership follows ADRs 0031, 0038 and 0040. Code, scoped guides and architecture tests are authoritative; Git retains earlier extraction history.

| Responsibility | Current owner and boundary |
| --- | --- |
| Commands, queries, validation and application mapping | `Modules/Products/Application`; preserve the `FoodDiary.Application.Products` assembly identity and existing use-case behavior. |
| Aggregate repository, mutation transaction and usage-query ports | `Modules/Products/Application.Abstractions`; owner orchestration consumes these ports without acquiring foreign aggregate writes. |
| Consumer projections, lookup and semantic USDA linking contracts | `Modules/Products/Contracts`; no foreign aggregate-returning API. Product errors also belong here. |
| Product aggregate and invariants | `Modules/Products/Domain`; foreign links use scalar IDs. Product IDs, product type and measurement unit belong to `Modules/Products/Domain.Contracts`. |
| Food-quality formula | `Modules/Products/FoodQuality`; consumers reference this narrow owner project directly. |
| Runtime product persistence | `Modules/Products/Infrastructure/Persistence/ProductsDbContext.cs`; one mapped Product entity, using the existing `ProductsPersistenceModelRegistration`. |
| Product writes, row locking and cache | `ProductRepository` uses ProductsDbContext; `CachedProductRepository` preserves cache lifetime, access rechecks and mutation invalidation. |
| Related-data product snapshots | Owner `ProductSnapshotReadService` uses the owner DbSet and returns immutable scalar snapshots for Meals and Recipes; empty input performs no query. |
| Overview and cross-module usage counts | `FoodDiary.ReadModel.Composition/Products`; host registration supplies `IProductOverviewReadService` and `IProductUsageQuery`. Only no-tracking scalar/DTO reads are allowed. |
| Shared transaction coordination | Products' `EfProductMutationTransactionRunner` retains the central Serializable transaction and retry/reset boundary; shared IUnitOfWork commits central and owner changes atomically. |
| Schema, foreign keys and migrations | Central FoodDiaryDbContext remains the complete migration/composition model. Runtime extraction does not change schema, indexes, conversions or database foreign keys. |
| Ordered user cleanup | `ProductsUserDataPurgeParticipant` retains shared-context bulk operations under the Users cleanup coordinator. |

## Persistence invariants

Owner queries synchronize with the live shared transaction, including after an intermediate unit-of-work save. Product update/delete keeps the existing owner/public predicates and parameterized `FOR UPDATE` SQL; xmin remains the concurrency token. Composed usage counting uses the same scoped shared context and therefore participates in the caller transaction.

Overview queries preserve escaped search, SQL pagination, ordering, correlated Meals/Recipes usage counts and masking of another owner's private comment. ProductRepository delegates usage counting and no longer directly reads Meals or Recipes tables.

## Verification and delivery

`SharedProductsContextIntegrationTests` covers shared saves, xmin, reads after an intermediate flush, rollback/reset and independent-connection row locking. `ProductUsageCompositionIntegrationTests` covers visibility and uncommitted usage counts. Products and Meals PostgreSQL suites protect related-data reads; ProductPostgresApiFlowTests covers API persistence and the composite favorites/meal/recent flow. That multi-context flow requires PostgreSQL rather than InMemory.

Run the solution build, architecture and pending-model checks, module tests, affected consumers, relevant HTTP/Swagger checks and unfiltered central PostgreSQL coverage. No collectors or live provider calls are needed. Hosts must be rebuilt together; no schema/configuration migration or deployment is part of this refactor.
