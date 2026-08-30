# USDA Wiki-first extraction report

## Ownership evidence

USDA owns its application queries/commands/services, provider and repository ports, shared read models, EF configurations, repository adapter, and focused application tests. Central Domain retains USDA entities because `Product` has a direct EF navigation to `UsdaFood`; central Infrastructure retains `FoodDiaryDbContext`, migrations, and snapshot. `FoodDiary.Integrations` retains the typed `HttpClient`, USDA request/response DTOs, normalization/mapping, options/API-key boundary, 15-second timeout, cancellation flow, and in-memory detail cache.

Products consumes USDA suggestions and link workflows. Meals supplies activity projections for daily micronutrients. Recipes has no direct USDA dependency; its seam remains through Products/meal composition. OpenFoodFacts is a parallel external-catalog module with no direct USDA reference.

| Responsibility | Owner / retained seam |
| --- | --- |
| Search, food details, daily micronutrients, link/unlink | `Modules/Usda/Application` (3 query slices, 2 command slices, 3 read services, health-score mapping) |
| Provider/repository/daily-read ports and error factory | `Modules/Usda/Application/Abstractions`; central `Errors.Usda` remains a compatibility facade |
| Eleven read/result models | `Modules/Usda/Contracts`; CLR namespaces preserved |
| USDA foods, nutrients, portions, food-nutrients, DRI | central `FoodDiary.Domain/Entities/Usda`; no owned IDs/events introduced |
| Four USDA EF configurations | `Modules/Usda/Infrastructure/Model`; Product navigation and Nutrition DRI configuration remain central |
| Read repository | `Modules/Usda/Infrastructure/Persistence`; central DbContext/unit of work retained |
| HTTP adapter, options, DTO mapping, detail cache | shared Integrations, unchanged |
| API transport, import/seed operations, jobs | presentation/Initializer/JobManager remain composition or operational consumers |
| Tests | 27 USDA application tests moved; provider, HTTP and shared PostgreSQL seam tests remain central |

## Topology and failure semantics

`IUsdaFoodSearchService` is registered as a typed client. `UsdaApiOptions` validates an absolute HTTPS base URL and binds the API key from configuration. Requests preserve their existing 15-second timeout and caller cancellation. No retry policy or explicit provider rate limiter exists; API host request limiting remains unchanged. Detail lookups retain positive/negative TTL caching, in-flight coalescing, and non-cacheable provider-failure behavior. There is no stale-value fallback. Search/provider failures continue to log and map to empty/null results; no provider requests or result semantics changed.

The default base URL is `https://api.nal.usda.gov/fdc/v1`. Request logging is disabled for the typed client; the API key remains only in configured provider requests. Detail cache bounds remain 2048 entries, eight distinct in-flight lookups, 30-minute positive and five-minute negative TTLs, and a 20-second shared-operation deadline. Caller cancellation stops that caller's wait without cancelling a shared lookup. A 404 is negative-cacheable; malformed/transient failures are not. Expired entries are removed, not served stale.

## Wiki utility and defects

- Useful: `research` found the current USDA flow and OpenFoodFacts precedents; `privacy` found `UsdaApiOptions.ApiKey`; `journeys` identified FD-CATALOG.
- False positives: `ownership` ranked unrelated integration and infrastructure tests as high-confidence USDA owners.
- Query limitation, not a generator defect: a long natural-language topology query returned no records; the scoped `topology -Query Usda` correctly found the typed client. `brief` and `test-plan` without explicit scope returned empty results and were rerun with planned paths.
- Reproducible false positive: scoped topology marks the USDA registration as `retry-or-resilience`, but the retry handler in the same registration window belongs only to OpenFoodFacts. It also labels provider source signals as URI/IP policy candidates, not verified connection-time controls. Treat these as leads, not established behavior.
- Reproducible limitation: TypeScript prerequisites were unavailable, forcing the read-only JSON baseline. `trace` then attempted a graph build and failed instead of completing a backend-only trace. No module-specific generator special-case was added.
- Explicit `trace -Query SearchUsdaFoodsQuery -CompiledIndexSource Json` completed but combined deleted donor paths with current module paths (including duplicate implementation/test entries). Current source verification rejected those stale donor anchors. This is a general mixed-baseline relocation defect, not USDA-specific.
- Governed workspace limitation: the initial planned-path argument was passed as one comma-separated string, leaving its evidence packet with no changed paths. Later scoped research and real checks succeeded, but `delivery-validate` still reports missing change links (12) and evidence lineage issues (15), despite 12/12 accepted criteria and no unresolved checks/reviews. This is an intake invocation error, not evidence that the tests failed; the governed delivery gate is not claimed green. Default `delivery-critique` also requires the unavailable SQLite projection.

## Verification

- Force-evaluate solution restore: passed.
- Full solution build under `.artifacts/usda-solution`: passed, zero warnings/errors.
- USDA application: 27/27; provider/options: 132/132; Products seams: 13/13; USDA presentation: 11/11.
- PostgreSQL USDA repository: 1/1; architecture: 742/742.
- Full API integration suite: 181/181, no skips. NuGet vulnerability audit: no vulnerable packages reported.
- Wiki update and seven-stage verify passed; architecture-health reports 500 production / 261 test project edges and no enforced drift.
- EF pending-model check: no model changes. Tools 10.0.10 reported a non-failing version warning against runtime 10.0.11.
- Provider implementations/options, HTTP feature source, central Domain and historical migrations have no content changes.
