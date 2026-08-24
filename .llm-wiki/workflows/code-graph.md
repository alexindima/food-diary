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
`Find-LlmWikiContext.ps1 -SqlShadow` remains a diagnostic comparison with the
JSON retrieval and reports SQLite query time separately from the
PowerShell/Node round trip. The Development MCP aggregate route now uses a
fresh SQL result as its primary code-scope selection; policy, checks, reviewed
knowledge, and source claims remain Git-backed.

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
Run either corpus without changing the JSON-authoritative path, or run the
combined gate:

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
refresh for missing or stale state, then uses the JSON trace fallback when SQL
is still unavailable, invalid, or empty. `get_server_status` exposes both
process-local timings and the last 1000 persistent route decisions. Persistent
events contain no query, intent, path, fingerprint, content, or payload data;
they record only route, normalized fallback category, duration, refresh
outcome, and timestamp. See ADR 0014 for freshness, fallback, and eventual
compatibility-trace removal criteria.

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
if rebuilding still fails, the compatibility JSON trace remains the fallback.

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
