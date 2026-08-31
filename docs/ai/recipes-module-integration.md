# Recipes integration with Ai and Dashboard

Base master: `c91a3fe3f898081fed1f1b6a9e10fdec2a421609`.
Recipes source: `86aea3d35722f6420a66a55df6f1ebb971f3d764`.
User requested investigation of failed checks and local integration, without push.

## Scope and compatibility

Preserve all Ai, Dashboard and Recipes project references, solution folders and
host registrations. Recipes owns Application, application ports, read Contracts,
Infrastructure and PersistenceModel; central Domain navigations, shared context,
migrations, HTTP contracts, SQL, transaction/advisory locking and media behavior
remain unchanged. JobManager adds persistence only, not Recipes handlers.
The stale central Recipes abstraction-ownership key is removed: its former
directory is empty after the port relocation, not a second owner.

Source evidence was copied to `.artifacts/recipes-extraction-archive-86aea3d35/evidence`:
167 files, 29,065,544 bytes, all SHA256 matched. The original worktree is retained.
Integration evidence is `.artifacts/recipes-integration-evidence`; ordinary .NET
outputs use only the repository-level `.artifacts/recipes-integration-build`.

## Original failures and bounded Wiki repairs

- Original unfiltered PostgreSQL run: 116/117, one measured first-page latency
  of 280.9 ms against the unchanged 250 ms budget. Its separate 2/2 recheck did
  not replace that failure. A new merged full run is required; no timing budget,
  query or test filter is changed to make it pass.
- Extraction readiness rejected Dietologist on both master and source checkout
  because it recognized only legacy application roots. A shared layout resolver
  now handles mapped/current projects, folder-only legacy modules, exact module
  identities and nested-port exclusion. Module research uses the same resolver.
  Readiness classifies module-owned consumers correctly; compile probes build
  existing application projects with their actual references/excludes/identity.
- Broad Recipes trace returned FavoriteRecipes/frontend candidates and missed
  owned module handlers. Candidate filtering now honors the layer end-to-end and
  matches exact module/project segments. Synthetic regressions cover multiple
  arbitrary modules, Windows paths, legacy roots and neighbor exclusions.
  Context-search ranking weights, queries and thresholds are unchanged.
- A synthetic staged rename proved that read-only snapshot overlays copied only
  the new path and retained the removed HEAD declaration. Overlay enumeration now
  uses `--no-renames`, preserving both deletion and addition. The old implementation
  failed the two-path assertion; scoped/batched and direct reproduction regressions
  cover the repair. This explains a concrete route to stale module paths, not every
  historical retrieval miss.
- The original frozen100/source and supplemental Wiki quality failures remain
  historical failures; new results must be recorded independently. No security
  scan, coverage collector, production request or deployment is performed.

## Runtime verification

Full solution restore, locked restore and build passed (zero warnings/errors).
Initial architecture 800/801 failed solely against the provisional pre-merge
repository catalog; generated indexes were rebuilt from the combined graph
and the complete architecture suite rerun successfully: 801/801. No guard weakened.

Final ordinary VSTest results: Recipes63; central Application1026/Domain877;
Ai Application59/Infrastructure87; Dashboard Application44/Infrastructure26;
Favorites78; RecipeCommunity33; ContentReports12; JobManager168; WebApi unit247;
Presentation825; central Infrastructure651; Architecture801; unfiltered PostgreSQL117;
selected HTTP176 (excludes only PostgresPerformanceBaselineTests).
All final results have zero failures/skips: 5,290 passed cases. Earlier failed
architecture output remains archived and is not added again to this total.
The old latency failure did not reproduce in the new unfiltered PostgreSQL run
(8m50s), including the unchanged Recipes 250ms assertion. Its environmental cause
is not proven. EF reports no pending model changes; full NuGet audit is clean.
No retained package resolved versions, Ai/Dashboard module files, Recipes module
implementation relative to its source commit, Domain source or HTTP/migration
snapshots changed in integration. All 259 solution project paths exist uniquely.

Native affected smoke passed research-confidence, extraction-readiness,
adaptive-evals, code-graph (including trace output), and tool-contract. Its actual
context-bundle exit1 remains top10=96/100, acceptedPrecision=.9481 and
errorCaptureRate=.5, matching the measured prior-master Ai/Dashboard integration
failure. The final facade after the rename repair also exited 1 at exactly these
three quality thresholds, without timeout. No aggregate context-bundle pass was
created. This is the measured c91a3fe master result, not the older source-branch
98/100 exception. Unknown-baseline supplemental failures remain unresolved.

The full separate read-only-guard smoke group passed (222.25s), including staged
rename/batching, mutation isolation, retrieval contracts and a JSON cold checkout.
It was run separately because the full gate exits at context-bundle before reaching
this serial group. The facade Recipes trace now returns current module handlers
and ports with Backend scope, without legacy donor, FavoriteRecipes or frontend
candidates. This is bounded candidate discovery, not proof of a full execution chain.

Native architecture-health, failure-knowledge validation, change policy and source
impact checks passed independently. All 65 affected pages were reviewed. The final
runtime count is independently recomputed from 17 TRX files in
`final-runtime-summary.json`, including their SHA256 hashes. Native clean completed
with zero warnings/errors; evidence and shared Wiki caches were retained.

The directory-only `test-plan -ChangedPath Modules/Recipes` still returns zero
focused files/commands/scenarios; the tests above were selected from verified
ownership and source paths. This remaining planning limitation was not hidden or
expanded into another unrelated generator change. Decision routing found the
existing ADRs and no additional deterministic ADR trigger.

Governed state is retained in `.artifacts/llm-wiki/tasks/recipes-integration-20260831`.
Its initial legacy-path acceptance text is corrected through native matrix
initialization to the actual module path and per-host capabilities, retaining the
other original criteria and adding explicit checks for the three generic repairs
and preservation of Ai/Dashboard. Original criteria/evidence are archived before
closure. Canonical check definitions and actual executed commands are bound using
the native lineage builder; the same full PostgreSQL run supports both equivalent
policy IDs. Wiki quality is explicitly non-green even when delivery accepts a
known-baseline exception. Final validation, critique and context-assessment logs
are stored alongside runtime evidence; they are not substitutes for quality gates.

The initial Wiki start and brief passed. A redundant follow-up research overlapped
the unresolved merge and failed parsing conflict markers in its isolated snapshot;
that diagnostic is retained, not interpreted as a source defect or successful
research. Stable post-merge diagnostics and final checks are recorded separately.
