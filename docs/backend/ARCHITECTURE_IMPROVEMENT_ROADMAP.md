# Backend architecture improvement roadmap

FoodDiary is a modular monolith with independently deployed MailRelay and MailInbox.
Current ownership is described in [the architecture guide](../ARCHITECTURE.md) and
[the module map](../BACKEND_MODULE_MAP.md). Projects use FoodDiary.Modules.<Module>.<Layer>;
shared technical libraries live under Shared. Historical plans remain in Git history.

## Reliability baseline

[ADR 0052](../adr/0052-backend-transaction-and-delivery-reliability.md) defines the protocol:

- Uncertain COMMIT is verified with PostgreSQL transaction status. Unknown outcomes
  never replay a handler automatically.
- CreateMeal and RepeatMeal save durable results with business writes. Redis remains
  the HTTP lease/cache. Other flows opt in explicitly or use existing owner receipts.
- The outer command owns saving and cleanup; ignored nested failure cannot commit.
- MailRelay renews and fences claims by attempt. SMTP remains at least once across crashes.
- WebPush failures reach outbox retry; completed subscriptions are checkpointed.
- Users account, preferences and nutrition have separate tables, xmin and audit
  timestamps. Account revocation remains a write barrier.
- Save priorities and the cutoff for participant enlistment are explicit.
- Dashboard uses a common read-only snapshot with time/query limits and metrics.
  EF reads stay sequential on the shared connection.

## Rules for new work

Hosts compose owner modules and narrow shared adapters. HTTP belongs to Presentation,
use cases to Application, rules to Domain, providers to Infrastructure.
FoodDiary.Infrastructure owns migrations/full-model composition;
Shared/FoodDiary.Persistence.Runtime owns session/transaction coordination.
Cross-module SQL reads belong to FoodDiary.ReadModel.Composition.

## Durable Side Effects

Domain handlers may create transactional state and outbox records. Critical external
work uses durable delivery. IPostCommitActionQueue provides best-effort refresh hints.
Retried handlers do not invoke external transports.

### Event Taxonomy

Domain events coordinate local transactional changes; delivery records represent
durable work for external providers. Post-commit callbacks carry refresh hints whose
loss must not affect business correctness. Keep these guarantees explicit at each owner.

### Shared Outbox Policy

The shared outbox engine owns claim fencing, bounded retries, cancellation and
dead-letter policy. Owner dispatchers report provider outcomes and retain progress.
Remote acceptance can precede the local checkpoint; consumers must tolerate delivery
repeats across this crash window.

## Keep JobManager Thin

JobManager invokes owning capabilities, including mediator service commands when
an owner contract defines them. Jobs do not acquire repositories, foreign aggregate
writes, HTTP presentation, or unreviewed persistence boundaries. JobExecutionObserver
records execution outcome and duration.

Command/query slices each have a feature folder. Reference the contract owner directly.
Update the exact dependency graph and reviewed technical persistence inventory for
new capabilities; an inventory entry never permits foreign aggregate writes.

## Further changes require evidence

Extend receipts when a failure-window test demonstrates the need. Change User ownership
further when contention measurements justify it. Optimize Dashboard using measured SQL
counts and latency. Split deployments when independent operation, load or ownership
requires it. Each change needs owner tests, API compatibility checks and a rollout plan.
