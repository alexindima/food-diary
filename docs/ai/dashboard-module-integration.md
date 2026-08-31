# Dashboard integration with Ai

## Provenance and scope

Source Dashboard commit: `4248f07fba09d0861e3f971be33cbb3429bebd24`, based on
`a11d9a5d2c4dce2682abb4691b2b23fb6b38b9a6`. Target master before integration:
`ec86da7995d1603052052a3f6fd59fb100af47ed`, already containing Ai and orphan cleanup.
This local integration does not authorize a push/deployment or declare Wiki quality green.

The shared-file conflicts preserve both module project sets, host registrations,
dependency guards and ownership documentation. Both obsolete donor solution entries
are removed. Central Infrastructure installs neither module's extracted adapters;
hosts own composition. JobManager and its DI fixture install both Ai persistence
and Dashboard optimized readers. No new Domain/Model project is invented for Dashboard.
Module implementations, SQL/query behavior, tenant/UTC/cancellation semantics,
HTTP payloads and the central schema are not redesigned during integration.

Merged NuGet locks and Wiki indexes are regenerated from the combined graph.
Source-impact receipts must be renewed for changed hashes; historical reviews do
not certify the new tree. Existing Ai tests, module code and cleanup are preserved.

Architecture validation exposed a non-portable source-worktree artifact: the
manifest still declared the now-removed central Dashboard abstraction directory.
An empty untracked directory could satisfy the source check but cannot survive a
Git checkout. The obsolete ownership key is removed; module mappings remain.
Old local Dashboard bin/obj files (7,539,492 bytes) were moved recoverably to
`legacy-application-builds` inside the archive above. No test guard was weakened.

## Historical evidence retained

`C:/FD/.artifacts/dashboard-extraction-archive-4248f07fb/evidence` contains 221
source evidence files (23,577,920 bytes), copied with every SHA-256 checked.
Original embedded source paths/fingerprints remain historical. The source worktree,
branch and shared Wiki cache are retained; no source-worktree deletion is part of
this integration. New integration evidence is in
`C:/FD/.artifacts/dashboard-integration-evidence`.

Source TRX report Dashboard Application 44, Infrastructure 26, Architecture 781,
unfiltered PostgreSQL 116 and selected HTTP 175 (six unrelated latency tests
excluded). These are source runs, not merged-tree results. The source full build
and normal commit hooks passed. See the extraction report and ownership inventory.

## Wiki status and deferred work

- Dashboard's actual full facade exits 1 at frozen100 (94/100 top1, 98/100 top10,
  error capture .5). Its approved baseline exception is limited to that measured
  equivalence. An earlier 600-second timeout is a separate execution limitation.
- Eight supplemental corpora have semantic `passed=false`, with numerical baseline
  UNKNOWN. Process exit 0 is not a semantic pass. Development-context also remains
  non-green for Favorites, Notification and context-snapshot cases. See
  `dashboard-wiki-supplemental-evals.md/json`; these failures are not waived.
- Ai's additional retrieval regression and unresolved PDF timing/cancellation
  failures remain as recorded in `ai-module-integration.md`. Dashboard's 98/100
  baseline exception cannot certify the combined Ai/Dashboard tree.
- Dashboard's two general fixes are retained: physical mapped-project discovery
  for extraction planning, and exact module-path layer inference in MCP. Their
  source fail-before/pass-after regressions do not imply all Wiki quality passes.
- No ranking, quality threshold, frozen-query, provider behavior or broad fixture
  remediation is included. Governed critique scores are not retrieval-quality proof.

## Integration verification

Actual merged-tree verification (no coverage collectors):

- Forced restore and subsequent locked restore passed. Full solution build passed
  with zero warnings/errors in 2m37.66s. All 254 solution projects exist without
  duplicates; common package resolved versions did not change.
- Dashboard Application 44/44 and Infrastructure 26/26; Ai Application 59/59,
  Domain 23/23 and Infrastructure 87/87.
- Full Architecture 789/789, JobManager 168/168, Web API unit 247/247,
  MCP 220/220, central DI 101/101 and Presentation 825/825.
- PostgreSQL Dashboard body/query-plan/Ai quota seams: 20/20. This is a focused
  integration run, not the source task's full PostgreSQL 116-case run.
- HTTP integration 175/175 excludes only PostgresPerformanceBaselineTests;
  it is not described as an unfiltered full HTTP suite.
- Total selected final TRX: 2,784 passed, zero failed/skipped. Two earlier
  Architecture failures exposed the local old output folder and stale manifest
  key; original failed TRX are retained separately, not counted as final passes.
- EF pending-model check passed, with the existing tool/runtime 10.0.10/10.0.11
  warning. Migration, central context and HTTP snapshot diffs are empty.
- The physical-project discovery regression passed. Its initial larger governed
  regression correctly rejected a stale graph during index generation; after
  graph refresh the stable complete governed-extraction regression passed.
- Native Wiki update passed, and 57 affected pages were reviewed. One full verify
  actually exited 1, without timeout: context-bundle failed after 157.48s at
  top10 96/100, accepted precision .9481 and error capture .5. Workspace policy,
  page contracts, lint, index freshness and four other selected smoke groups
  passed. This reproduces the Ai-integration live gate values, not Dashboard's
  older 98/100 exception. No full verify retry or synthetic PASS receipt was made.
- Source and merged module implementations match their respective commits;
  the integration only reconciles shared composition/metadata and the stale
  ownership entry. Original source failures and unknown-baseline supplemental
  corpus results remain unresolved, not implicitly waived by these passes.
- Native clean removed this task's disposable build outputs with exit 0 and
  zero warnings/errors; user build scopes, worktrees and Wiki caches were not
  cleaned. Normal commit hooks run separately and their logs are retained.

Wiki planning still has limitations: the initial explicit-path brief returned no
focused tests; the later test plan selected central observability/Telegram tests
but omitted the requested module suites. The actual test list was derived from
the project graph and merge surfaces. Decision review found the established
logical-module pattern (ADR 0016); no new deployment or system-boundary decision
is introduced by merging the existing extractions.

The 254-project NuGet vulnerability audit passed. Standalone failure-knowledge,
change-policy (274 paths, 10 rules, zero violations) and source-impact checks
passed. Full-verification and failed-smoke logs are copied into integration evidence.

Native delivery replan succeeded, but the new integration governance workspace is
not closed: validation exited 1 with 17 unmapped acceptance criteria and pending
check/review receipts; critique exited 1, reject 19/100, for unresolved governance
and no selected-context trust assessment. Actual runtime results are in the TRX
summary, not falsely imported as a fully approved delivery bundle. In particular,
AC-001 still says source lives in deleted `FoodDiary.Application.Dashboard`, even
after refresh. It must not be marked satisfied for source under `Modules/Dashboard`.
This repeats the stale physical-path acceptance issue observed in Ai integration.
No successful context scan or release approval is claimed. These administrative
and retrieval-quality obligations remain deferred with the existing Wiki work;
the local merge is not a full Wiki pass or deployment approval.
