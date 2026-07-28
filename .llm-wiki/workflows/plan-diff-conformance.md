---
id: workflow-plan-diff-conformance
kind: workflow
status: current
title: Verify implementation plan against the actual diff
summary: Classify every changed path against the declared plan and block AI task completion when implementation drifts from intent.
tags:
  - workflow
  - conformance
  - scope
  - verification
sources:
  - .llm-wiki/tools/Manage-LlmWikiPlanConformance.ps1
  - .llm-wiki/tools/Manage-LlmWikiChangeManifest.ps1
  - .llm-wiki/tools/Manage-LlmWikiTaskWorkspace.ps1
  - .llm-wiki/tools/Complete-LlmWikiTaskWorkspace.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskAudit.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskHandoff.ps1
  - .llm-wiki/policies/workspace-policies.json
---

# Verify implementation plan against the actual diff

Assess the live Git diff without writing a receipt:

```powershell
./.llm-wiki/wiki.ps1 task-conformance-assess `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name> `
  -FailOnInvalid
```

Every changed path is classified as:

- planned and changed;
- allowed by scope but not declared in the plan;
- outside the allowed task scope;
- planned but missing from the implementation.

The assessment also detects newly introduced checks and review obligations.
Policy controls how many unplanned or missing paths are tolerated. The default
is strict: any unplanned, missing, or out-of-scope path blocks readiness.

When the additional work is intentional, explicitly rebaseline it:

```powershell
./.llm-wiki/wiki.ps1 task-conformance-replan `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name> `
  -Reason <why-the-plan-changed>
```

Replanning preserves the task's allowed and excluded scope, rebuilds the
manifest and implementation-plan snapshot from the live diff, invalidates any
old conformance receipt, and records the rationale in the task journal.

Seal the current result when producing completion evidence:

```powershell
./.llm-wiki/wiki.ps1 task-conformance-seal `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name> `
  -FailOnInvalid

./.llm-wiki/wiki.ps1 task-conformance-verify `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name> `
  -FailOnInvalid
```

The receipt binds the manifest hash, implementation-plan fingerprint,
task-scoped change packet, workspace policy, diff-snapshot fingerprint,
classification, and findings. The change packet is refreshed from Git before
completion; this avoids mixing neighboring task shards from the same dirty
worktree. Any later packet or metadata change invalidates the receipt. Task
finish creates it after the final refresh and includes it in the completion
artifact hashes. Audit, status, and handoff expose the same classification.
