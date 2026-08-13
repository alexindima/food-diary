---
id: workflow-code-graph
title: Local Code Intelligence Graph
kind: workflow
status: current
summary: Query an incremental SQLite symbol and consumer graph without replacing governed Wiki evidence or committed project knowledge.
sources:
  - .llm-wiki/tools/code-graph.mjs
  - .llm-wiki/tools/Manage-LlmWikiCodeGraph.ps1
  - .llm-wiki/tools/Get-LlmWikiGraphResearch.ps1
  - .llm-wiki/tools/Get-LlmWikiGraphTestPlan.ps1
  - .llm-wiki/tools/Test-LlmWikiCodeGraph.ps1
  - .llm-wiki/tools/Measure-LlmWikiCodeGraph.ps1
  - .llm-wiki/wiki.ps1
---

# Local Code Intelligence Graph

The experimental graph stores reconstructable code intelligence in
`.artifacts/llm-wiki/code-graph/code-graph.sqlite`. It is a local cache and is
never committed. Human-reviewed policy, acceptance, evidence, journeys, and
architecture documentation remain JSON or Markdown sources in Git.

Build or incrementally refresh the graph:

```powershell
./.llm-wiki/wiki.ps1 graph-build
```

The first build scans tracked and untracked C#, TypeScript, HTML, and project
files. Later builds use file size and modification time before hashing and
parsing only candidates whose metadata changed. Updates run in one SQLite
transaction and deleted files cascade to their symbols and token edges.

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

The fast trace route refreshes the graph, uses it only when an exact symbol
exists, and falls back to the established semantic trace when it does not.
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
