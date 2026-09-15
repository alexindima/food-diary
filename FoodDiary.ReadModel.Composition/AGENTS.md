# Read model composition

ADR 0043 supersedes the concrete constructor contracts below. Readers request
ICompositionReadContext, whose explicit IQueryable properties use AsNoTracking on
the same scoped full context. Only ReadModelCompositionRegistration may name
FoodDiaryDbContext to supply the DI alias for explicit-context fixtures. Do not
cast the facade to DbContext or use EF bulk-write/tracking extensions.

Implements cross-module read ports with SQL projections, per ADR 0038. API,
JobManager and Initializer explicitly register AddReadModelComposition. Modules
must never reference this assembly. Existing implementation namespaces are retained
as a deliberate compatibility exception; physical and assembly ownership is here.

ADR 0042 separates the shared runtime root from FoodDiaryDbContext. Readers still
receive the same scoped complete-model context, resolved lazily on the session's
connection. The shared coordinator enlists it in caller transactions, including
late resolution and reads after intermediate saves. Do not create independent
connections or change reader SQL to compensate for the runtime split.

Only no-tracking scalar/immutable DTO reads are permitted. No aggregate-returning
API, writes, tracked queries, SaveChanges, transactions, raw SQL or external clients.
Preserve SQL-side joins, predicates, ordering and limits. Keep persistence access
visible in persistence-capabilities.json and architecture capability scanning.
Do not move owner writes or application orchestration into this project.

Verify host DI, the architecture suite and affected PostgreSQL query tests.

ComposedReadIsolationTests additionally rejects mapped EF entities in public
reader results, including nested DTO members. Keep cross-module EF relationships
navigation-free by module ownership, while allowing owner-internal navigations
across that module's Domain/PersistenceModel assemblies. See
docs/architecture/COMPOSED_READ_BOUNDARIES.md for the remaining boundary assessment.

MealPlanning composition implements IMealPlanCompositionReader. Return only the
immutable detail model and recipe snapshot dictionary. Preserve inner joins,
serving fallback and ingredient batching; aggregate mutation/attachment stays in
the owner repository. Hosts register the adapter through AddReadModelComposition.

ContentReports owns a single-entity runtime context and report writes. The host
composition implements its existing read-model and target-read ports, preserving
visibility predicates, SQL paging and bounded title/comment excerpts. No module
references the composition implementation; central migrations remain (ADR 0040).

Favorites composition owns product/recipe source visibility predicates and joined DTO projections. Return authorized scalar favorite IDs for owner materialization, never favorite aggregates. Preserve user/public filtering, private product comment masking, recipe ingredient count and DTO ordering/limits. Keep IDs filtered by the requested favorite or source IDs for point/batch lookups.

Dietologist attention metrics implement the owner IAttentionSignalMetricsReadService port. Preserve requested-client filtering, inclusive date bounds, all-time last meal, manual-calorie fallback, daily grouping and ordered weight points. Relationship authorization and permission-dependent signal decisions remain in Dietologist Application. Cross-module reader tests live in central Infrastructure.Tests and the existing PostgreSQL Dietologist integration suite.

Dietologist composition directly registers IDietologistInvitationReadModelRepository, IRecommendationReadModelRepository and IRecommendationCommentReadModelRepository. Preserve client inner joins, optional dietologist left joins, status/owner predicates, limits and ordering. These ports return immutable DTOs only; combined owner repositories delegate their compatibility read-model methods to these ports.

Meals source lookup implements IMealSourceSnapshotQuery for IDs derived from an already authorized meal graph. Return image URLs and immutable recipe name/image/serving/nutrition snapshots only. Empty ID collections perform no query. Preserve existing ID filtering and nullable totals; owner MealRepository retains snapshot precedence and legacy serving rules.

Products overview implements IProductOverviewReadService with unchanged SQL paging, search escaping, owner/public visibility, private comment masking and correlated meal/recipe usage counts. Preserve food-quality calculation on immutable projected rows and register the port only in host composition.

ProductUsageQuery implements the owner IProductUsageQuery port. Preserve owner/public filtering, zero for missing or inaccessible products, and correlated Meals/Recipes counts. Use the same scoped shared context so mutation-time reads see uncommitted writes under the caller serializable transaction; never introduce a new transaction or context.

RecipeUsageQuery implements IRecipeUsageQuery. Preserve owner/public filtering, zero for inaccessible or missing recipes, and the sum of meal items and nested recipe usages. Query the same scoped shared context without tracking or a new transaction.

Identity UserLoginEventQuery implements the owner query port. Preserve the Users inner join, escaped ILIKE search, all filters before count/paging, descending login time with ID tie-break, half-open paged date range and inclusive summary date range. Return immutable DTOs without tracking; use the caller scoped context and transaction.

AiUsageQuery implements the owner IAiUsageQuery port. Preserve SQL-side usage totals, day/operation/model breakdowns, Users inner join for display, requested-user filtering and [fromUtc, toUtc) bounds. Only immutable no-tracking reads live here; usage writes remain in Ai.

Users current weight/waist providers implement the existing consumer ports with scalar BodyMetrics reads. Preserve user filtering, descending Date then CreatedOnUtc ordering, null for no measurements, cancellation and no tracking. Users goal mutation remains in its application layer.

Dashboard body composition implements IDashboardBodyReadService. Preserve two latest measurements, Date/CreatedOnUtc ordering, UTC date normalization for weight/waist, inclusive original instant bounds for hydration, trend buckets and section flags. Register the scoped concrete/interface alias here; AddDashboardReadServices must not remove it. Dashboard Infrastructure has no EF Core or central Infrastructure dependency.
