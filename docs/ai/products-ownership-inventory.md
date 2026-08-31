# Products ownership inventory

Source audit base: `4c1b1c2c31a0886cf7e01d8bba7ec00231e6b149`.

| Responsibility | Physical owner and compatibility boundary |
| --- | --- |
| 52 application source files: Create/Update/Delete/Duplicate, GetById/GetProducts/Overview/Recent/Suggestions, validation, mappings, image resolution and USDA linking | `Modules/Products/Application`; preserve `FoodDiary.Application.Products` assembly name and CLR namespaces |
| Read/write/composite aggregate repositories, mutation transaction runner and ProductErrors | `Modules/Products/Application/Abstractions`; no outward dependency on central Abstractions |
| IProductLookupService, IProductOverviewReadService, IProductUsdaLinkService, ProductOverviewReadItem, ProductQueryFilters | `Modules/Products/Contracts`; existing projection and semantic mutation API, preserving CLR namespaces. Meals, Recipes, Favorites and MealPlanning consume lookup projections; USDA consumes owner-side linking capability. No foreign aggregate is exported by these contracts. |
| ProductRepository, CachedProductRepository, ProductOverviewReadService, EfProductMutationTransactionRunner, ProductLookupService | `Modules/Products/Infrastructure`; shared context, five-minute cache/access recheck, scoped aliases, row locks, SQL, transaction/save boundaries and cancellation remain unchanged |
| ProductConfiguration | `Modules/Products/Infrastructure/Model`; explicit registration from shared context, unchanged xmin, indexes, conversions, FKs, navigation access and delete behavior |
| Product, ProductId, enums/value objects and related domain behavior | Central Domain seam: User.Products and Product.User; Product.RecipeIngredients and RecipeIngredient.Product; Product.MealItems and MealItem.Product plus ApplyProductSnapshot(Product) form public bidirectional CLR graphs. Product.UsdaFood also preserves the USDA compatibility seam. An independent Domain assembly would create a cycle or require changes to foreign aggregates. |
| RecipeCompositionTransactionLock | Central Infrastructure internal seam, shared with Recipes mutation. Products gets explicit friend access; lock keys and lifetime are not duplicated or changed. |
| FoodDiaryDbContext, migrations/snapshot, User cleanup, foreign projections, HTTP/auth and host configuration | Existing central/consumer owners; no database, route, payload or authorization change |
| Provider HTTP, credentials, cache policy and cleanup/outbox | Integrations and catalog owners retain HTTP/options. Products retains suggestion orchestration only. Images retains media deletion/outbox and RecentItems retains usage recording/ordering. |
| Tests | Products-only application tests, ProductInvariantTests and three repository PostgreSQL cases move to three separate nested module test projects; mixed Favorites overview composition, mixed central domain, shared PostgreSQL fixture, HTTP, host and architecture suites remain with their existing owners. The module Domain.Tests project references the central Domain assembly without requiring a production Domain project. |

Compatibility means coordinated rebuilding of consumers and executable hosts. CLR namespaces and the legacy application assembly identity are retained; relocation of ports/adapters does not promise compatibility with old precompiled binaries.

Required verification is tracked separately from navigation: full solution restore/build, locked restore, owned and donor/consumer suites, full architecture, relevant HTTP/Swagger, EF pending-model comparison and one unfiltered real-PostgreSQL infrastructure suite. No collectors or production access.

## Source review and rollout

`ProductOverviewReadService` retains AsNoTracking, owner/public filtering, escaped
ILIKE search, normalized pagination, descending creation order, one count query
and one page projection. Usage counts remain projected rather than materializing
navigation collections, and another owner's private Comment is redacted.
ProductConfiguration retains owner/creation and visibility/creation indexes,
four trigram indexes, xmin concurrency, optional image/USDA SetNull relationships
and field access for inverse usage collections. Source equality is evidence of
preservation; PostgreSQL execution and EF comparison are separate required checks.

`CachedProductRepository` retains its five-minute cache and access check on every
cache hit; update reads bypass the cache. Mutations evict the same keys.
`EfProductMutationTransactionRunner` retains execution-strategy retries, the shared
Recipe composition advisory lock, one transaction lifetime and conditional unit
of work saving. Repository methods gain no SaveChanges calls. No new background
work, provider requests, credentials, configuration, media cleanup or outbox
ownership is introduced by registration changes.

API and Initializer compose the full Products facade; JobManager composes only
Products persistence and retains its existing handler set. Deployments must use
coordinated host builds containing the new project outputs and corresponding
Docker COPY paths. Do not mix old compiled port/adapter binaries with new hosts.
No schema/configuration migration or provider ordering is needed. Rollback means
rebuilding and redeploying the previous complete host revision; there is no data
rollback step. After deployment, verify authenticated product reads/mutations,
private visibility, suggestions and recipe/meal composition through existing
smoke journeys and normal application error/latency signals. Deployment itself
is outside this task and was not performed.

The physical boundary implements the existing ownership/abstraction ADRs; it does
not introduce a new logical module dependency or service boundary. The narrow
central Domain and advisory-lock seams above remain explicit limitations rather
than claims of independent aggregate or binary deployment.
