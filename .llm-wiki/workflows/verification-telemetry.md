---
id: workflow-verification-telemetry
kind: workflow
status: current
title: Learn from verification outcomes
summary: Preserve integrity-protected check outcomes and durations, detect flaky checks, and improve future failure and cost forecasts.
sources:
  - .llm-wiki/tools/Manage-LlmWikiVerificationTelemetry.ps1
  - .llm-wiki/tools/Test-LlmWikiOperationalTelemetry.ps1
  - .llm-wiki/tools/Invoke-LlmWikiTaskChecks.ps1
  - .llm-wiki/tools/Manage-LlmWikiFailurePrediction.ps1
  - .llm-wiki/tools/Manage-LlmWikiVerificationCost.ps1
  - .llm-wiki/policies/workspace-policies.json
---

# Learn from verification outcomes

Every real task-check execution appends its outcome and elapsed time to the
worktree-local `.git/llm-wiki/verification-telemetry.json` registry. Observable
`wiki verify` stages record repository-level `@wiki` events, while governed task
checks bind the task packet. Repository-level events bind the exact stage-input
fingerprint already used by resumable verification. Events also bind check id,
command, workspace policy, and previous event hash. Concurrent writers serialize
through a registry lock.
Dry runs never create telemetry. The registry is operational state:
it is initialized on demand and never appears in the repository diff. Tests may
isolate it with `LLM_WIKI_VERIFICATION_TELEMETRY_PATH`.

```powershell
./.llm-wiki/wiki.ps1 verification-telemetry-metrics
./.llm-wiki/wiki.ps1 verification-telemetry-metrics -CheckId architecture-tests
./.llm-wiki/wiki.ps1 verification-telemetry-verify -FailOnInvalid
```

A check is reported as flaky only when the same input, command, and policy cohort
has the configured minimum sample count, both passing and failing outcomes, and a
transition rate above the policy threshold. Legacy events without an input
fingerprint remain visible in totals but are not comparable flakiness evidence.
`action-required` records an expected workflow pause, such as a stale index or
missing source-review receipt; it is counted separately and excluded from success,
failure, and transition rates. Flakiness is surfaced for investigation; it does
not excuse or suppress a required check.

An empty registry reports `health=insufficient-data`; it is never presented as a
healthy history. Workflow metrics likewise retain failed, timed-out, and
interrupted research/verify runs instead of deriving a success rate from passed
runs only.

Failure prediction uses historical failure rate as a bounded signal. Cost
forecasting blends the policy duration with the historical median only after enough
samples exist. Forecast receipts bind the telemetry registry hash, preserving the
exact historical basis used at creation time.
