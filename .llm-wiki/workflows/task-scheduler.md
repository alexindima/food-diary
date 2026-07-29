---
id: workflow-task-scheduler
kind: workflow
status: current
title: Schedule concurrent AI tasks with leases
summary: Select conflict-free graph-ready tasks, enforce concurrency capacity, and prevent duplicate agent ownership.
tags:
  - workflow
  - task
  - scheduler
  - lease
  - orchestration
sources:
  - .llm-wiki/tools/Get-LlmWikiTaskSchedule.ps1
  - .llm-wiki/tools/Manage-LlmWikiTaskLease.ps1
  - .llm-wiki/tools/Manage-LlmWikiTaskDispatch.ps1
  - .llm-wiki/tools/Manage-LlmWikiAgentRegistry.ps1
  - .llm-wiki/tools/Get-LlmWikiAgentFleetCoverage.ps1
  - .llm-wiki/tools/Manage-LlmWikiSchedulePlan.ps1
  - .llm-wiki/tools/Manage-LlmWikiOrchestrationCycle.ps1
  - .llm-wiki/tools/Test-LlmWikiOrchestrationLineage.ps1
  - .llm-wiki/tools/Manage-LlmWikiDispatchWatchdog.ps1
  - .llm-wiki/tools/Manage-LlmWikiWorkspaceCircuit.ps1
  - .llm-wiki/tools/Manage-LlmWikiTaskDecomposition.ps1
  - .llm-wiki/tools/Manage-LlmWikiContextBundle.ps1
  - .llm-wiki/tools/Manage-LlmWikiContextFeedback.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskGraph.ps1
  - .llm-wiki/policies/workspace-policies.json
  - .llm-wiki/tools/Get-LlmWikiTaskWorkspaces.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskAudit.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskHandoff.ps1
---

# Schedule Concurrent AI Tasks

Compile the next safe orchestration cycle:

```powershell
./.llm-wiki/wiki.ps1 task-schedule

./.llm-wiki/wiki.ps1 task-schedule -MaxConcurrency 4 -Format Json

./.llm-wiki/wiki.ps1 task-schedule -FailOnBlocked
```

Selected lanes include `dispatchCommand`. Prefer it over acquiring a raw lease:
the command records the actual assignment, lease, owner, lane, and packet
fingerprint in a tamper-evident dispatch receipt.

Running dispatches consume scheduler capacity. Orphaned, packet-drifted, or
invalid dispatches block reassignment until they are audited and closed.

## Capability-aware routing

Agents register an owner, capability set, capacity, and expiring heartbeat.
Supported capabilities are controlled by workspace policy. The scheduler
derives task requirements from packet scopes and changed paths, then selects
only an active agent that covers every requirement and still has capacity.
`generalist` covers all requirements.

When active registrations exist, unmatched tasks become `waiting-capability`.
When the registry is empty, scheduler remains backward compatible in
`unregistered-fallback` mode and emits placeholder-owner dispatch commands.

Compatible agents are ranked by a policy-weighted score: historical success,
heartbeat coverage, duration, remaining capacity, specialization, and
fairness. Reliability is learned per owner and per required capability. Once
every capability required by a task has enough samples, the scheduler blends
those task-specific profiles with the broader owner history. Cold agents and
unseen capabilities receive a neutral score until they reach the configured
sample count.
Every selected task includes the full ranking, score components, weights, and
selected score. Explainability includes whether a capability profile was used,
its sample count, success, heartbeat, and average duration. The dispatch
receipt preserves the routing score and required capabilities so future
metrics can learn from the outcome.

`task-agent-coverage` reports capability demand from active tasks, agent supply,
total and available capacity, single-agent constraints, and exact task gaps.
Use `-FailOnGap` to gate an orchestration cycle that the current fleet cannot
cover.

## Automatic task decomposition

Large workspaces can be split into graph-native child tasks without immediately
changing scheduler state. `task-decompose-plan` groups changed paths by project
and architectural boundary, merges small groups within the configured shard
limit, infers capability demand, and calculates contract/frontend/test
prerequisites. The immutable plan covers every source path exactly once and is
bound to the parent packet and policy fingerprints:

```powershell
./.llm-wiki/wiki.ps1 task-decompose-plan -WorkspacePath .artifacts/llm-wiki/tasks/<name> -MaxShards 6
./.llm-wiki/wiki.ps1 task-decompose-verify -DecompositionId <id> -FailOnInvalid
```

Only `task-decompose-apply` creates children. Creation is all-or-rollback: if
any child fails validation, already-created children are removed and the parent
descriptor is restored. A successful apply writes a hashed application receipt,
marks the parent as decomposed, and links every child to the plan, its shard,
required capabilities, and prerequisite workspaces. The task graph excludes
the decomposed parent and adds explicit prerequisite edges between children:

```powershell
./.llm-wiki/wiki.ps1 task-decompose-apply -DecompositionId <id>
./.llm-wiki/wiki.ps1 task-graph
```

Every child receives a bounded context bundle. Dispatch receipts bind the exact
bundle hash alongside the packet fingerprint; context drift is a distinct
unhealthy state and is eligible for reconciliation.
Terminal agents can submit hashed context feedback. Aggregate metrics influence
future bundle ranking only after the policy sample threshold is reached.

## Immutable schedule plans

`task-schedule-plan-create` freezes selected workspaces, packet fingerprints,
agents, capabilities, lanes, routing scores, policy fingerprint, concurrency,
and a short expiry into an immutable hashed plan. `verify` rejects hash
mutation, expiry, policy drift, packet drift, new leases, missing agents, owner
changes, or insufficient agent capacity.

`task-schedule-plan-claim` is a dry-run by default. `-Apply` starts every
dispatch under one scheduler lock. The filesystem cannot provide a true
cross-file transaction, so a mid-batch race uses all-or-compensated semantics:
already-started dispatches receive terminal compensation events and their
leases are released. Every preview or apply writes a separately hashed claim
receipt linked to the plan hash and resulting dispatch IDs.

Every applied dispatch also links back to the claim and immutable plan through
`scheduleClaimId`, `schedulePlanId`, and `schedulePlanHash`. The values are
captured in the hashed `started` event as well as the dispatch envelope.
Run `task-orchestration-audit -FailOnInvalid` to verify plan and claim hashes,
both directions of every dispatch link, missing artifacts, illegal dry-run
dispatches, and duplicate successful claims.

## Supervisor cycle

`task-orchestrate` combines the operational sequence into one reproducible
cycle. It checks orchestration lineage and fleet coverage, previews orphan
reconciliation, creates an immutable schedule plan, claims it, performs a
postflight audit, and writes a separately hashed cycle receipt.

The command is preview-only by default:

```powershell
./.llm-wiki/wiki.ps1 task-orchestrate -MaxConcurrency 3
./.llm-wiki/wiki.ps1 task-orchestrate -MaxConcurrency 3 -Apply -FailOnAttention
```

Only `-Apply` reconciles orphaned dispatches and starts planned work. A cycle is
`idle` when no task is assignable, `preview` when work is planned without
dispatch, `dispatched` after a successful applied claim, or `blocked` when
preflight lineage or fleet coverage is invalid. Cycle receipts preserve policy,
plan, claim, dispatch, reconciliation, and pre/postflight fingerprints:

```powershell
./.llm-wiki/wiki.ps1 task-orchestration-cycle-list
./.llm-wiki/wiki.ps1 task-orchestration-cycle-verify -CycleId <id> -FailOnAttention
./.llm-wiki/wiki.ps1 task-orchestration-cycle-prune -Apply
```

Retention keeps plans, claims, and linked dispatches while a retained cycle
still references them.

## Dispatch watchdog and quarantine

Every supervisor cycle runs the dispatch watchdog before planning. The watchdog
compares the last hash-chained dispatch event with the silence threshold,
counts recent workspace and owner failures, calculates remaining retries, and
recommends quarantine after the configured agent failure threshold.

Preview and apply are explicit:

```powershell
./.llm-wiki/wiki.ps1 task-watchdog
./.llm-wiki/wiki.ps1 task-watchdog -Apply
./.llm-wiki/wiki.ps1 task-watchdog-verify -WatchdogId <id>
```

When the retry budget is exhausted, an applied watchdog run opens a hashed
workspace circuit. Open circuits are excluded from scheduling and appear in
task audit and handoff output. A circuit automatically stops blocking when its
cooldown expires or the workspace packet fingerprint changes; an operator can
also reset it explicitly after reviewing the failure:

Circuit lineage verification canonicalizes UTC timestamps before hashing.
This keeps receipts created by PowerShell 7/Linux valid when inspected by
Windows PowerShell 5, whose JSON parser materializes ISO timestamps as date
objects.

```powershell
./.llm-wiki/wiki.ps1 task-circuit-list
./.llm-wiki/wiki.ps1 task-circuit-reset -WorkspacePath .artifacts/llm-wiki/tasks/<name> -Reason "Reviewed and safe to retry"
./.llm-wiki/wiki.ps1 task-circuit-verify -CircuitId <id>
```

`-Apply` terminates silent dispatches through the normal hashed failure event
and releases their leases. Agents crossing the failure threshold are
quarantined for a policy-controlled duration. Quarantined registrations remain
visible for diagnosis but expose zero capacity and are excluded from routing.
Operators can also quarantine or restore an agent explicitly:

```powershell
./.llm-wiki/wiki.ps1 task-agent-quarantine -AgentId <id> -Owner <owner> -Reason <reason>
./.llm-wiki/wiki.ps1 task-agent-unquarantine -AgentId <id> -Owner <owner>
```

The scheduler combines task graph edges, workspace doctor results, journal
blockers, prerequisites, active leases, policy limits, and a deterministic
priority score. Tasks with exact-path conflicts, dependency cycles, invalid
artifacts, open blockers, or unmerged prerequisites are not assignable.

Ready tasks are ordered by the amount of downstream work they unlock,
contract/API sensitivity, and change-set size. The scheduler fills available
capacity after counting already leased tasks. Tasks joined by shared-boundary
or generated-artifact coordination edges are never selected in the same cycle.
Each selected item has a stable lane and an acquire command.

## Lease lifecycle

Acquire ownership before dispatching an agent:

```powershell
./.llm-wiki/wiki.ps1 task-lease-acquire `
  -WorkspacePath .artifacts/llm-wiki/tasks/fasting `
  -Owner agent-fasting `
  -LeaseMinutes 30

./.llm-wiki/wiki.ps1 task-lease-heartbeat `
  -LeaseId <lease-id> `
  -Owner agent-fasting

./.llm-wiki/wiki.ps1 task-lease-release `
  -LeaseId <lease-id> `
  -Owner agent-fasting
```

The registry uses an exclusive mutation lock and atomic file replacement.
Only one active lease may own a workspace. Heartbeat and release require the
opaque lease ID; an optional owner check prevents accidental cross-agent
release. Expired leases do not consume scheduler capacity and can be removed
with `task-lease-prune`.

Default/max concurrency and lease duration are centralized in
`.llm-wiki/policies/workspace-policies.json`. Task list, audit, and handoff
surface active lease ownership so resumed work does not silently duplicate an
agent assignment.
