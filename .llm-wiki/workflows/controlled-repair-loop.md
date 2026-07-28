---
id: workflow-controlled-repair-loop
kind: workflow
status: current
title: Run a controlled repair loop
summary: Turn failed verification into bounded, non-repeating repair attempts that require fresh passing evidence.
tags:
  - workflow
  - repair
  - verification
  - self-healing
sources:
  - .llm-wiki/tools/Manage-LlmWikiRepairLoop.ps1
  - .llm-wiki/tools/Manage-LlmWikiRepairLearning.ps1
  - .llm-wiki/knowledge/repair-learnings.json
  - .llm-wiki/tools/Manage-LlmWikiTaskWorkspace.ps1
  - .llm-wiki/tools/Complete-LlmWikiTaskWorkspace.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskAudit.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskHandoff.ps1
  - .llm-wiki/policies/workspace-policies.json
---

# Run a controlled repair loop

When a workspace evidence check is failed, request a categorized repair
suggestion:

```powershell
./.llm-wiki/wiki.ps1 task-repair-suggest `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name> `
  -CheckId architecture-tests `
  -Format Json
```

Start an attempt with an explicit hypothesis, owner, and bounded set of paths:

```powershell
./.llm-wiki/wiki.ps1 task-repair-start `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name> `
  -CheckId architecture-tests `
  -RepairHypothesis 'The dependency boundary is crossed by the new adapter.' `
  -RepairPath FoodDiary.Integrations/Feature/Adapter.cs `
  -Owner agent-name
```

The repair scope must already belong to the task plan. Workspace policy limits
the paths per attempt, attempts per failed check, total attempts, and repeated
attempt fingerprints. Only one attempt can be active, preventing concurrent
repair loops from racing over the same workspace.

After changing the implementation, record fresh evidence. A successful repair
can be completed only when its original check is now `passed` and the evidence
artifact changed after the attempt began:

```powershell
./.llm-wiki/wiki.ps1 task-repair-complete `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name> `
  -RepairAttemptId <id> `
  -Resolution 'Moved the dependency behind the approved client boundary.'
```

Use `task-repair-fail` when the hypothesis was wrong. The same normalized
hypothesis and path set cannot be retried; the next attempt must materially
change. `task-repair-verify` checks the hash chain and registry fingerprint.
Task status, audit, handoff, and completion all expose or enforce unresolved
repair state. If a registry exists, completion seals it with the other task
artifacts.

## Learn from proven repairs

A completed attempt becomes a scored candidate only when it is bound to fresh
passing evidence. Prior failed hypotheses add useful confidence because they
show that the final repair was selected after discriminating alternatives:

```powershell
./.llm-wiki/wiki.ps1 repair-learning-candidates `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name> `
  -Format Json
```

Promotion is explicit and records the owner, source workspace, terminal
attempt hash, proof lineage, confidence, policy fingerprint, and a chained
event hash:

```powershell
./.llm-wiki/wiki.ps1 repair-learning-promote `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name> `
  -RepairCandidateId <id> `
  -Owner agent-name
```

Equivalent candidates cannot be promoted twice. Future repair suggestions
reuse only promoted learnings that match the failed check, category, and
planned paths. The durable learning registry is independently hash-chained
and is validated by the repository task audit.
