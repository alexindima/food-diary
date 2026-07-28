---
id: workflow-similar-task-plan-reuse
kind: workflow
status: current
title: Reuse verified task experience without reusing stale verification
summary: Cluster sealed tasks by deterministic change profiles and transfer implementation experience through policy, risk, lineage, and drift gates.
sources:
  - .llm-wiki/tools/Manage-LlmWikiTaskSimilarity.ps1
  - .llm-wiki/tools/Get-LlmWikiImplementationPlan.ps1
  - .llm-wiki/tools/Manage-LlmWikiVerificationPlan.ps1
  - .llm-wiki/tools/Complete-LlmWikiTaskWorkspace.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskAudit.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskHandoff.ps1
  - .llm-wiki/policies/workspace-policies.json
---

# Similar Task Plan Reuse

Completed tasks contain more reusable knowledge than individual check logs. Their change packet preserves the implementation plan, affected modules and scopes, matched policy rules, required checks, path areas, and final evidence.

## Similarity model

Each workspace receives a deterministic in-memory profile with:

- direct and downstream modules;
- change scopes;
- matched policy rules;
- required checks;
- normalized path areas;
- risk score and current workspace-policy fingerprint.

Weighted Jaccard similarity produces a score from 0 to 100. A stable cluster key groups tasks with the same module, scope, and rule shape, while ranked similarity can still find useful neighbors outside the exact cluster.

```powershell
./.llm-wiki/wiki.ps1 task-similarity-profile -WorkspacePath .artifacts/llm-wiki/tasks/<target>
./.llm-wiki/wiki.ps1 task-similarity-find -WorkspacePath .artifacts/llm-wiki/tasks/<target>
./.llm-wiki/wiki.ps1 task-similarity-clusters
```

## Governed reuse

```powershell
./.llm-wiki/wiki.ps1 task-similarity-reuse `
  -WorkspacePath .artifacts/llm-wiki/tasks/<target> `
  -SourceWorkspacePath .artifacts/llm-wiki/tasks/<sealed-source> `
  -DryRun

./.llm-wiki/wiki.ps1 task-similarity-reuse `
  -WorkspacePath .artifacts/llm-wiki/tasks/<target> `
  -SourceWorkspacePath .artifacts/llm-wiki/tasks/<sealed-source>
```

Reuse requires:

- a valid unsealed target and valid sealed source;
- similarity above the configured reuse threshold;
- risk-score delta within policy;
- identical current workspace-policy fingerprints;
- intact source completion lineage.

The resulting `plan-reuse.json` snapshots both profiles, the source completion fingerprint, component scores, check drift, the target implementation plan, source implementation experience, and resolved source-check outcomes.

Verification is never copied. The target risk calibration, failure prediction, cost forecast, coverage, commands, and verification plan are regenerated canonically for the target. The receipt links that new plan to the source experience and becomes invalid if either task profile, source completion, implementation plan, or target verification plan drifts.

Audit and handoff surface the receipt, and task completion seals it when present.
