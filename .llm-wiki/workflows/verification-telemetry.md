---
id: workflow-verification-telemetry
kind: workflow
status: current
title: Learn from verification outcomes
summary: Preserve integrity-protected check outcomes and durations, detect flaky checks, and improve future failure and cost forecasts.
sources:
  - .llm-wiki/tools/Manage-LlmWikiVerificationTelemetry.ps1
  - .llm-wiki/tools/Invoke-LlmWikiTaskChecks.ps1
  - .llm-wiki/tools/Manage-LlmWikiFailurePrediction.ps1
  - .llm-wiki/tools/Manage-LlmWikiVerificationCost.ps1
  - .llm-wiki/knowledge/verification-telemetry.json
  - .llm-wiki/policies/workspace-policies.json
---

# Learn from verification outcomes

Every real task-check execution appends its outcome and elapsed time to
`verification-telemetry.json`. Events bind the task packet, check id, command,
workspace policy, and previous event hash. Dry runs never create telemetry.

```powershell
./.llm-wiki/wiki.ps1 verification-telemetry-metrics
./.llm-wiki/wiki.ps1 verification-telemetry-metrics -CheckId architecture-tests
./.llm-wiki/wiki.ps1 verification-telemetry-verify -FailOnInvalid
```

A check is reported as flaky only after the configured minimum sample count, when
both passing and failing outcomes exist and the transition rate crosses the policy
threshold. Flakiness is surfaced for investigation; it does not excuse or suppress
a required check.

Failure prediction uses historical failure rate as a bounded signal. Cost
forecasting blends the policy duration with the historical median only after enough
samples exist. Forecast receipts bind the telemetry registry hash, preserving the
exact historical basis used at creation time.
