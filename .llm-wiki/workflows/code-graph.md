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
```

Exact backend traces can opt into the graph through the existing facade:

```powershell
./.llm-wiki/wiki.ps1 trace -Query RecipeNutritionUpdater -Fast
./.llm-wiki/wiki.ps1 research `
  -Intent "Extract Recipes into an isolated application module" `
  -PlannedPath FoodDiary.Application/Recipes `
  -Fast
```

The fast trace route refreshes the graph, uses it only when an exact symbol
exists, and falls back to the established semantic trace when it does not.
Fast research requires an explicit module or planned path and returns bounded
source, dependency, and downstream-consumer evidence. It is not a
replacement for source validation, extraction readiness, architecture tests,
or delivery gates.

Run the regression and benchmark with:

```powershell
./.llm-wiki/tools/Test-LlmWikiCodeGraph.ps1
./.llm-wiki/tools/Measure-LlmWikiCodeGraph.ps1
```

The benchmark uses Recipes as a stable cross-layer scenario. Compare exact
symbol trace and contract consumers directly; module impact is broader than the
specialized extraction-readiness analyzer and should be assessed for relevance
as well as duration.
