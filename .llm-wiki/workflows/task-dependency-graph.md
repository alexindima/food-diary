---
id: workflow-task-dependency-graph
kind: workflow
status: current
title: Coordinate parallel AI tasks with an executable dependency graph
summary: Detect write conflicts, shared boundaries, contract ordering, and safe merge waves across task workspaces.
tags:
  - workflow
  - task
  - graph
  - coordination
  - merge
sources:
  - .llm-wiki/tools/Get-LlmWikiTaskGraph.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskWorkspaces.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskAudit.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskHandoff.ps1
---

# Coordinate Parallel AI Tasks

Inspect all active task workspaces before assigning or merging parallel work:

```powershell
./.llm-wiki/wiki.ps1 task-graph

./.llm-wiki/wiki.ps1 task-graph -FailOnBlocked

./.llm-wiki/wiki.ps1 task-graph -IncludeSealed -Format Json
```

The graph compiles one node from each current task packet. Nodes retain changed
paths, direct/transitive/downstream modules, projects, scopes, generated
actions, and contract-sensitive policy rules.

Edges have executable meaning:

- `write-conflict` is critical and blocking when two active tasks write the
  same path. Parallel merge must stop until ownership or scope is changed.
- `boundary-coordination` marks different files inside the same module and
  project. The later task must rebase and recheck shared invariants.
- `generated-artifact-coordination` delays shared derived outputs until both
  source changes are integrated.
- `module-dependency` directs an upstream module task before work in a
  downstream consumer.
- `contract-before-consumer` directs a contract-producing task before affected
  consumer work and requires consumer compatibility checks after rebase.

Directed edges are topologically sorted into deterministic merge waves.
Ordering cycles and exact-path conflicts make the graph invalid.
`-FailOnBlocked` converts either condition into a failing gate suitable for CI
or an orchestration loop.

`task-list` includes edge counts, blocking conflicts, prerequisites, and
dependents for every active workspace. `task-audit` promotes a blocking graph
edge to `conflict` and recommends the failing graph command. Task handoff JSON
and Markdown carry the same related edges so a resumed agent sees parallel-work
constraints before editing.

Sealed workspaces are excluded by default because their work is historical.
Use `-IncludeSealed` for post-merge analysis or to explain why a newly opened
task depends on completed work.
