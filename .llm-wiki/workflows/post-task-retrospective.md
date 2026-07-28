---
id: workflow-post-task-retrospective
kind: workflow
status: current
title: Learn from completed AI tasks
summary: Compare forecasts with sealed outcomes and produce integrity-protected learning candidates for future planning, verification, repair, cost, and context selection.
sources:
  - .llm-wiki/tools/Manage-LlmWikiRetrospective.ps1
  - .llm-wiki/tools/Complete-LlmWikiTaskWorkspace.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskAudit.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskHandoff.ps1
  - .llm-wiki/policies/workspace-policies.json
---

# Learn from completed AI tasks

Completion proves that a task passed its gates. A retrospective asks what the
system predicted incorrectly and what should change for the next similar task.

Every successful `task-finish` automatically creates `retrospective.json`.
It compares the sealed outcome with:

- risk and impact forecasts;
- failure-prediction calibration;
- expected versus actual verification cost;
- failed and completed repair attempts;
- flaky verification history;
- confidence and independent critique;
- quarantined AI context.

```powershell
./.llm-wiki/wiki.ps1 task-retrospective-show -WorkspacePath .artifacts/llm-wiki/tasks/<name>
./.llm-wiki/wiki.ps1 task-retrospective-verify -WorkspacePath .artifacts/llm-wiki/tasks/<name> -FailOnInvalid
```

The retrospective assigns an outcome quality and emits ranked learning
candidates with stable IDs, rationale, evidence, suggested tags, and a
policy-backed eligibility score. Candidates identify reusable lessons such as
missed failure risk, ineffective repair fingerprints, cost-model variance,
unexpected architectural impact, flaky checks, or unsafe context sources.

Candidates are proposals, not automatic truth. Eligible items can be reviewed
and promoted through the durable-memory workflow; weak or task-specific lessons
remain local to the retrospective.

The receipt hashes the completion, task artifacts, forecast receipts, outcome,
telemetry snapshot, and candidate set. Verification recomputes the outcome and
lessons from the sealed evidence, detecting removed failures, rewritten
rationales, or inflated learning scores.
