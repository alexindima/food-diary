---
id: workflow-risk-calibration
kind: workflow
status: current
title: Calibrate change risk before implementation
summary: Produce an explainable, integrity-protected risk forecast and use it to strengthen verification before an AI agent changes code.
tags:
  - workflow
  - risk
  - verification
  - orchestration
sources:
  - .llm-wiki/tools/Manage-LlmWikiRiskCalibration.ps1
  - .llm-wiki/tools/Manage-LlmWikiVerificationPlan.ps1
  - .llm-wiki/tools/Manage-LlmWikiQualityAdjustment.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskAudit.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskHandoff.ps1
  - .llm-wiki/policies/workspace-policies.json
---

# Calibrate change risk before implementation

Create an explainable forecast for a task workspace:

```powershell
./.llm-wiki/wiki.ps1 task-risk-calibrate `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name>

./.llm-wiki/wiki.ps1 task-risk-verify `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name> `
  -FailOnInvalid
```

The score combines the semantic risk already discovered in the change packet
with change breadth, cross-scope impact, API and database boundaries, mandatory
security review, and the current history of post-completion rework, rollback,
and regression. Every signal records its points and source evidence.

The receipt is bound to the packet fingerprint and workspace policy. It records
the quality-adjustment-history fingerprint used at creation, while historical
pressure remains an immutable snapshot for the lifetime of that plan. New
history is consumed on the next calibration rather than changing an active
task underneath its agent. The payload is hash protected; packet, policy,
signal, control, or score drift invalidates it.

Verification-plan creation refreshes the calibration automatically. At high
risk it reruns checks even when old passing evidence exists. At critical risk
it creates an exhaustive plan: check supersedence and command deduplication are
disabled, so every required policy check is executed independently. The plan
stores the calibration hash, risk score, level, and selected execution mode.

Audit reports invalid calibration as attention. Handoff includes the complete
forecast so the next agent can see why verification was strengthened.

Scheduler also consumes the calibrated level, falling back to the packet risk
before a receipt exists. High- and critical-risk work adds a policy-controlled
reliability blend based on proven success and post-verification quality. This
preferentially assigns risky changes to agents with stronger outcomes without
blocking cold-start agents when no history exists.
