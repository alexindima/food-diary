---
id: workflow-context-strategy-experiments
kind: workflow
status: current
area: ai-development
title: Experiment with context strategies safely
summary: Build isolated compact, coverage, depth, and policy-balanced context variants, benchmark them, and retain only an integrity-protected recommendation.
tags:
  - context
  - experiments
  - benchmark
  - agents
sources:
  - .llm-wiki/tools/Manage-LlmWikiContextExperiment.ps1
  - .llm-wiki/tools/Manage-LlmWikiContextBenchmark.ps1
  - .llm-wiki/policies/workspace-policies.json
---

# Experiment with context strategies safely

After creating and scoring the current context, inspect the deterministic variants:

```powershell
./.llm-wiki/wiki.ps1 task-context-experiment-plan `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name>
```

Run the experiment:

```powershell
./.llm-wiki/wiki.ps1 task-context-experiment-run `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name>
```

The runner creates isolated temporary task workspaces and evaluates:

- `baseline`: rebuild the current limits as the control;
- `compact`: reduce the character budget without reducing item capacity;
- `coverage`: allow more sources with modestly more characters;
- `depth`: keep the sources and deepen their excerpts;
- `balanced`: apply the current budget optimizer recommendation.

Duplicate limit combinations are removed. Every remaining variant passes through
the normal context security scanner, budget optimizer, and baseline benchmark.
Regressed variants cannot win. The ranking prefers the highest safe quality score,
then fewer used characters, then a stable variant ID.

Temporary workspaces are deleted even when a variant fails. The source workspace
is never rewritten. Only `context-experiment.json` remains, containing hashes for
the baseline packet, bundle and budget, all plans and results, the winning limits,
the policy, and the experiment implementation.

Verify it before acting on the recommendation:

```powershell
./.llm-wiki/wiki.ps1 task-context-experiment-verify `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name> `
  -FailOnInvalid
```

Applying the suggested limits remains an explicit reviewed action through
`task-context-create`. Once the baseline changes, the old experiment receipt
correctly becomes stale and must be rerun.
