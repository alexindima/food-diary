---
id: workflow-index-pipeline
kind: workflow
status: current
title: Run the staged index pipeline
summary: Build or verify independent knowledge indexes concurrently while preserving their dependency order.
tags:
  - workflow
  - performance
  - ci
  - indexes
sources:
  - .llm-wiki/tools/Test-LlmWiki.ps1
  - .llm-wiki/tools/Test-LlmWikiLint.ps1
  - .llm-wiki/tools/Test-LlmWikiPortable.ps1
  - .llm-wiki/tools/Test-LlmWikiLinux.ps1
  - .llm-wiki/tools/Invoke-LlmWikiIndexPipeline.ps1
  - .llm-wiki/tools/Build-LlmWikiQualityIndex.ps1
  - .llm-wiki/tools/Build-LlmWikiBackendContractIndex.ps1
  - .llm-wiki/tools/Build-LlmWikiFrontendIndex.ps1
  - .llm-wiki/tools/Build-LlmWikiFrontendContractIndex.ps1
  - .llm-wiki/tools/Invoke-LlmWikiAffectedSmoke.ps1
  - .llm-wiki/tools/Test-LlmWikiStrictAffected.ps1
  - .llm-wiki/tools/Invoke-LlmWikiObservedStage.ps1
  - .llm-wiki/tools/LlmWikiIndexCache.ps1
  - .llm-wiki/tools/Manage-LlmWikiVerificationCache.ps1
  - .llm-wiki/tools/Invoke-LlmWikiFullVerification.ps1
  - .llm-wiki/tools/LlmWikiProcess.ps1
  - .llm-wiki/wiki.ps1
  - .github/workflows/ci-tests.yml
---

# Run the Staged Index Pipeline

`wiki lint` is the fast prerequisite for both verification commands and CI. It
checks page contracts, sources, generated ownership, local links and anchors,
and high-confidence credential signatures before expensive index work begins.
Its isolated regression fixtures run immediately after it in the verification
gates.

The unified `wiki.ps1` entrypoint also pins ISO timestamps to JSON strings on
PowerShell versions that otherwise coerce them into `DateTime`. Nested tools
therefore compute the same deterministic hashes on Windows and Linux.
It forwards `-ProposedPath` to the direct `brief` and `test-plan` planning
commands so they can classify intended files before a Git diff exists.
The alias `-PlannedPath` accepts normal PowerShell arrays and a
semicolon-delimited convenience form. Context and privacy commands receive the
same normalized scope.

`wiki update`, `wiki verify`, `wiki verify-full`, and CI then use the same
dependency-aware index pipeline:

1. Source indexes run concurrently: catalog, C# symbols, frontend, frontend contracts, domain/data, configuration, runtime, and sensitive data.
2. Backend contracts and quality wait for the symbol index; module pages wait for the catalog.
3. Architecture health waits for catalog, backend contracts, frontend contracts, and quality.

The default concurrency is four processes and can be changed for constrained environments:

```powershell
./.llm-wiki/tools/Invoke-LlmWikiIndexPipeline.ps1 -MaxConcurrency 2
./.llm-wiki/tools/Invoke-LlmWikiIndexPipeline.ps1 -Check -MaxConcurrency 4
```

During iteration, select only indexes affected by the current diff:

```powershell
./.llm-wiki/wiki.ps1 update -AffectedOnly
./.llm-wiki/wiki.ps1 verify -AffectedOnly
./.llm-wiki/wiki.ps1 verify-fast
./.llm-wiki/wiki.ps1 verify-strict-affected
```

`verify-fast` is the explicit local completion gate for tiny, visual UI,
maintenance, and bounded bug work. It runs lint, the
dependency-aware affected-index check, change policy, and source-impact review,
while strict full verification remains enforced by pre-push and CI. When every
stale affected index artifact is already modified in the working tree, the fast
gate reports the checks as deferred because parallel Wiki work is possible and
tells the current session not to overwrite them. This is an iteration-only
diagnostic; source-impact findings caused by those artifacts are reported but
not enforced in that fast run. Full `verify` remains strict and must pass in
the integration session before commit, push, or final handoff. In every other
stale case, the pipeline emits one canonical `wiki.ps1 update` repair command
in addition to the focused `update -AffectedOnly` option.

`verify -Fast` is a supported compatibility spelling of `verify-fast`. Ordinary
`verify` runs every stage through an observed runner: it prints stage start and
duration, emits a heartbeat every 30 seconds, applies a stage-specific timeout,
and reports the exact standalone diagnostic command when a stage fails or times
out. Index and adaptive verification each receive five minutes; cheap contract,
policy, and impact stages use shorter limits.

Each index worker also emits its own heartbeat and has an independent timeout.
Timeout handling terminates the complete process tree on Windows and modern
.NET runtimes, preventing abandoned PowerShell descendants. Update mode holds an
exclusive repository-local lock and snapshots the generated tree; an internal
failure or worker timeout restores that snapshot before releasing the lock, so
parallel sessions cannot publish overlapping or partially successful updates.

Local strict and exhaustive verification can opt into
`-ResumePassedStages`. Each passed stage receipt is keyed by HEAD plus hashes of
all modified and untracked files. Unchanged reruns skip only completed stages;
any source edit invalidates the receipt set. Hooks and CI omit this switch and
therefore remain fully uncached.

`verify-strict-affected` is the final local gate for a grounded visual UI change.
It is read-only and deliberately bypasses verification and index caches, stale
deferral, and unrelated full-repository smoke. It runs portable lint, affected
indexes, affected smoke, change policy, and source impact. CI continues to run
`verify-full`, so scoped strictness never replaces repository-wide integration.

After a successful non-deferred run, `verify-fast` stores a worktree-local
content-addressed receipt under the ignored Git directory. The receipt binds
HEAD, resolved base ref, explicit task scope, completion mode, PowerShell/OS
identity, and hashes of every modified or untracked file. Repeating the command
with the identical state returns immediately. Git raw metadata is included so a
file-mode-only change also invalidates the receipt. An exact match returns
immediately. In visual UI mode, a stylesheet-only delta since the last successful
receipt reuses prior source-index and source-review evidence, then reruns the cheap
policy/impact checks only for that CSS/SCSS delta. Any non-style edit, commit, new
or deleted non-style file, scope change, mode change, or runtime change invalidates
that incremental reuse. Strict `verify`, `verify-full`, publication hooks, and CI
never consume it.

The pre-commit hook runs the affected compiled-index freshness check when staged
source or Wiki generator inputs change. This catches a final TS/test edit made
after index generation before the stale artifacts can reach CI. CSS/SCSS-only
commits skip the compiled-index check because no compiled index reads stylesheets.

Adaptive routing regression, adaptive experience/lifecycle regression, the
integration-scan contract, and three deterministic eval shards are independent
read-only checks, so strict verification runs them concurrently and still fails
if any process fails. Shards partition cases by stable source order; every case
runs exactly once and each shard reports its own assertion total. Their
individual durations remain visible, and `Test-LlmWikiAdaptiveWorkflow.ps1`
still defaults to `-Group All` for a simple standalone full regression.

The quality, backend-contract, frontend, and frontend-contract checks use local
content-addressed receipts during `verify-fast`. A hit requires matching hashes
for every declared source input, upstream compiled index, generator and shared
helper, plus the current generated output. The pipeline prints an explicit
cache-hit message. Any source, generator, dependency-index, or output change
invalidates its receipt and runs the normal freshness computation. Receipts
live under ignored `.artifacts/llm-wiki/index-cache`; they are never committed.
`wiki verify`, publication hooks, and CI deliberately bypass this optimization
and retain a complete deterministic check.

Frontend and C# test-source-only changes select the quality index without
forcing catalog, symbol, contract, sensitive-data, module-page, or architecture
rebuilds. Architecture health remains tied to project/catalog and public-contract
inputs; stylesheet-only changes select no compiled index.

Use `wiki smoke -SmokeGroup tools -AffectedOnly` during iteration. Its dispatcher
maps adaptive routing, solution/design planning, integration scanning,
dependency analysis, and facade changes to existing focused regression suites
and prints per-group duration. Only genuinely unknown shared-tool changes fall
back to the complete monolithic tools smoke. The ordinary tools smoke and
`verify-full` remain complete stateful publication/CI gates.

Use `-BaseRef <ref>` for a committed range or `-ChangedPath <path[]>` for an
explicit scope. The conservative dependency map still runs derived indexes and
architecture health when their source indexes can change. Final handoff and CI
continue to use the full pipeline.

Plan mode emits `Affected path count` and `Affected index tools` on the ordinary
PowerShell output stream. Tests and shell hooks should consume these stable plan
fields instead of parsing human-oriented `Write-Host` diagnostics.

An Angular `*.spec.ts`-only change selects quality plus its downstream
architecture-health check. It does not run frontend source, frontend contract,
or sensitive-data generators because test content cannot change those indexes.
Stylesheet-only changes select no compiled index because no generator reads CSS
or SCSS contents. Template-only changes select the frontend and frontend-contract
indexes plus downstream architecture health, but skip quality and sensitive-data.
The pipeline prints per-generator and total duration to make remaining hotspots
visible instead of treating index latency as an opaque fixed cost.

Workers are isolated PowerShell processes with real exit-code propagation. A failed worker fails its stage and prevents dependent stages from running. Parallelism changes execution time only; every existing generator and freshness check still runs.

`wiki verify` is the interactive gate. `wiki verify-full` and CI additionally
run the portable contract and an adaptive stateful tool-smoke suite. Its `Core`
profile always validates navigation, contracts, policies, indexes, acceptance,
evidence, and durable-memory isolation. The governed task-workspace and
orchestration lifecycle remains in the `Full` profile and is selected when the
commit changes those tools or scheduler policy. Use
`./.llm-wiki/tools/Invoke-LlmWikiFullVerification.ps1 -FullTools` to force it.
Use `-CoreTools` only for focused profiling of the Core pipeline; CI keeps the
conservative automatic selection.
Full verification completes the index freshness checks before starting the stateful
tools. This prevents tool-smoke readers from observing generated files while
index workers are replacing them. Index workers remain concurrent within their
dependency-aware stage, and every non-zero exit is propagated. CI runs this gate
in its own job alongside backend, PostgreSQL, dependency, and frontend jobs, so
Wiki verification does not serialize the backend test pipeline.

Use the focused commands before the full gate:

```powershell
./.llm-wiki/wiki.ps1 smoke -SmokeGroup portable
./.llm-wiki/wiki.ps1 smoke -SmokeGroup linux
./.llm-wiki/wiki.ps1 smoke -SmokeGroup tools
```

The Linux group uses the pinned
`mcr.microsoft.com/powershell:7.5-ubuntu-24.04` Docker image on non-Linux
workstations and directly executes the portable group on Linux.

After these gates, CI publishes the compiled LLM Wiki change-review report to
the GitHub job summary so reviewers see the same scope, risk, and readiness
assessment.
