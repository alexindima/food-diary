---
id: workflow-code-graph
title: Local Code Intelligence Graph
kind: workflow
status: current
summary: Query an incremental SQLite symbol and consumer graph as the primary Development MCP code-context route without replacing governed Wiki evidence or committed project knowledge.
sources:
  - .llm-wiki/tools/code-graph.mjs
  - .llm-wiki/tools/Manage-LlmWikiCodeGraph.ps1
  - .llm-wiki/tools/Get-LlmWikiGraphResearch.ps1
  - .llm-wiki/tools/Get-LlmWikiGraphTestPlan.ps1
  - .llm-wiki/tools/Test-LlmWikiCodeGraph.ps1
  - .llm-wiki/tools/Find-LlmWikiContext.ps1
  - .llm-wiki/tools/Get-LlmWikiDiffContext.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskBrief.ps1
  - .llm-wiki/tools/Find-LlmWikiBackendContract.ps1
  - .llm-wiki/tools/Test-LlmWikiCompiledIndexSqlParity.ps1
  - .llm-wiki/tools/Test-LlmWikiDiffContextSqlParity.ps1
  - .llm-wiki/tools/Test-LlmWikiTaskBriefSqlParity.ps1
  - .llm-wiki/tools/Test-LlmWikiBackendContractSqlParity.ps1
  - .llm-wiki/tools/Test-LlmWikiSqlContextShadow.ps1
  - .llm-wiki/policies/context-search-ranking.json
  - .llm-wiki/evals/context-search.json
  - .llm-wiki/evals/context-search-holdout.json
  - .llm-wiki/evals/context-search-generalization.json
  - .llm-wiki/evals/context-search-validation.json
  - .llm-wiki/evals/context-search-probe.json
  - .llm-wiki/evals/context-search-probe-2.json
  - .llm-wiki/evals/context-search-probe-3.json
  - .llm-wiki/evals/context-search-probe-4.json
  - .llm-wiki/evals/context-search-probe-5.json
  - .llm-wiki/evals/context-search-probe-6.json
  - .llm-wiki/evals/context-search-probe-7.json
  - .llm-wiki/evals/context-search-holdout-100.json
  - .llm-wiki/evals/context-search-postfix-control-30.json
  - .llm-wiki/evals/context-search-posttune-control-30.json
  - .llm-wiki/evals/development-context-bundles.json
  - .llm-wiki/tools/Measure-LlmWikiSqlContextEvaluation.ps1
  - .llm-wiki/tools/Test-LlmWikiSqlContextEvaluation.ps1
  - .llm-wiki/tools/Test-LlmWikiDevelopmentContextEvaluation.ps1
  - FoodDiary.Development.Mcp/Wiki/SqliteWikiContextSearch.cs
  - FoodDiary.Development.Mcp/Wiki/WikiContextSearchEvaluationRunner.cs
  - FoodDiary.Development.Mcp/Wiki/DevelopmentContextEvaluationRunner.cs
  - FoodDiary.Development.Mcp/Wiki/ContextRoutingTelemetryStore.cs
  - docs/adr/0013-read-only-sqlite-context-search-in-development-mcp.md
  - docs/adr/0014-sql-first-development-context-with-json-fallback.md
  - .llm-wiki/tools/Measure-LlmWikiCodeGraph.ps1
  - .llm-wiki/wiki.ps1
---

# Local Code Intelligence Graph

The experimental graph stores reconstructable code intelligence in
`.artifacts/llm-wiki/code-graph/code-graph.sqlite`. It is a local cache and is
never committed. Human-reviewed policy, acceptance, evidence, journeys, and
architecture documentation remain JSON or Markdown sources in Git.

Each build also publishes
`.artifacts/llm-wiki/code-graph/code-graph.fingerprint`. Query caches hash this
small sidecar instead of the live SQLite file, avoiding WAL writer/read-lock
races and a full database scan during cache-key construction.

The same database contains a versioned FTS5 projection named
`context_search`. It indexes code paths and symbols, compiled module, contract,
and quality records, Wiki and current documentation, and scoped `AGENTS.md`
files. `Manage-LlmWikiCodeGraph.ps1 search` queries this projection with
deterministic path, module, task-type, and source-kind ranking.
PowerShell tools are indexed as code with function symbols and raw source text,
so operational Wiki commands participate in natural-language retrieval.
The database also projects the generated repository catalog and C# symbol index
into versioned `compiled_indexes` and `compiled_index_records` tables. The
frontend index is projected into the same tables as ordered feature, symbol,
route, and localization records.
`Find-LlmWikiContext.ps1` reads that projection by default, verifies source
hashes, and returns an error for a missing or stale projection instead of
silently reading JSON. `-CompiledIndexSource Json` exists only as an explicit
parity baseline. `Test-LlmWikiCompiledIndexSqlParity.ps1` compares seven
catalog/symbol-dependent result sections across representative queries, records
transport timing and candidate reduction, and is part of the `context-bundle`
smoke group. Diff context selects exact changed-path symbol rows in SQL and its
task-baseline parity test guards the complete legacy result shape. Task-brief
intent discovery consumes the same projection and has a separate end-to-end
parity and latency envelope. `Find-LlmWikiContext.ps1 -SqlShadow` remains a diagnostic
comparison between the compiled-index result and the broader FTS route and
reports SQLite query time separately from the PowerShell/Node round trip. The Development MCP aggregate route now uses a
fresh SQL result as its primary code-scope selection; policy, checks, reviewed
knowledge, and source claims remain Git-backed.

Context and diff queries retrieve a safe frontend candidate superset in their
existing compiled-context round trip, then keep the established PowerShell
scoring and output shape. Changed frontend paths use exact symbol-path selection.
The task-brief intent prefilter still reads the 0.23 MiB JSON source: its measured
parse cost is about 15 ms, less than a second Node process round trip, so that
consumer remains explicitly reported as partial migration rather than being
made slower for architectural uniformity.

The `query_documents` projection also stores record kind and source ordinal.
Backend-contract discovery uses specialized SQL views for contracts, consumers,
production/test consumers, ambiguity, and unconsumed contracts. The latter
builds one distinct consumed-name set instead of repeating a correlated scan.
Frontend-contract discovery uses the same projection for components, consumer
edges, API calls, translations, and components without direct specs. Both query
tools preserve exact group shape and source order, verify source hashes, and fail
closed instead of silently parsing JSON. Their JSON sources remain committed for
generation and explicit parity only. Projection freshness normalizes CRLF/LF
before hashing so isolated snapshots and cross-platform checkouts keep identical
lineage without weakening content validation. Runtime-owner discovery uses a
specialized query over the same projection: SQL selects the bounded component candidate set, ranks the
owners, and follows only the selected render chains instead of materializing the
1.19 MiB source in PowerShell. Its parity smoke compares ten explicit-path,
query-only, Unicode, and empty-result cases and requires both payload reduction
and a measured end-to-end improvement over the explicit JSON baseline.

General frontend trace also joins the compiled frontend symbol/route records with
frontend-contract documents in one SQLite process. It preserves the established
consumer and AI-dependency traversal over current source files while avoiding two
large PowerShell JSON parses and repeated interpreted scans. The default route
reports both source hashes, scanned/candidate/returned counts, SQL duration, and
full round-trip duration; its eight-case smoke requires exact output parity and a
measurable average improvement.

The committed retrieval suite has a 60-case regression corpus in
`.llm-wiki/evals/context-search.json` and a separately authored 40-case
challenge corpus in `.llm-wiki/evals/context-search-holdout.json`. The challenge
set includes Russian queries and deliberately indirect implementation searches.
Its recorded pre-tuning baseline was 11/40 top-1, 19/40 top-10, and 0.3276 MRR.
An additional frozen 70-case generalization corpus in
`.llm-wiki/evals/context-search-generalization.json` covers unseen files,
conversational Russian and English, neighboring implementation roles, frontend
utilities, service boundaries, and Wiki operations. Its pre-tuning baseline was
29/70 top-1, 54/70 top-10, and 0.5498 MRR. It is a regression gate, while the
original primary and challenge corpora remain the SQL promotion gate.
The 50-case validation corpus in
`.llm-wiki/evals/context-search-validation.json` was frozen after the structural
ranking changes. Its first-run result was 26/50 top-1, 42/50 top-10, and 0.6224
MRR; after its failure classes were addressed it reached 50/50 top-1 and became
a strict regression gate. The first 30-case probe in
`.llm-wiki/evals/context-search-probe.json` was then frozen and initially scored
13/30 top-1, 24/30 top-10, and 0.5547 MRR. After its failure classes were
addressed it reached 30/30 and joined the regression gate. The independent
follow-up in `.llm-wiki/evals/context-search-probe-2.json` was frozen next and
run once without tuning. Its diagnostic baseline was 17/30 top-1, 26/30 top-10,
and 0.6909 MRR. Generic role and scope improvements raised it to 30/30 top-1,
after which it joined the regression gate. A fresh independent probe in
`.llm-wiki/evals/context-search-probe-3.json` was then frozen with 30 previously
unused targets. Its untuned baseline was 19/30 top-1, 30/30 top-10, and 0.7612
MRR. File-scoped role and intent improvements raised it to 30/30 top-1, after
which it joined the regression gate. The next independent diagnostic set in
`.llm-wiki/evals/context-search-probe-4.json` was frozen with 30 more unused
targets before its only untuned run. Its baseline is 16/30 top-1, 29/30 top-10,
and 0.6708 MRR. File-scoped service, frontend-role, and Wiki-tool intent rules
raised it to 30/30 top-1 without changing its cases or regressing the previous
310 cases, after which it joined the regression gate.
A fifth independent probe in `.llm-wiki/evals/context-search-probe-5.json` was
frozen with 40 unused targets before its first run. Its blind baseline was
22/40 top-1, 36/40 top-10, and 0.6906 MRR. Shared structural-role scoring and
general query normalization raised it to 40/40 top-1. Fourteen probe-4
file-specific identity rules were removed during that work; the replacement
policy uses candidate role, source scope, query/candidate terms, and filename
affinity in both Node and .NET.
A sixth independent probe in `.llm-wiki/evals/context-search-probe-6.json` was
frozen with 30 more previously unused backend, frontend, and Wiki targets. It
deliberately mixes Russian and English behavior-level queries. Its blind
baseline was 9/30 top-1, 18/30 top-10, and 0.4028 MRR. Shared bilingual query
normalization plus structural policy, parser, observer, interceptor, utility,
mapper, workflow, and tool roles raised it to 30/30 top-1 without changing its
cases. It is now part of the strict regression gate.
A seventh 40-case probe adds five equal methodology cohorts: conversational
Russian, mixed RU/EN, adjacent-role disambiguation, behavior-to-test lookup,
and Wiki-intent lookup. Its raw blind baseline was 17/40 top-1, 32/40 top-10,
and 0.5574 MRR. One query that did not name a module was adjudicated to accept
all equivalent UTC normalizers; the corrected baseline is 18/40 top-1, 33/40
top-10, and 0.5812 MRR. General negated-role handling, explicit test-behavior
affinity, bilingual term normalization, and structural source roles raised it
to 40/40 top-1, with every cohort at 8/8. The same work made configured term
lookup safe for JavaScript prototype keys such as `constructor`.
Run either corpus without changing the committed JSON evaluation sources, or
run the combined gate:

```powershell
./.llm-wiki/tools/Measure-LlmWikiSqlContextEvaluation.ps1
./.llm-wiki/tools/Measure-LlmWikiSqlContextEvaluation.ps1 `
  -CorpusPath .llm-wiki/evals/context-search-holdout.json
./.llm-wiki/tools/Measure-LlmWikiSqlContextEvaluation.ps1 `
  -CorpusPath .llm-wiki/evals/context-search-generalization.json
./.llm-wiki/tools/Measure-LlmWikiSqlContextEvaluation.ps1 `
  -CorpusPath .llm-wiki/evals/context-search-validation.json
./.llm-wiki/tools/Measure-LlmWikiSqlContextEvaluation.ps1 `
  -CorpusPath .llm-wiki/evals/context-search-probe.json
./.llm-wiki/tools/Measure-LlmWikiSqlContextEvaluation.ps1 `
  -CorpusPath .llm-wiki/evals/context-search-probe-2.json
./.llm-wiki/tools/Measure-LlmWikiSqlContextEvaluation.ps1 `
  -CorpusPath .llm-wiki/evals/context-search-probe-3.json
./.llm-wiki/tools/Measure-LlmWikiSqlContextEvaluation.ps1 `
  -CorpusPath .llm-wiki/evals/context-search-probe-4.json
./.llm-wiki/tools/Measure-LlmWikiSqlContextEvaluation.ps1 `
  -CorpusPath .llm-wiki/evals/context-search-probe-5.json
./.llm-wiki/tools/Measure-LlmWikiSqlContextEvaluation.ps1 `
  -CorpusPath .llm-wiki/evals/context-search-probe-6.json
./.llm-wiki/tools/Measure-LlmWikiSqlContextEvaluation.ps1 `
  -CorpusPath .llm-wiki/evals/context-search-probe-7.json
./.llm-wiki/tools/Measure-LlmWikiSqlContextEvaluation.ps1 `
  -CorpusPath .llm-wiki/evals/context-search-holdout-100.json
./.llm-wiki/tools/Measure-LlmWikiSqlContextEvaluation.ps1 `
  -CorpusPath .llm-wiki/evals/context-search-postfix-control-30.json
./.llm-wiki/tools/Measure-LlmWikiSqlContextEvaluation.ps1 `
  -CorpusPath .llm-wiki/evals/context-search-posttune-control-30.json
./.llm-wiki/tools/Measure-LlmWikiSqlContextEvaluation.ps1 -FailOnRegression
./.llm-wiki/tools/Test-LlmWikiSqlContextEvaluation.ps1
```

Each evaluation reports top-1 accuracy, top-10 recall, mean reciprocal rank,
SQL timing, per-query misses, and metrics grouped by the optional case `cohort`.
Cases without a cohort remain compatible and are reported as `unclassified`.
Each corpus keeps lower regression thresholds
separate from stricter `switchCriteria`. The combined gate requires at least
100 committed cases, both corpora to meet switch criteria, and no top-10 miss.
The current Node result is 60/60 top-1 on the regression corpus, 40/40 top-1 on
the challenge corpus, 70/70 top-1 on the generalization corpus, 50/50 top-1 on
the validation corpus, 30/30 top-1 on each of the first four promoted probes,
40/40 on probe-5, 30/30 on probe-6, and 40/40 on probe-7. All 450 strict cases
are top-1. Probe-4, probe-5, probe-6, and probe-7 preserve their blind baselines in their committed
descriptions. Timing is diagnostic rather than a correctness gate because
workstation load varies.

The separate 100-case fallback-retirement holdout is not a tuned promotion
corpus and is not included in the 450-case strict total. Its queries and unique
targets were frozen before the first search run. The blind Node result was
21/100 top-1, 61/100 top-10, and 0.3404 MRR; the in-process .NET reader produced
the same ranks and top-five candidates for all 100 cases. After fixing
limit-dependent candidate pools, positive expansion of negated roles,
Russian/English technical normalization, and structural role affinity, both
readers reached 57/100 top-1, 100/100 top-10, and 0.719 MRR with zero exact-rank
or top-five differences. The original blind result remains immutable; the
post-fix result is regression evidence, not a new blind baseline. Use a new
unseen holdout for any later generalization claim.

A separate post-fix control froze 30 additional unique targets before its first
run and reused none of the earlier 550 targets. Without further tuning it
produced 17/30 top-1, 29/30 top-10, and 0.7079 MRR. Preserve that first-run
baseline. Shared bilingual subject terms and explicit file roles later raised
it to 27/30 top-1, 30/30 top-10, and 0.95 MRR with exact Node/.NET parity.

A second post-tuning control then froze 30 more targets that appear in none of
the previous 580 cases. Its untouched first run produced 18/30 top-1, 28/30
top-10, and 0.7467 MRR with exact Node/.NET rank and top-five parity. General
quality, invariant, test-role, notification, exercise, and change vocabulary
raised it to 26/30 top-1, 30/30 top-10, and 0.925 MRR. Both control baselines
remain immutable and neither control is part of the strict 450-case promotion
total.

The Development MCP reads the existing projection directly with
`Microsoft.Data.Sqlite`. It opens the database read-only, uses the same ranking
policy as Node, and accepts candidates only when the database contains the
exact current change-set fingerprint. Evaluate that in-process reader with a
built MCP:

```powershell
dotnet FoodDiary.Development.Mcp/bin/Debug/net10.0/FoodDiary.Development.Mcp.dll `
  --evaluate-context-search .llm-wiki/evals/context-search.json
dotnet FoodDiary.Development.Mcp/bin/Debug/net10.0/FoodDiary.Development.Mcp.dll `
  --evaluate-context-search .llm-wiki/evals/context-search-holdout.json
dotnet FoodDiary.Development.Mcp/bin/Debug/net10.0/FoodDiary.Development.Mcp.dll `
  --evaluate-context-search .llm-wiki/evals/context-search-generalization.json
dotnet FoodDiary.Development.Mcp/bin/Debug/net10.0/FoodDiary.Development.Mcp.dll `
  --evaluate-context-search .llm-wiki/evals/context-search-validation.json
dotnet FoodDiary.Development.Mcp/bin/Debug/net10.0/FoodDiary.Development.Mcp.dll `
  --evaluate-context-search .llm-wiki/evals/context-search-probe.json
dotnet FoodDiary.Development.Mcp/bin/Debug/net10.0/FoodDiary.Development.Mcp.dll `
  --evaluate-context-search .llm-wiki/evals/context-search-probe-2.json
dotnet FoodDiary.Development.Mcp/bin/Debug/net10.0/FoodDiary.Development.Mcp.dll `
  --evaluate-context-search .llm-wiki/evals/context-search-probe-3.json
dotnet FoodDiary.Development.Mcp/bin/Debug/net10.0/FoodDiary.Development.Mcp.dll `
  --evaluate-context-search .llm-wiki/evals/context-search-probe-4.json
dotnet FoodDiary.Development.Mcp/bin/Debug/net10.0/FoodDiary.Development.Mcp.dll `
  --evaluate-context-search .llm-wiki/evals/context-search-probe-5.json
dotnet FoodDiary.Development.Mcp/bin/Debug/net10.0/FoodDiary.Development.Mcp.dll `
  --evaluate-context-search .llm-wiki/evals/context-search-probe-6.json
dotnet FoodDiary.Development.Mcp/bin/Debug/net10.0/FoodDiary.Development.Mcp.dll `
  --evaluate-context-search .llm-wiki/evals/context-search-probe-7.json
```

The separate full-bundle evaluation runs real SQL retrieval, compact brief,
and fast test planning on one snapshot. It gates SQL routing, top-10 scope
recall, bounded expansion, complete components, focused checks, effective
layers, payload size, and end-to-end p95. It is intentionally a deep smoke
evaluation rather than a pre-commit check:

```powershell
./.llm-wiki/tools/Test-LlmWikiDevelopmentContextEvaluation.ps1
```

The evaluation calculates the live change-set fingerprint, so it fails closed
when the database is stale. The graph manager remains the only SQLite writer.
It records the fingerprint in the projection transaction and rolls back if the
worktree changes during the build. `get_development_context` attempts one graph
refresh for missing or stale state, then returns an explicit partial recovery
result when SQL is still unavailable, invalid, locked, or empty. It does not
invoke JSON trace automatically; `trace_backend_flow` remains an explicit
standalone tool. `get_server_status` exposes both
process-local timings and the last 1000 persistent route decisions. Persistent
events contain no query, intent, path, fingerprint, content, or payload data;
they record only route, normalized fallback category, duration, refresh
outcome, and timestamp. Status derives SQLite-unavailable count/rate, refresh
attempt/success/failure counts, the current SQLite-primary streak, and the
historical retirement threshold without expanding the stored event schema. See
ADR 0014 for the freshness protocol and completed compatibility-trace retirement.

Build or incrementally refresh the graph:

```powershell
./.llm-wiki/wiki.ps1 graph-build
```

The first build scans tracked and untracked C#, TypeScript, HTML, and project
files. C# declarations, identifier references, inheritance, method calls,
object construction, DI, mediator, HTTP, and migration relations are extracted
from Roslyn syntax trees in one batch. TypeScript declarations, imports,
inheritance, Angular selectors, routes, lazy imports, constructor/inject DI,
resources, constructions, and HTTP calls are extracted through the TypeScript
Compiler API; regex parsing remains limited to lighter HTML and configuration
formats. Later builds use file size and modification time before
hashing and parsing only candidates whose metadata changed. A no-op refresh
does not start Roslyn. Updates run in one SQLite
transaction and deleted files cascade to their symbols and token edges.
Concurrent refreshes use a PID- and token-owned directory lock. A build never
steals the lock from a live owner merely because it is old; an unreadable or
dead-owner lock must also exceed the stale threshold, and cleanup verifies the
ownership token before removing it.

Because the graph is reconstructable, a build that confirms `SQLITE_CORRUPT`
or `SQLITE_NOTADB` while holding that lock quarantines the database, WAL/SHM,
and dependency fingerprint with a unique `.corrupt-*` suffix, then rebuilds
from current sources. Custom database paths publish isolated fingerprint
sidecars. Busy/locked databases, cancellation, permission failures, and
unclassified SQLite errors are not corruption and never trigger quarantine.
The aggregate MCP route refreshes once for the two confirmed corruption codes;
if rebuilding still fails, it returns `context_search_unavailable` with a
recovery action and records `sqlite-unavailable` telemetry.

Semantic enrichment is automatic for ordinary incremental updates. SQLite
selects declaration files referenced by the changed C# identifiers, and Roslyn
records fully-qualified declaration IDs, selected overloads, implemented types,
and constructed types. A cold rebuild intentionally stays syntax-first so it
does not turn graph bootstrap into a minute-long compilation; exact commands
remain available immediately, while files touched by normal development gain
semantic edges without an extra flag. Syntax edges remain the fallback when a
bounded compilation contains unresolved external types.

Roslyn also records declared namespaces, namespace-shaped string literals, and
reflection/convention filters that compare `Type.Namespace`. During an
incremental edit it compares the declared namespace with the `.csproj` and
source folder, exposing `namespace-path-mismatch` evidence without treating
ordinary user-facing strings as code. A trace for a qualified namespace works
even when no type has that exact name and reports how many declarations each
convention filter actually selects; zero matches are labelled `EMPTY`.

Query the graph:

```powershell
./.llm-wiki/wiki.ps1 graph-symbol -Query RecipeNutritionUpdater
./.llm-wiki/wiki.ps1 graph-consumers -Query IRecipeOverviewReadService
./.llm-wiki/wiki.ps1 graph-trace -Query RecipeNutritionUpdater
./.llm-wiki/wiki.ps1 graph-impact -ChangedPath FoodDiary.Application/Recipes
./.llm-wiki/wiki.ps1 contract-consumers -Query IRecipeOverviewReadService -Fast
./.llm-wiki/wiki.ps1 graph-relations `
  -PlannedPath FoodDiary.Application/Recipes `
  -RelationKind mediator-handler,di-service
./.llm-wiki/wiki.ps1 graph-coverage
```

Exact backend traces can opt into the graph through the existing facade:

```powershell
./.llm-wiki/wiki.ps1 trace -Query RecipeNutritionUpdater -Fast
./.llm-wiki/wiki.ps1 research `
  -Intent "Extract Recipes into an isolated application module" `
  -PlannedPath FoodDiary.Application/Recipes `
  -Fast
./.llm-wiki/wiki.ps1 test-plan `
  -PlannedPath FoodDiary.Application/Recipes `
  -Fast
```

The fast trace route reads the already-prewarmed graph without performing a
hidden refresh, uses exact symbols or high/medium ranked candidates to establish
scope, and falls back to the established semantic trace when graph evidence is
insufficient. Read-only snapshots copy the SQLite database (and active WAL when
present) as a content-addressed dependency, so graph queries remain isolated
without seeing an empty ignored `.artifacts` tree.
Fast research requires an explicit module or planned path and returns bounded
source, dependency, and downstream-consumer evidence. Its boundary report keeps
the logical module, current project, physical source root, and target-project
candidate distinct. Fast contract-consumer discovery uses SQLite only as an
exact-file prefilter and then runs the established source-level method/access
analysis over those files. Regression tests compare it with the complete scan.
It is not a
replacement for source validation, extraction readiness, architecture tests,
or delivery gates.

The graph is the computed authority for exact declarations, consumers, typed
static relations, bounded impact, and the fast test plan. Typed edges cover DI
registrations, mediator handlers/dispatch, HTTP routes and clients, Angular
imports/lazy routes/templates, project references, test ownership,
configuration keys, workflow actions, and migration tables/columns. Every edge
stores source path, line, evidence text, parser version context, and confidence.
Typed Roslyn references are ranked before lexical token matches; token matches
remain a compatibility safety net while semantic project compilation is added
incrementally.

`graph-coverage` is the promotion gate from graph-first to graph-only. It
compares the SQLite declarations with every symbol in the committed C# and
frontend indexes. A missing symbol fails the graph regression rather than being
silently accepted. Policy, scoped instructions, ADRs, journeys, acceptance,
evidence, privacy decisions, and reviews remain Git-backed normative sources;
they are not reconstructable code intelligence and therefore are intentionally
not moved into SQLite.

Use graph-only commands for bounded source questions. Use ordinary `research`
and `test-plan` when historical precedent, runtime wiring, policy, or journey
coverage can change the answer. Publication gates continue to validate the
underlying sources even when SQLite selected their affected scope.

An empty backend semantic trace is a successful empty fragment, not a facade
failure. Extracted-module readiness recognizes explicit `Add<Module>Module()`
calls in executable composition roots in addition to legacy registrations from
the monolithic Application dependency-injection files.

Extraction readiness and its optional compile probe are content-addressed by
the complete graph input fingerprint, module, test scope, and probe mode.
Repeated readiness calls return the cached structured result; any tracked or
untracked code, project, module-dependency, or relevant configuration change
refreshes the graph and selects a different cache key. Synthetic dependency
fixtures always bypass the cache.

Run the regression and benchmark with:

```powershell
./.llm-wiki/tools/Test-LlmWikiCodeGraph.ps1
./.llm-wiki/tools/Measure-LlmWikiCodeGraph.ps1
```

The benchmark uses Recipes as a stable cross-layer scenario. Compare exact
symbol trace and contract consumers directly; module impact is broader than the
specialized extraction-readiness analyzer and should be assessed for relevance
as well as duration.
