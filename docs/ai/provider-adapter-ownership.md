# Provider adapter ownership: Ai, Wearables, USDA and OpenFoodFacts

## Boundary

This coordinated batch moves twelve provider implementation/options files from
`FoodDiary.Integrations` into the four existing module Infrastructure projects,
under `Providers`. Original CLR namespaces, client names and logger categories
are retained. A narrowly scoped namespace-layout exception follows the existing
Notifications extraction precedent; positive ownership tests protect the exact
legacy types. This is a coordinated source rebuild, not old binary compatibility.

API and JobManager explicitly call `AddAiProvider`, `AddUsdaProvider`,
`AddOpenFoodFactsProvider` and `AddWearablesProvider`. Initializer never called
`AddIntegrations` and does not gain provider validation. Application/persistence
module registrations and all jobs/schedules remain unchanged.

Integrations stops registering these providers and removes its seven references
to their ports/contracts/Domain. The four Infrastructure projects reference it
one-way for the single shared `BoundedHttpContentReader`, `IntegrationUriValidator`
and `IntegrationsTelemetry`; internal friend access does not make these helpers
public. Integrations cannot reference the four owner projects back. No new shared
assembly, copied meter, source link, provider facade or module-to-module adapter
reference is introduced. Billing, authentication, Images/storage and mail-client
bridges remain unchanged. This intentionally retains a shared transport dependency,
not a claim that all SDK transitive dependencies have been eliminated.

## Preserved runtime contracts

- OpenAI: the same responses/input-token requests, prompt/schema construction,
  error sanitization, 60-second HTTP/overall request limits, maximum two 429
  retries and five-second Retry-After cap. Circuit-breaker name and configuration
  remain unchanged. Existing fallback and quota/orchestration boundaries stay put.
- Fitbit: optional complete configuration, secure redirect validation, scoped
  `IWearableClient`, 30-second HTTP and aggregate daily-data deadline, OAuth/token
  request bodies and all three sequential daily requests remain unchanged.
- USDA: 15-second HTTP timeout, no request loggers, singleton detail cache with
  2048 entries, eight in-flight admissions, positive/negative 30/5-minute TTLs
  and 20-second shared deadline. Caller cancellation still cancels its wait;
  404 is negative-cacheable, transient failures are not. Search limits stay 1..200.
- OpenFoodFacts: 10-second HTTP timeout, configured User-Agent, no request loggers,
  two exponential retries from 250 ms; search limits/cache/single-flight/HMAC key
  behavior unchanged (1024 entries, 16 MiB, 16 concurrent searches, 128 in-flight,
  10-minute fresh and six-hour stale TTLs, 15-second search deadline).
- Shared response byte/depth/deadline limits, meter/instrument/tag names, config
  section names, validation messages and `ValidateOnStart` remain unchanged.
  Separate OFF/Fitbit registration uses `TryAddSingleton(TimeProvider.System)`
  for standalone composition without replacing a supplied clock.

No EF mapping, SQL, migration, snapshot, HTTP route/payload, authorization, data
lifecycle or production credential changes are part of this batch.

## Tests and evidence

Provider-only USDA, OpenFoodFacts and Fitbit suites move to their owners. Mixed
external-food tests are split into owner suites; six option test methods are
split out of the mixed options catalog. Other options and cross-provider/host
composition tests remain central. The OFF collection remains non-parallel to
protect its shared static cache. Existing Ai provider tests already have an owner.
One USDA Infrastructure.Tests project is added with shared test build settings.

The pre-edit inventory contains 169 existing test methods across the relevant
source files. A method-body/attribute audit checks preservation across the split.
New standalone registration tests check owner assemblies/CLR names, typed-client
timeouts, option errors, clock preservation and scoped/cache lifetimes. HTTP safety
guardrails now scan all four provider roots as well as central Integrations.

Complete static changes precede consolidated restore/build and unfiltered test
execution. Actual commands, initial failures if any, TRX counts, source hashes,
reference/lock audits and Wiki receipts are retained under
`.artifacts/provider-ownership-evidence`; build outputs use only the separate
`.artifacts/provider-ownership` scope. No coverage collectors or live provider calls.
Verification results are recorded after execution, not inferred from test discovery.

## Wiki observations

Start, research, brief, test-plan, design, decision, ownership, topology, privacy,
dependencies and rollout were consulted. Design was ready; no deterministic ADR
trigger matched. On the clean pre-edit diff, ownership and dependency outputs were
empty and rollout inferred no configuration/provider changes despite explicit
planned paths. Direct source/DI inventories establish those responsibilities;
these query outputs are not proof of absence. Final current-graph update, reviews,
verification and governed checks are required before commit. Ranking policy,
weights, queries and thresholds are unchanged. The generic path-layout helper
now maps `Modules/<owner>/Infrastructure/Providers/<tail>` to the existing
Integrations selector identity. Ordinary Infrastructure and test exclusions stay
unchanged. Synthetic Inventory/Shipping tests reproduce FAIL before and PASS
after, including Windows paths and negative boundaries. This corrects the
introduced USDA navigation regression (19/20 to 20/20, matching exact base).

Source-proven fixture maintenance updates eleven one-to-one `expectedPaths`
literals in ten eval corpora and one split-test target in the same unseen corpus.
The latter maps ExternalFoodServiceTests to both USDA and OpenFoodFacts descendants;
all ten original methods remain unchanged, five per owner. Reverse transformation
restores the original corpus data;
case IDs, queries, cohorts, thresholds, ranking and historical measurements are
unchanged. The frozen holdout-100 has no affected path and remains unchanged.
Start also inferred notification/background-job criteria from the shared host
context. This extraction adds neither notification delivery nor background jobs;
those unrelated criteria are explicitly assessed, not silently marked passed.

### Actual Wiki quality limitation

The initial full facade verify exits 1 in the context-bundle frozen100 live gate:
top10 99/100 is below 100. A fresh detached checkout/index at exact base
`9faaacd7cbc87ac4d467d8a497dafce292edd32c` gives the same top1 96/100,
top10 99/100, MRR .9719 and sole miss `holdout100-conv-001` (shared DomainGuard,
rank 45). The final current diagnostic after the provider alias fix is identical.
Frozen100 and ranking policy hashes match baseline; no target/threshold tuning.
This proves ONLY that frozen100 failure is pre-existing, not that Wiki verify
passed or all retrieval quality is unchanged. Final evidence remains FAILED:
the later code-graph check exposes an introduced regression described below,
so a known-baseline exception cannot close the overall Wiki obligation.

Supplemental diagnostics expose further limitations, retained separately from
the facade result:

- Business regression: exact base 20/20; initial current 19/20 due to missing
  provider identity; fixed current 20/20 with unchanged queries/weights.
- Unseen100: base top1/top10 62/83, corrected current 60/82; both semantically
  fail. Existing 17 misses persist and `unseen-v2-062` is a NEW miss: YooKassa
  tests fall from rank 10 to 17, displaced partly by new generic provider
  registration tests. This measured navigation regression is NOT waived as
  baseline and was not repaired by tuning accepted targets or ranking.
- Validation50: base/current 48/49, same stale RecentItems target; both fail.
- Posttune30: base 25/29, initial current 24/29; both report semantic pass but
  retain the same missing old FoodQualityGrade target. These are not proof that
  the stricter full SQL evaluation footer passes.
- Other initial changed-corpus diagnostics: primary60/60, generalization67/70,
  holdout38/40, probe2 30/30 and probe3 30/30 (top1/top10) pass. The earlier
  unseen result 60/81 includes the subsequently corrected split-test target.
- Development-context evaluation exits 0 but overall JSON is false. USDA's
  scope/layer/SQL/check selection and context readiness pass; other cases are
  not baseline-certified. Cache, workflow-recovery and Users context helpers
  passed independently. The full SQL group remains failed, not aggregate-green.

These diagnostics justify a separate retrieval-quality follow-up, not a runtime
provider repair. Raw failures, fresh-base outputs and corrected outputs remain in
the evidence directory. Green delivery bookkeeping must not be read as a clean
Wiki-quality certificate.

Changing the path helper also activates the code-graph smoke group. Its first
run stopped on an additional stale fixture in Test-LlmWikiCodeGraphPathTransport:
the source tail still pointed at FoodDiary.Domain/Entities/Users/User.cs.
Git commit 7a9a3fad8 records its move to Modules/Users/Domain/Entities/Users/User.cs.
Only this literal is updated; oversized-scope, projection equality, Unicode and
invalid-input assertions are retained. This early failure is recorded separately
from the frozen100 quality failure, not mislabeled as the same gate outcome.
The same smoke script also retained three obsolete Users/Cycles/Meals anchors;
they now follow the exact R100 relocations in 1e85022a6, 416a70f77 and 464fb66e1,
with query text, expected role and all assertions unchanged. One index refresh
was rolled back by the native transaction after a Windows mapped-file write
failure on quality-index.json; this execution failure is retained separately.

After these fixture repairs, final facade verify still exits 1, now at
Test-LlmWikiCodeGraph's unchanged `OpenFoodFacts barcode lookup` assertion.
A second fresh exact-base graph returns the Application IOpenFoodFactsService
contract first (score 1474); current returns the relocated provider first (1580),
with the same contract second (1473). The provider now receives the existing
exact-module-identity boost because its real owner is OpenFoodFacts. This is a
NEW measured retrieval regression, not a baseline failure. Returning a fictitious
Integrations module identity or changing the expected target merely to pass this
query would hide the ownership distinction; neither is done. Resolving use-case
versus provider intent needs a separately scoped search-policy decision.
The full code-graph/context groups are NOT certified green, and delivery remains
blocked on wiki-verify despite passing runtime checks. No broader ranking tuning
or fabricated aggregate PASS is included in this batch.

## Executed verification

The consolidated force-evaluate and locked restores passed. Formatting completed
with the workspace-loader warning retained; the first complete solution build
passed with zero warnings/errors (217 seconds). No test campaign preceded it.

Final unfiltered results (latest execution per suite, no rerun double counting):

| Suite | Passed |
| --- | ---: |
| Ai Infrastructure / Application | 94 / 61 |
| Wearables Infrastructure / Application | 51 / 48 |
| USDA Infrastructure / Application | 30 / 32 |
| OpenFoodFacts Infrastructure / Application | 66 / 14 |
| Products / Meals Application | 130 / 163 |
| Central Infrastructure / Application | 422 / 374 |
| Architecture | 1138 |
| JobManager / Web.Api unit / Presentation | 168 / 247 / 825 |
| Full Infrastructure PostgreSQL | 93 |
| Full Web.Api HTTP integration | 182 |

Total: eighteen suites, 4138 passed, zero failed/skipped. The first Architecture
run had 1136 passed and two failures: the new USDA project was missing from the
not-yet-regenerated Wiki catalog, and Initializer's Dockerfile lacked the newly
transitive Integrations restore/source COPY. The native catalog generator and
two exact COPY lines corrected these omissions; only the full Architecture suite
was repeated. API/JobManager Dockerfiles already contained the required copies.
No runtime C# source or project input changed after the successful build/tests.

EF reports no changes since the last migration; its existing tools 10.0.10 vs
runtime 10.0.11 warning is retained. NuGet's whole-solution audit covers all 325
solution projects with no vulnerable entries/problems; retained resolved package
versions are unchanged. API compatibility reports zero breaking/additive or
behavioral restrictions. No frontend packages or configuration values changed.

Static evidence: all twelve provider source token streams, all 169 existing
test method/attribute streams, all four HTTP and options registration chains,
seven remaining central options chains, and 325 protected helper/foreign-provider/
migration files are unchanged. The final reference graph is acyclic. Source/
project manifests and TRX hashes are retained with the original failed logs.

Rollout requires rebuilding API, JobManager and Initializer from the same source
revision. There is no new secret, setting, schedule, schema or migration; roll
back the source and rebuild the coordinated hosts if needed. Provider latency,
error and cache metrics keep their existing names. No deployment was performed.
