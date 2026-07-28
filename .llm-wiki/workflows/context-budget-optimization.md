---
id: workflow-context-budget-optimization
kind: workflow
area: ai-development
status: current
title: Measure and optimize task context budgets
summary: Turn prompt size into a reproducible quality decision by measuring coverage, utilization, truncation, diversity, and relevance density.
tags:
  - context
  - budget
  - optimization
  - agents
sources:
  - .llm-wiki/tools/Manage-LlmWikiContextBudget.ps1
  - .llm-wiki/tools/Manage-LlmWikiContextBundle.ps1
  - .llm-wiki/policies/workspace-policies.json
---

# Measure and optimize task context budgets

Create the bounded context bundle first, then produce its budget receipt:

```powershell
./.llm-wiki/wiki.ps1 task-context-create -WorkspacePath .artifacts/llm-wiki/tasks/<name>
./.llm-wiki/wiki.ps1 task-context-budget-create -WorkspacePath .artifacts/llm-wiki/tasks/<name>
```

`context-budget.json` binds its conclusions to the exact task packet, context
bundle, policy, and optimizer implementation. It records:

- mandatory-context coverage;
- selected relevance versus relevance omitted by the item limit;
- character-budget utilization;
- excerpt truncation;
- source-kind diversity;
- relevance per thousand characters;
- deterministic findings and next-generation budget recommendations.

The optimizer never silently rewrites an agent prompt. A `balanced` verdict keeps
the current limits. A `tune` verdict gives explicit `suggestedItemLimit` and
`suggestedCharacterBudget` values for a reviewed regeneration. `invalid` means
mandatory context has no usable content and must be repaired before delegation.

Verify the saved decision before using it:

```powershell
./.llm-wiki/wiki.ps1 task-context-budget-verify `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name> `
  -FailOnInvalid
```

Changing the bundle, task packet, workspace policy, optimizer, metrics, findings,
or recommendations invalidates the receipt. Refreshing the task removes it
together with the other derived context artifacts.
