---
id: workflow-independent-change-critique
kind: workflow
status: current
title: Independently critique an AI-authored change
summary: Re-evaluate intent, scope, proof, verification, architecture, security, and operability before an AI task can be sealed.
sources:
  - .llm-wiki/tools/Manage-LlmWikiChangeCritique.ps1
  - .llm-wiki/tools/Manage-LlmWikiTaskWorkspace.ps1
  - .llm-wiki/tools/Complete-LlmWikiTaskWorkspace.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskAudit.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskHandoff.ps1
  - .llm-wiki/policies/workspace-policies.json
---

# Independently critique an AI-authored change

The confidence ledger aggregates evidence. The independent critique acts as a
separate review pass: it re-derives findings from the current workspace rather
than accepting the task's own conclusion.

```powershell
./.llm-wiki/wiki.ps1 task-critique-assess -WorkspacePath .artifacts/llm-wiki/tasks/<name>
./.llm-wiki/wiki.ps1 task-critique-create -WorkspacePath .artifacts/llm-wiki/tasks/<name>
./.llm-wiki/wiki.ps1 task-critique-verify -WorkspacePath .artifacts/llm-wiki/tasks/<name> -FailOnInvalid
```

Every critique covers seven policy-required review areas:

- intent and requirement quality;
- declared versus observed scope;
- proof linking criteria to the change;
- checks, reviews, telemetry, and prediction quality;
- architectural impact;
- AI context security;
- repair and operational closure.

Findings have stable IDs, severity, evidence, a concrete recommendation, and a
blocking flag. Their penalties are applied to the current confidence score.
Critical findings produce `reject`; other blocking findings or insufficient
score produce `request-changes`; non-blocking risks remain visible through
`approve-with-notes`.

`change-critique.json` binds the findings, area decisions, score, verdict,
policy, telemetry, confidence ledger, and local input artifacts with hashes.
Verification independently recomputes the critique, so editing a finding,
removing an objection, or inflating the score invalidates the receipt.

Task refresh invalidates stale critique evidence. Status and audit surface its
findings, handoff carries its verdict, and completion refuses to seal
`reject` or `request-changes`.
