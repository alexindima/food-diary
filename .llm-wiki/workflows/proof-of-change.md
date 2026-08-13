---
id: workflow-proof-of-change
kind: workflow
status: current
title: Prove acceptance criteria from the implemented change
summary: Bind every satisfied acceptance criterion to actual changed paths and verified evidence before task completion.
tags:
  - workflow
  - acceptance
  - evidence
  - traceability
sources:
  - .llm-wiki/tools/Manage-LlmWikiProofOfChange.ps1
  - .llm-wiki/tools/Manage-LlmWikiAcceptanceMatrix.ps1
  - .llm-wiki/tools/Manage-LlmWikiTaskWorkspace.ps1
  - .llm-wiki/tools/Complete-LlmWikiTaskWorkspace.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskAudit.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskHandoff.ps1
  - .llm-wiki/policies/workspace-policies.json
---

# Prove acceptance criteria from the implemented change

Map a criterion to the files that implement it and to its verification:

```powershell
./.llm-wiki/wiki.ps1 acceptance-map `
  -AcceptancePath .artifacts/llm-wiki/tasks/<name>/acceptance-matrix.json `
  -CriterionId AC-001 `
  -ChangedPath FoodDiary.Application/Feature/Handler.cs `
  -CheckId architecture-tests
```

Changed paths must be present in the task packet. Once no criterion is pending,
the proof assessment requires each satisfied criterion to reference at least
one current changed path. It also checks that mapped test paths exist and that
the criterion has verified evidence: a passed check, a completed review, or an
explicit evidence note.

A Git-confirmed rename destination is equivalent to its source path for this
check. The acceptance matrix keeps the `from`/`to` provenance, so extraction
tasks can prove the new project path without weakening the current-diff rule.

```powershell
./.llm-wiki/wiki.ps1 task-proof-assess `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name> `
  -FailOnInvalid
```

Draft workspaces report `applicable=false` while criteria remain pending. This
keeps planning and scheduling incremental. When acceptance becomes terminal,
missing links, paths outside the task diff, missing test files, rejected
criteria, and absent verification block readiness.

Task completion seals `proof-of-change.json` automatically. The receipt binds
the acceptance matrix, evidence bundle, manifest, change packet, workspace
policy, per-criterion classification, findings, and verdict:

```powershell
./.llm-wiki/wiki.ps1 task-proof-seal `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name> `
  -FailOnInvalid

./.llm-wiki/wiki.ps1 task-proof-verify `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name> `
  -FailOnInvalid
```

Any later change to the requirements, evidence, implementation scope, packet,
or policy invalidates the receipt. Status, audit, and handoff expose the same
criterion-level proof so another AI agent can continue without trusting an
unsupported completion claim.
