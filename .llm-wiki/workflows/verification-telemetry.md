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
./.llm-wiki/wiki.ps1 verification-telemetry-archive -Format Json
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

When the active registry reaches its retention limit, `verification-telemetry-archive`
preserves its exact bytes in `<registry>.archives/<registryHash>.json` and atomically
publishes an empty active window. Recording and archiving share the same exclusive
lock. The active registry hash binds archive descriptors and their ordered hash
chain; every read checks archived file content hashes, including same-time edits.
Missing or corrupt archives invalidate the registry instead of silently discarding
history. An interrupted archive publication can leave an unreferenced immutable
file; repeating the operation reuses it only when the bytes match.
Existing registry files publish through filesystem replacement; a first registry
uses a move to an absent path. Failed replacement retains the previous file.

`list` and `metrics` describe the active window. Their `archives`,
`archivedEventCount` and `metricsScope=active-registry` fields make this boundary
explicit; the original full histories remain available in the referenced archive
files. Archiving is explicit, never a silent overwrite at the cap. It changes the
telemetry hash and can invalidate cost/failure forecasts bound to the previous
window; it does not change passing/failed execution evidence or verification gates.
