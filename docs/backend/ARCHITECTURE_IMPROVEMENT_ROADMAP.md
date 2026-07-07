# Backend Architecture Improvement Roadmap

This roadmap captures the current backend architecture direction. It is intentionally practical: each item should reduce production risk or make future feature work easier.

## Current Baseline

The primary backend is a modular monolith with strict project boundaries:

- `FoodDiary.Domain` owns domain model and invariants.
- `FoodDiary.Application` owns use cases and business workflows.
- `FoodDiary.Infrastructure` owns EF Core persistence and technical implementations.
- `FoodDiary.Integrations` owns external provider adapters and service-client bridges.
- `FoodDiary.Presentation.Api` owns HTTP and SignalR transport.
- `FoodDiary.Web.Api` is the executable host and composition root.

MailRelay and MailInbox are separate bounded contexts. The primary core talks to them only through client packages from `FoodDiary.Integrations`.

## Completed Baseline Improvements

The backend now has first-pass guardrails for the reliability split:

- post-commit actions are named and documented as best-effort only,
- infrastructure outbox processors share retry/dead-letter policy and telemetry shape,
- outbox claiming uses an explicit `IOutboxMessage` contract instead of reflection,
- event taxonomy is documented with `IIntegrationEvent` for committed cross-process facts,
- JobManager jobs use `JobExecutionObserver` for execution state, metrics, and duration recording.

## Priority 1: Durable Side Effects

Critical side effects must be represented as durable state before a command completes. Use transactional outbox records for work that must eventually happen, including:

- email delivery,
- object deletion,
- billing/provider calls that cannot be safely lost,
- integration events for other processes or services,
- audit or compliance events.

`IPostCommitActionQueue` is only for best-effort real-time notifications after a successful commit. It is acceptable for SignalR refreshes or push hints where loss is tolerable because clients can recover by reloading state.

## Priority 2: Event Taxonomy

Keep these concepts separate:

- Domain event: a fact raised by the domain model inside the current transaction.
- Application integration event: a committed fact intended for another process, service, or provider workflow.
- Outbox message: the durable delivery record used to process an integration event or side effect.
- Post-commit action: best-effort in-memory callback after commit, not durable delivery.

Domain event handlers may create transactional state and outbox records. They must not call external transports directly.

## Priority 3: Shared Outbox Policy

Existing outbox processors should converge on one shared policy:

- lease/claim with bounded batch sizes,
- retry with explicit backoff,
- terminal failure state or poison-message handling,
- structured logs with message ids and provider names,
- metrics for claimed, processed, failed, retried, and dead-lettered messages.

Avoid adding one-off processor behavior unless the provider truly requires it.

## Priority 4: Keep JobManager Thin

`FoodDiary.JobManager` is a worker host and scheduler. It should register jobs and call application/infrastructure services, but business decisions should stay in `FoodDiary.Application`.

Jobs should be idempotent where possible. Re-running a job after a crash or timeout should not create duplicate user-visible state.

## Priority 5: Continue Feature-First Migration

Keep reducing global shared areas. `Application/Common` should stay limited to cross-cutting pipeline, result, validation, and mediator infrastructure. Feature-specific models, services, mappings, and helper policies should live under their feature folders.

Do not add new legacy flat folders. New backend work should follow the feature-first layout immediately.

## Guardrail Direction

When making these changes, update architecture tests alongside implementation. The tests should prevent regressions in:

- project references,
- direct external side effects from domain event handlers,
- accidental use of post-commit queue for critical delivery,
- JobManager taking on business orchestration,
- feature-specific contracts moving back into global common folders.
