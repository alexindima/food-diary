# Backend Architecture Improvement Roadmap

This roadmap captures the current backend architecture direction. It is intentionally practical: each item should reduce production risk or make future feature work easier.

## Current Baseline

The primary backend is a modular monolith with strict project boundaries:

- `FoodDiary.Domain` owns domain model and invariants.
- independently compiled `FoodDiary.Application.<Feature>` modules own use cases and business workflows.
- `FoodDiary.Application.Runtime` owns mediator, transaction, and post-commit pipeline behavior.
- `FoodDiary.Infrastructure` owns EF Core persistence and technical implementations.
- `FoodDiary.Integrations` owns external provider adapters and service-client bridges.
- `FoodDiary.Presentation.Api` owns HTTP and SignalR transport.
- `FoodDiary.Web.Api` is the executable host and composition root.

MailRelay and MailInbox are separate bounded contexts. The primary core talks to them only through client packages from `FoodDiary.Integrations`.

## Completed Baseline Improvements

The backend now has first-pass guardrails for the reliability split:

- post-commit actions are named, documented as best-effort only, bounded by queue depth and a total execution budget, and expose bounded outcome telemetry,
- mediator notification handlers execute sequentially in registration order so transactional handlers never share one scoped `DbContext` concurrently,
- infrastructure outbox processors share one processing engine for claim/lease, retry/dead-letter policy, persistence, and telemetry,
- outbox claiming uses an explicit `IOutboxMessage` contract instead of reflection,
- event taxonomy is documented with `IIntegrationEvent` for committed cross-process facts,
- JobManager jobs use `JobExecutionObserver` for execution state, metrics, and duration recording.

The backend also has structural guardrails for the main ownership boundaries:

- `FoodDiary.Application.Abstractions` keeps feature contracts in feature folders and does not place contracts in the project root.
- Each `FoodDiary.Application.<Feature>` project keeps source in use-case purpose folders and owns only its feature registration.
- `FoodDiary.Application.Runtime` stays limited to cross-cutting mediator execution and does not aggregate feature modules.
- `FoodDiary.Infrastructure` and `FoodDiary.Integrations` keep root folders limited to technical implementation and provider-adapter areas.
- `FoodDiary.Presentation.Api` keeps HTTP controllers thin by limiting feature purpose folders and controller constructor dependencies.
- `FoodDiary.Web.Api` and `FoodDiary.JobManager` keep executable-host code out of project roots except `Program.cs`.
- Cross-module Application reads use semantic owner APIs or consumer-owned ports; no module acquires another module's repository.
- The executable module manifest exactly matches source dependencies, rejects undeclared edges, and has no dependency cycles.
- Direct `FoodDiaryDbContext` acquisition is confined to Infrastructure persistence adapters, migrations/design-time support, and the persistence composition root.
- Marketing and Billing are physically extracted application modules and are registered explicitly by executable composition roots.
- Marketing no longer references the core Application assembly; shared transaction semantics flow through abstraction-level command markers.
- Dietologist attention signals use a dedicated multi-client projection instead of rebuilding dashboard snapshots per client.
- Dietologist client-task, recommendation-template, comment and bulk-dispatch workflows expose narrow read/write/read-model ports to Application code.
- Domain event declarations are immutable, transport-agnostic, and verified to be raised by domain code; integration-event naming and placement are guarded separately.
- Domain events remain attached until persistence succeeds, and post-commit actions have bounded per-action and total flush timeouts.
- Dead-letter replay is operator-driven, requires actor/reason metadata, and writes a durable audit record.
- HTTP idempotency uses owner fencing; Redis response completion is an atomic compare-and-set so stale requests cannot overwrite a newer owner.
- Personal-data export, retention, and purge guarantees are captured in `PERSONAL_DATA_LIFECYCLE.md`.
- Distributed traces cover HTTP, outbound providers, and PostgreSQL while route cardinality, sensitive attributes, 14-day retention, and operator access are governed by `BACKEND_OBSERVABILITY_BASELINE.md`.
- MailInbox bounds SMTP connections, sessions, senders, recipients, MIME complexity, parsing concurrency, and daily storage; raw MIME is byte-preserving, SMTP retries are durably deduplicated, and content/metadata retention is enforced by a bounded worker as documented in `MAILINBOX_DATA_LIFECYCLE.md`.
- NuGet restore clears inherited machine feeds, maps packages to the repository allowlist, and enforces committed dependency graphs in CI and container builds; external container inputs and production application references are immutable digests with automated update coverage.

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

- configurable per-message lease/claim and bounded dispatch/finalization deadlines,
- immediate per-message progress persistence rather than one batch-final commit,
- retry with explicit backoff,
- terminal failure state or poison-message handling,
- structured privacy-safe logs with message ids and provider names,
- explicit replay/idempotency semantics for every external consumer,
- metrics for claimed, reclaimed, processed, timed out, retried, and dead-lettered messages.

Avoid adding one-off processor behavior unless the provider truly requires it.

## Priority 4: Keep JobManager Thin

`FoodDiary.JobManager` is a worker host and scheduler. It should register jobs and call owning application-module or infrastructure services, but business decisions should stay in `FoodDiary.Application.<Feature>` modules.

Jobs should be idempotent where possible. Re-running a job after a crash or timeout should not create duplicate user-visible state.

Keep job classes under `FoodDiary.JobManager/Services`. The project root should remain a host entrypoint only, and jobs should not call MediatR, EF `DbContext`, or HTTP presentation code directly.

## Priority 5: Continue Feature-First Migration

Keep reducing global shared areas. `FoodDiary.Application.Runtime/Common` stays limited to cross-cutting pipeline and post-commit runtime behavior. Feature-specific models, services, mappings, and helper policies live in their owning application module.

Do not add new legacy flat folders. New backend work should follow the feature-first layout immediately.

Within `FoodDiary.Application.<Feature>`, use the established purpose folders: `Commands`, `Queries`, `Services`, `Mappings`, `Models`, `Validators`, `EventHandlers`, `SearchSuggestions`, and feature-local `Common`.

## Guardrail Direction

When making these changes, update architecture tests alongside implementation. The tests should prevent regressions in:

- project references,
- direct external side effects from domain event handlers,
- accidental use of post-commit queue for critical delivery,
- JobManager taking on business orchestration,
- feature-specific contracts moving back into global common folders.
