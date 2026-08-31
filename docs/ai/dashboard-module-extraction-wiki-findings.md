# Dashboard logical extraction: Wiki findings and evidence

## Scope and provenance

Base `a11d9a5d2c4dce2682abb4691b2b23fb6b38b9a6`, initially clean detached
worktree `C:/Users/alexi/.codex/worktrees/11c6/FD`, subsequently named
`codex/dashboard-module-extraction`. No master edits, push, production/SSH access,
paid provider calls or coverage collectors. Pre-edit ownership inventory:
`docs/ai/dashboard-ownership-inventory.md`.

Application, Application/Abstractions, Contracts and Infrastructure are real
projects under Modules/Dashboard, with two owned test projects in a nested tests
solution folder. Application preserves its legacy assembly and CLR namespaces.
No Domain/PersistenceModel is justified: Dashboard owns read composition, not any
contributing aggregate, identifier, EF mapping or write repository.

The stable statistics interface and bucket model live in Contracts. A one-way
central Abstractions reference preserves existing statistical consumers without
giving Statistics a reference to Dashboard Application. Internal projection ports
live separately in module Abstractions. Infrastructure references the central
DbContext one-way and retains internal adapter visibility. Hosts explicitly install
optimized readers after AddInfrastructure, preserving concrete/interface scoped
aliases; Application fallback registrations remain intact. HTTP transport, shared
PostgreSQL fixtures, migrations/model snapshot and contributing entities remain
with their original owners. Consumers rebuild together; moving a contract type
between assemblies does not promise compatibility with old precompiled binaries.

## Wiki usefulness and limitations

- The module page found all four original source areas and eleven focused test
  paths, including the real PostgreSQL body-projection regression. Trace resolved
  GetDashboardSnapshot to its builder and user-context implementation.
- `Business-module consumers: none observed` was a false negative. Actual
  IDashboardStatisticsReadService consumers include Statistics, Cycles, Tdee,
  WeeklyCheckIn and Gamification. Generated `extracted-project` described the old
  horizontal Application assembly; it did not establish physical logical-module
  extraction or aggregate ownership.
- The focused module page omitted mixed DI/date tests and the PostgreSQL HTTP
  first-dashboard flow/payload snapshot. Source searches found these. The named
  DashboardValidatorTests also included DailyAdvice and Statistics tests; those
  remain central while Dashboard validation moves to the module suite.
- Git/source review of `917897f93` identified the prior one-day weekly-statistics
  reuse and concatenated latest/trend body queries. Its QueryPlanIntegrationTests
  belong to the shared PostgreSQL suite and remain there. No separate API tuning,
  query redesign or arbitrary wall-clock gate is part of this extraction.
- The initial start log was mistakenly redirected into the repository root;
  baseline OpenRead rejected the live file handle. The log was moved into ignored
  evidence and start repeated. This invocation mistake is not a generator bug.
- Start used a real PlannedPath array and JSON fallback because TypeScript was
  initially absent. It created an architectural workspace with 59 grounded paths,
  seven phases and 17 criteria. npm ci later installed locked frontend prerequisites
  and the normal Husky launcher. No package upgrade was requested.
- Research/brief/test-plan/decision/ownership/privacy/topology/dependencies/rollout
  and trace were invoked and logged. Before changes, dependency/ownership commands
  reported no diff rather than proving isolation. The explicit source inventory
  supplied missing edges. Broad topology output included unrelated provider paths;
  these were discovery noise, not reasons to change provider behavior.
- Research required a boundary decision despite the supplied compatibility scope;
  design was invoked with those source-backed decisions, without asking the user to
  repeat already-given authorization. No Wiki ranking, frozen queries, thresholds
  or generator special cases were changed.

## Verification status

The initial source audit compared all 65 moved production C# files with the exact
base and found identical content after line-ending normalization. Infrastructure
namespace alignment subsequently changes only the namespace and DbContext import;
the final audit separately verifies that normalized bodies/declarations still
match. Application and contract CLR namespaces remain unchanged.
Every retained NuGet resolved package version is unchanged. Static solution/XML
inspection found 247 unique existing projects, no missing ProjectReference target,
and no mismatch against the deliberately updated dependency matrix. These checks
do not substitute for compilation or runtime tests.

Parent-authorized fixture maintenance is audited in
`docs/ai/dashboard-fixture-path-audit.md`: exact old/new targets and source evidence,
with query text, IDs, expected-target counts and holdout-100 preserved.

The full solution force-evaluate restore and build passed (0 warnings/errors).
The initial build failed on namespace/fixture/guardrail alignment; the corrected
build and full Architecture reruns passed. Initial Architecture had five failures
(776/781); final Architecture has 781/781. The JobManager production-mirroring
fixture initially omitted the newly explicit reader registration; aligning that
fixture with the actual host produced 168/168 passing tests. Initial failure logs
remain alongside successful reruns.

| Executed VSTest suite | Passed | Failed/skipped |
| --- | ---: | --- |
| Dashboard Application | 44 | 0/0 |
| Dashboard Infrastructure | 26 | 0/0 |
| Full Architecture, after final consumer rebuild | 781 | 0/0 |
| Central Application donor | 1148 | 0/0 |
| Central Infrastructure donor | 738 | 0/0 |
| Statistics / WeeklyCheckIn / Tdee | 24 / 30 / 35 | 0/0 |
| Gamification / Cycles | 45 / 141 | 0/0 |
| Presentation / Web.Api unit / JobManager | 825 / 247 / 168 | 0/0 |
| Full unfiltered Infrastructure.IntegrationTests | 116 | 0/0 |
| HTTP excluding six unrelated latency cases | 175 | 0/0 |

The full PostgreSQL suite ran exactly once without a filter, taking 26m47s in
VSTest (1677.8s outer command). It includes the Dashboard combined body-query
regression and all eight QueryPlan tests. Read-only health evidence showed active
CPU/database work during buffered output; it was not interrupted or duplicated.
The HTTP suite includes real PostgreSQL product-meal-dashboard first-request
behavior, new-user Dashboard payload and full/focused Swagger snapshots. The six
excluded latency cases cover refresh/products/recipes/meals/images/billing, not
Dashboard. This is selected HTTP coverage, not a full HTTP-suite claim.

EF has-pending-model-changes reports no changes; its existing tool/runtime patch
version warning is retained. NuGet audit reports no vulnerable direct/transitive
packages across 247 projects. No coverage collector or separate security scan ran.
All counts come from VSTest TRX (vstest-counts.json), not test source enumeration.

## Known quality gate and execution limits

The exact-base frozen holdout-100 remains byte-identical, with measured top1=94/100,
top10=98/100, MRR=.953 and errorCapture=.5 against .9. The first standalone JSON
measurement and later native/context-bundle/facade invocations actually exit 1.
The latter report top10=98<100 and errorCaptureRate=.5<.9. Baseline provenance:
`docs/ai/module-integration-exercises-mealplanning-recipecommunity.md`; missing
DailyAdvice/Notification targets were not retuned. This is a known quality FAIL,
not full Wiki green and not a Dashboard regression.

A separate affected-smoke invocation reached its 600s outer timeout after four
parallel groups passed but before context-bundle completed. That timeout is an
execution limitation, not a successful group or the holdout exception. Its child
processes were confirmed gone. One approved native run reused only legitimate
fingerprinted receipts for completed groups, then failed at the actual frozen
holdout gate after 244.49s. One subsequent facade verify also exited 1 at that same
gate after context-bundle 229.34s; it did not time out. No aggregate PASS receipt
was fabricated and no threshold, wrapper or policy was changed. Remaining targeted
checks are recorded separately; they do not imply the full context-bundle passed.

Evidence is in `.artifacts/dashboard-evidence`, including actual commands/receipts,
logs and `results/*.trx`; the governed workspace is
`.artifacts/llm-wiki/tasks/current`. Supplemental IDs are not active policy checks.
The native evidence-lineage builder separates canonical policy Definition from
the actual Command and exit code. Security/privacy attestations are bounded source
reviews, not a claim of a security scan or whole-repository contextual coverage.
Deleted donor paths are absent context inputs, not silently treated as scanned old
content; final context counts and current-source fingerprints belong to the
assessment artifact.

The only reusable runtime output scope is `.artifacts/dashboard-extraction`.
After its runtime consumers completed, native dotnet clean succeeded with zero
warnings/errors and preserved logs/TRX/Wiki caches. Any subsequent bounded tool
build uses that same scope and is cleaned again before the normal serialized hook.
Commit SHA/hook outcome and final clean status are supplied in the parent handoff;
no push, deployment or master integration is part of this worktree task.

## Approved generic discovery correction

The unchanged GovernedExtraction assertion for FoodDiary.Web.Api.csproj failed
because extraction discovery recognized legacy FoodDiary.Application.Dashboard
references but not the relocated physical FoodDiary.Modules.Dashboard project
filenames. This was a new failure, not the known retrieval holdout exception.
The parent independently confirmed and authorized a bounded generic correction.
Discovery now reads existing direct-child csproj files from declared *Projects
mapping directories, or an explicitly mapped csproj; it does not recursively scan
other modules. Escaped physical filenames have delimiter boundaries. Legacy CLR
namespace and DI-entrypoint recognition remains unchanged and independent of the
physical name. No abstractionAreas redesign, ranking, policy or threshold change.

The synthetic fixture fails on the old implementation at Host/New.csproj and
passes after the fix. It covers slash/backslash references, explicit-file mapping,
physical filename different from AssemblyName, legacy namespace and registration,
similar names, unmapped sibling/nested projects, and absent manifest. The real API
host assertion stays unchanged. Full GovernedExtraction including Windows fixture
has process exit 0. Evidence: extraction-references-before.log,
extraction-references-after.log, extraction-references-windows.log and
wiki-governed-windows-final.log. Nine corpora contain exactly eleven expectedPaths
substitutions; recursive JSON comparison confirms all other fields/target counts
unchanged (fixture-json-audit.json).

## Additional discovery limitations

Post-change ownership reports Ai, Dashboard, Dietologist, Fasting and Statistics
as direct modules and Admin downstream; this broad impact is navigation, not
ownership transfer. Rollout flags jobs/providers/public API because paths and host
references changed, although their runtime semantics did not. Dependency output
shows package removal/addition for project relocation, not a resolved-version
upgrade. The 247-project lockfile audit and actual project graph are authoritative.
Acceptance suggestions for AC-004 (Dashboard tests) pointed to unrelated central
AI tests; actual Dashboard test paths must be mapped manually. The first ownership
facade attempt failed in its temporary SQLite mirror (compiled-index projection
missing); the native ownership reader subsequently accepted the current packet's
explicit diff. No generator workaround was introduced for that limitation.

Initial CodeGraph attempts overlapped source changes and failed; this is not
silently equated with success. After edits stopped, the full regression reported
7117 files, 31525 symbols, passing incremental no-op and Recipes queries. The outer
PowerShell sequence then observed a residual native LASTEXITCODE=1 and did not
start verify. Both the positive assertions and outer exit1 are preserved; the
subsequent standalone pwsh -File verification has its own process outcome. No
LASTEXITCODE reset or wrapper change was made.

Approved module layer inference correction: the focused public GetDevelopmentContextAsync regression failed before the fix (9 failed/19 passed, 28 selected) and the full MCP suite passed after it (220 passed, zero failed/skipped). InferLayer now recognizes exact Modules/<nonempty module>/Application, Domain and Infrastructure segments, including Abstractions/Model descendants, without changing legacy roots or inventing Contracts semantics. Negative tests exclude test paths, unknown folders and prefix lookalikes. No ranking, queries, thresholds or CLI exit semantics changed.

The unchanged development-context corpus rerun completed with process exit0 and JSON passed=false. Dashboard dashboard-meal-projection-bundle now has expectedLayersPresent=true and contextBundleReady=true; expectedLayersRate becomes1 and readiness .75. Favorites, Notification and context-snapshot findings remain unmodified and their numerical baseline is UNKNOWN. See development-context-after.json and mcp-final.log/results/mcp-final.trx under .artifacts/dashboard-evidence. Product runtime suites were not repeated for this metadata-only fix.

The actual facade stopped at frozen100 with measured exact-baseline equivalence (94/100 top1,98/100 top10,errorCapture=.5); that exception is limited to this gate. Eight supplemental corpora also have semantic failures with numerical baseline UNKNOWN. Their process exits, denominators, every threshold/cohort violation, fingerprints and Dashboard old/new targets are documented in dashboard-wiki-supplemental-evals.md/json. Neither successful process exits nor source relocation hashes establish retrieval quality equivalence. This extraction does not claim full Wiki green or that only one quality failure exists.
