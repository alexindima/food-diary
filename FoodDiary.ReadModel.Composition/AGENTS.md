# Read model composition

Implements cross-module read ports with SQL projections, per ADR 0038. API,
JobManager and Initializer explicitly register AddReadModelComposition. Modules
must never reference this assembly. Existing implementation namespaces are retained
as a deliberate compatibility exception; physical and assembly ownership is here.

Only no-tracking scalar/immutable DTO reads are permitted. No aggregate-returning
API, writes, tracked queries, SaveChanges, transactions, raw SQL or external clients.
Preserve SQL-side joins, predicates, ordering and limits. Keep persistence access
visible in persistence-capabilities.json and architecture capability scanning.
Do not move owner writes or application orchestration into this project.

Verify host DI, the architecture suite and affected PostgreSQL query tests.
