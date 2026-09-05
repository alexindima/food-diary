# Architecture improvements — 2026-09-05

This change implements the first coordinated remediation of the repository architecture review. The most important infrastructure result is an executable capability inventory and a verified narrow Hydration adapter. It does not claim complete compile-time isolation of all persistence adapters.

## Outcome by audit finding

| Finding | Implemented outcome | Remaining scope |
| --- | --- | --- |
| 1. Dormant guards | Canonical current source roots, fail on missing/empty required roots, all 34 modules in graph, Billing/Marketing coverage, frontend root/platform injectable fixtures | Continue testing guards when source ownership moves |
| 2. Dashboard fallback cycle | Removed recursive reader and Dashboard → Statistics implementation reference; independent reader mandatory; real mediator composition tests | Keep host SQL reader composition explicit |
| 3. Users.Domain coupling | Hydration stores UserId without User navigation or Users.Domain dependency; relational FK/cascade retained | Other aggregate relationships require invariant-specific extraction |
| 4. Shared Infrastructure | Reviewed 91 module adapters; entity/tracking/write/call/context capability checks; 23 technical source fingerprints; scoped Hydration DbSet | Most existing adapters still receive the shared context; no separate databases/transactions introduced |
| 5. Users lifecycle concentration | Existing full direct-User-FK cleanup/retention matrix verified; Identity active-session query now projects a minimal read model instead of token aggregates | Account/security/profile/lifecycle decomposition remains incremental; no mechanical aggregate split |
| 6. Application dependencies | One implementation edge removed; remaining 11 classified below; documentation now distinguishes public API use from implementation access | Move a stable API when there is a concrete isolation benefit; do not create contracts projects mechanically |
| 7. Broad Integrations helper | Three BCL-only HTTP/URI/telemetry helpers extracted; five provider modules use the narrow project; Docker and locks updated | Billing Domain/PersistenceModel remain transitively visible through the central context |
| 8. Initializer composition | Database, outbox-administration and bootstrap profiles; dependencies resolved in their command branch; all four replay streams verified | Update-to-latest retains broad bootstrap registration and static host references |
| 9. CycleTracking facade | Form types/defaults, pure day mapping and export workflow extracted; initial cycle request cancelled when scope is destroyed; one state owner retained | Settings/consent and day-save orchestration still reside in the facade |
| 10. Cost of project splitting | Exactly one justified transport-helper project added; actual project graph and compiled provider dependency closures measured | Build-time improvement is not asserted without controlled benchmark conditions |

Restored guards exposed existing application inconsistencies: duplicate enum parsing, direct value-object constructors, Lessons administration reading through its write port, and Identity session reads hydrating security aggregates. These were corrected while preserving caller contracts.

The full backend run also exposed an existing MCP search-policy mismatch: the Node indexer supported `identityScope: identity` but C# rejected it. Both validation and ranking now support the scope, with a symbol-title regression test. All 267 MCP tests pass after the correction.

## Remaining Application → Application implementation references

There are 11 direct project edges. Namespace-only graph edges also include contracts whose legacy namespace contains `Application`; those are not necessarily implementation assembly references.

| Consumer → owner | Consumed surface and rationale |
| --- | --- |
| Dashboard → Cycles | Current-cycle request and cycle response models for dashboard composition |
| Dashboard → DailyAdvices | Daily-advice request and response models |
| Dashboard → Meals | GetMeals request and response mapping |
| Dashboard → Tdee | TDEE insight request and response models |
| Export → Cycles | CycleModel and cycle export behavior; consent and sensitive-data handling remain owner controlled |
| Admin → Ai | IAiAdministrationReadService and administration result models |
| Admin → Gamification | IAchievementDefinitionAdministrationService and administration input/output models |
| Meals → Images | ImageAssetResolver helper over the owner access capability |
| Products → Images | ImageAssetResolver helper over the owner access capability |
| Recipes → Images | ImageAssetResolver helper over the owner access capability |
| Products → Usda | IUsdaFoodSearchService for branded search enrichment |

These references are explicitly recorded in the project dependency matrix. They do not authorize importing a foreign concrete handler or repository. Requests still travel through mediator and semantic services. Some APIs remain co-located with implementation; this is an acknowledged boundary limitation.

## Verification and practical limits

The solution build and complete frontend `npm run verify` pass. The frontend run includes lint, dependency/state ownership checks, all application/library builds, prerender checks, 3,193 Angular tests and script tests. PostgreSQL validates Hydration rollback/cascade/model compatibility and Identity session isolation/projection. Architecture tests cover the module inventory, dependency matrix and persistence capabilities. Initializer tests resolve its command-specific services without accessing a live database.

The first complete backend run was red: a preexisting MCP policy defect and two timing-sensitive integration failures appeared under heavy machine load. MCP was fixed and rerun; Products and MailRelay are rerun separately. Consult the implementation handoff and local TRX/log artifacts for final test counts and any unresolved verification gates; this document does not turn the original full run into a passing result.

No production environment or external provider was modified. No database migration or HTTP payload/route/status change is introduced. Existing snapshot and API contract tests remain applicable. No package versions were upgraded. Helper assembly relocation requires coordinated rebuilding of hosts and providers.

See [ADR 0029](../adr/0029-reviewed-persistence-capabilities-and-narrow-adapters.md) for the durable decisions. Local evidence is under `.artifacts/architecture-review-20260905/` and `.artifacts/architecture-improvements/test-results/`.
