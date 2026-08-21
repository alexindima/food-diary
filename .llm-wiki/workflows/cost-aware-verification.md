---
id: workflow-cost-aware-verification
kind: workflow
status: current
title: Prioritize verification by expected engineering cost
summary: Forecast verification and repair time, calibrate it against outcomes, and order required checks by expected value.
sources:
  - .llm-wiki/tools/Manage-LlmWikiVerificationCost.ps1
  - .llm-wiki/tools/Manage-LlmWikiVerificationPlan.ps1
  - .llm-wiki/policies/workspace-policies.json
---

# Prioritize verification by expected engineering cost

Use cost forecasting when several required checks compete for execution order.
The forecast combines each check's predicted failure probability with policy-backed
verification and likely repair durations. The planner moves checks with higher
expected failure cost and value density earlier without skipping required coverage.

```powershell
./.llm-wiki/wiki.ps1 task-cost-assess -WorkspacePath .artifacts/llm-wiki/tasks/<name>
./.llm-wiki/wiki.ps1 task-cost-forecast -WorkspacePath .artifacts/llm-wiki/tasks/<name>
./.llm-wiki/wiki.ps1 task-cost-verify -WorkspacePath .artifacts/llm-wiki/tasks/<name> -FailOnInvalid
./.llm-wiki/wiki.ps1 task-verification-plan -WorkspacePath .artifacts/llm-wiki/tasks/<name>
```

`verification-cost.json` is an immutable, hash-bound forecast. It records expected
verification time, expected repair time, value density, and a bounded priority
boost for every predicted check. Calibration compares the original estimate with
later evidence duration and repair-attempt elapsed time; it never rewrites history.
Verification always recomputes the receipt hash even when it has already found a
schema or arithmetic defect, so compound tampering reports integrity drift instead
of allowing an earlier validation issue to mask it.

The model is policy-backed rather than learned silently. Update durations and boost
limits in `workspace-policies.json`, then recreate affected derived artifacts.

Once enough real runs exist, [verification telemetry](verification-telemetry.md)
blends their median duration into this model and contributes a bounded historical
failure signal.
