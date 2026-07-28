---
id: workflow-failure-prediction
kind: workflow
status: current
title: Predict and calibrate verification failures
summary: Rank checks by deterministic failure probability and measure false negatives after evidence arrives.
tags:
  - workflow
  - prediction
  - verification
  - calibration
sources:
  - .llm-wiki/tools/Manage-LlmWikiFailurePrediction.ps1
  - .llm-wiki/tools/Manage-LlmWikiRiskCalibration.ps1
  - .llm-wiki/tools/Manage-LlmWikiVerificationPlan.ps1
  - .llm-wiki/tools/Manage-LlmWikiRepairLearning.ps1
  - .llm-wiki/tools/Manage-LlmWikiRepairLoop.ps1
  - .llm-wiki/policies/workspace-policies.json
---

# Predict and calibrate verification failures

Create the forecast before running task checks:

```powershell
./.llm-wiki/wiki.ps1 task-failure-predict `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name> `
  -Format Json
```

Each required check receives a bounded probability derived from the immutable
risk calibration, change breadth, promoted repair learnings, and repair
history already known when the forecast is created. The receipt binds those
inputs, the complete prediction vector, policy fingerprint, and a prediction
hash.

The verification planner creates this forecast automatically. Checks with a
higher predicted probability receive a deterministic priority boost while
canonical commands, coverage reduction, and risk-driven execution controls
remain unchanged.

After evidence or repair attempts arrive, assess calibration:

```powershell
./.llm-wiki/wiki.ps1 task-failure-assess `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name> `
  -Format Json
```

Resolved checks are classified as true positive, false positive, false
negative, or true negative. The assessment also reports the Brier score.
Repair history counts as an observed failure even when the final check is
passing, so successful self-healing cannot erase a missed prediction.

The original forecast remains immutable as outcomes evolve. Verification
checks its source identity and internal probability arithmetic without
rewriting history. Status, audit, handoff, and completion expose or seal the
prediction; false negatives are surfaced as learning opportunities.
