---
id: workflow-governed-context-strategy-application
kind: workflow
status: current
area: ai-development
title: Apply a winning context strategy with rollback
summary: Require an exact human approval, reproduce the winning experiment, validate post-apply quality and security, and retain a verified rollback path.
tags:
  - context
  - approval
  - rollback
  - agents
sources:
  - .llm-wiki/tools/Manage-LlmWikiContextStrategy.ps1
  - .llm-wiki/tools/Manage-LlmWikiContextExperiment.ps1
  - .llm-wiki/policies/workspace-policies.json
---

# Apply a winning context strategy with rollback

Preview the exact experiment recommendation:

```powershell
./.llm-wiki/wiki.ps1 task-context-strategy-preview `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name>
```

Approval is explicit, reasoned, and bound to the exact experiment hash, variant,
item limit, character budget, and current policy:

```powershell
./.llm-wiki/wiki.ps1 task-context-strategy-approve `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name> `
  -Reason "Reviewed quality and security gates"
```

Apply only the approved strategy:

```powershell
./.llm-wiki/wiki.ps1 task-context-strategy-apply `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name>
```

Before rebuilding, the tool snapshots the exact security assessment, bundle, and
budget receipt. It then recreates the recommended bundle through the normal
pipeline and performs post-apply checks:

- actual quality must reproduce the winning experiment within policy tolerance;
- security findings cannot grow beyond policy tolerance;
- quarantined prompt-injection matches cannot grow.

The application becomes `applied` when every gate passes. Otherwise it becomes
`rollback-recommended`, remains visible in audit and handoff, and cannot be
silently treated as successful.

Verify or roll back:

```powershell
./.llm-wiki/wiki.ps1 task-context-strategy-verify `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name> `
  -FailOnInvalid

./.llm-wiki/wiki.ps1 task-context-strategy-rollback `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name> `
  -Reason "Post-apply quality did not hold"
```

Rollback restores byte-identical baseline artifacts and records the rationale,
time, restored bundle hash, state, and updated application hash. Experiment and
approval receipts are consumed into the application receipt so they cannot be
reused against another strategy.
