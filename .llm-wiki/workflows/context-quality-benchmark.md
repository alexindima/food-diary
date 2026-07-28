---
id: workflow-context-quality-benchmark
kind: workflow
status: current
area: ai-development
title: Benchmark context quality between task versions
summary: Compare a candidate context strategy against a comparable baseline with weighted quality, safety gates, and a tamper-evident decision receipt.
tags:
  - context
  - benchmark
  - evaluation
  - agents
sources:
  - .llm-wiki/tools/Manage-LlmWikiContextBenchmark.ps1
  - .llm-wiki/tools/Manage-LlmWikiContextBudget.ps1
  - .llm-wiki/policies/workspace-policies.json
---

# Benchmark context quality between task versions

Both workspaces need verified `context-bundle.json` and `context-budget.json`
artifacts. Compare without writing:

```powershell
./.llm-wiki/wiki.ps1 task-context-benchmark `
  -SourceWorkspacePath .artifacts/llm-wiki/tasks/<baseline> `
  -WorkspacePath .artifacts/llm-wiki/tasks/<candidate> `
  -FailOnRegression
```

The benchmark first measures whether the tasks are comparable from changed paths,
modules, and scopes. It then calculates a weighted quality score from mandatory
coverage, discovered-relevance coverage, low truncation, content yield, source
diversity, and budget fit. Character count and relevance density remain visible
as diagnostic deltas instead of being mistaken for quality by themselves.

Safety gates take precedence over the aggregate score:

- mandatory coverage cannot regress beyond policy tolerance;
- security findings cannot increase;
- quarantined prompt-injection matches cannot increase;
- tasks below the comparability threshold cannot claim an improvement.

Persist the comparison on the candidate:

```powershell
./.llm-wiki/wiki.ps1 task-context-benchmark-create `
  -SourceWorkspacePath .artifacts/llm-wiki/tasks/<baseline> `
  -WorkspacePath .artifacts/llm-wiki/tasks/<candidate>

./.llm-wiki/wiki.ps1 task-context-benchmark-verify `
  -WorkspacePath .artifacts/llm-wiki/tasks/<candidate> `
  -FailOnInvalid `
  -FailOnRegression
```

The receipt binds both packet fingerprints, both bundles, both budget receipts,
the policy, the implementation, all component scores, gates, deltas, and verdict.
Any drift invalidates the result.
