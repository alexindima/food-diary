---
id: workflow-task-dispatch-receipts
kind: workflow
title: Task dispatch receipts
summary: Records actual scheduler assignments as tamper-evident lifecycle receipts coupled to task leases and packet fingerprints.
status: current
owners:
  - platform
tags:
  - workflow
  - task
  - scheduler
  - dispatch
  - lease
sources:
  - .llm-wiki/tools/Manage-LlmWikiTaskDispatch.ps1
  - .llm-wiki/tools/Manage-LlmWikiTaskLease.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskSchedule.ps1
  - .llm-wiki/tools/Get-LlmWikiDispatchMetrics.ps1
  - .llm-wiki/tools/Manage-LlmWikiDispatchMetricsSnapshot.ps1
  - .llm-wiki/tools/Manage-LlmWikiDispatchWatchdog.ps1
  - .llm-wiki/wiki.ps1
---

# Task dispatch receipts

The scheduler proposes work; a dispatch receipt records that an agent actually
accepted it. `task-dispatch-start` atomically acquires a task lease and writes a
receipt containing the workspace, owner, lane, lease identifier, and current
packet fingerprint.

Router changes affecting these commands are covered by `wiki verify-full`;
the faster `wiki verify` gate intentionally omits the exhaustive tool-smoke
scenarios while retaining lint and its regression fixtures, index, eval,
policy, and impact checks.
The router's pre-diff `-ProposedPath` forwarding is limited to `brief` and
`test-plan`; dispatch lifecycle inputs and receipt fingerprints are unchanged.
Scoped verification, the `verify -Fast` alias, observed verification progress,
and trace presentation flags are independent facade options; they do not enter
dispatch payloads, leases, or receipt fingerprints.
Use `wiki smoke -SmokeGroup tools` to run the stateful dispatch lifecycle
directly; full verification runs that group alongside independent index checks.
The unified router preserves ISO timestamps as JSON strings before dispatching
commands so receipt hashes do not depend on the installed PowerShell version.

Every lifecycle event (`started`, `heartbeat`, `completed`, or `failed`) includes
the previous event hash. Verification recomputes the chain, rejects mutation,
and requires a single terminal event at the end.

An active receipt is classified as:

- `running` when its lease is active and its packet fingerprint still matches;
- `orphaned` when the receipt remains active but its lease is gone or expired;
- `packet-drift` when task context changed after dispatch;
- `invalid` when the receipt or event chain is malformed.

Use `task-dispatch-list -FailOnInvalid` as an operational audit. Close successful
work with `task-dispatch-complete`; close abandoned or unsuccessful work with
`task-dispatch-fail`. Terminal commands release an active lease.

Dispatch state is also projected into:

- `task-list`, including the active state and history count;
- `task-schedule`, where running work consumes capacity and unhealthy dispatches
  block reassignment;
- `task-audit`, which reports `orphaned-dispatch` and `dispatch-drift` with
  remediation commands;
- `task-handoff`, including the receipt identifier, event count, and chain head.

## Reconciliation and retention

`task-dispatch-reconcile` is a dry-run by default. It selects only valid active
receipts classified as `orphaned` or `packet-drift`. `-Apply` appends a
hash-chained `failed` event marked as reconciled and releases any surviving
lease. Invalid receipts are never rewritten automatically.

`task-dispatch-prune` is also a dry-run by default. It selects only valid,
terminal receipts older than `scheduler.terminalReceiptRetentionDays`.
`-RetentionDays` can override that value within the policy maximum. `-Apply`
deletes exactly the reported candidates; active, unhealthy, invalid, and recent
receipts are retained.

## Operational metrics

`task-dispatch-metrics` derives metrics directly from valid retained receipts
within the configured time window. It reports terminal throughput, success and
reconciliation rates, heartbeat coverage, average/p50/p95/maximum duration,
daily terminal counts, per-owner and per-capability reliability, agent
profiles, and categorized failure causes. Current orphaned, drifted, and
invalid receipts contribute to `attentionCount`; use `-FailOnAttention` as an
operational gate.

The workspace policy defines dispatch SLO thresholds for minimum terminal
samples, success rate, heartbeat coverage, reconciliation rate, and p95
duration. Before the minimum sample count is reached the verdict is
`insufficient-data`; otherwise it is `healthy` or `degraded`. Every violation
contains its actual value, comparison operator, threshold, and remediation.

The scheduler includes a compact metrics summary, task audit embeds the full
metrics object, and handoff shows both global rates and the current dispatch
owner's reliability when history is available.

Metrics do not maintain a second mutable store. Pruning intentionally bounds
their historical horizon, so retention should be at least as long as the
default metrics window when long-term comparisons are required.

## Metrics snapshots and regression gates

`task-dispatch-snapshot-save` persists the current metrics with both a metrics
hash and an envelope hash, plus the active policy fingerprint. `verify` detects
payload or envelope mutation. `compare` uses an explicit snapshot or the newest
valid baseline and reports point/percentage deltas for success, heartbeat,
reconciliation, p95 duration, and throughput.

Comparison verdicts are `no-baseline`, `invalid-baseline`,
`insufficient-data`, `stable`, or `regressed`. Regression thresholds and the
minimum comparable sample count live in workspace policy.
`-FailOnRegression` provides a CI/operations gate. Snapshot pruning is dry-run
by default and retains the newest policy-configured count.
