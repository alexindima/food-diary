# Backend Event Taxonomy

This document defines how backend events and side effects should be modeled.

## Domain Event

A domain event is a fact raised by the domain model inside the current transaction. Domain event handlers may create more transactional state, including notifications and outbox records, but they must not call external transports directly.

Examples: `RecommendationCreatedDomainEvent`, `UserDeletedDomainEvent`.

## Integration Event

An integration event is a committed application fact intended for another process, service, or provider workflow. Integration events are represented by `IIntegrationEvent` and should be persisted through durable outbox state before delivery.

Use integration events when another service/process must eventually observe a committed fact.

## Outbox Message

An outbox message is the durable delivery record used by infrastructure processors. It owns retry, lease, telemetry, and dead-letter state. Current concrete outboxes cover email delivery, image object deletion, and notification web-push delivery.

## Post-Commit Action

A post-commit action is an in-memory best-effort callback after a successful commit. It is suitable for live UI hints such as unread-count refreshes. It is not durable delivery and must not be used for critical email, storage, billing, provider, audit, or integration work.

## Current Side-Effect Audit

- Business email delivery is written through `IEmailOutbox`.
- Notification web-push delivery is written through `INotificationWebPushOutbox`.
- Image object deletion is written through `IImageObjectDeletionOutbox`, including orphan cleanup.
- Domain event handlers currently only create transactional notification state and best-effort live refresh hints.

## Outbox Delivery Semantics

The shared infrastructure outbox is deliberately **at least once**. A worker claims and finalizes one message at a time. The configured lease must cover the per-message dispatch deadline, the server-owned database-finalization deadline, and the validation safety margin. Caller cancellation during dispatch leaves the claim for lease recovery without incrementing delivery attempts; once dispatch succeeds, finalization no longer depends on the caller token.

A process or database failure after an external side effect but before finalization can still cause a replay. Each current consumer therefore owns an explicit replay strategy:

- Email passes `fooddiary-email-outbox:{outbox-id}` to MailRelay as its idempotency key. MailRelay deduplicates enqueue and gives the queued delivery a deterministic `Message-Id`.
- Web push uses the durable notification ID as the browser notification `tag`, so a replay replaces the same visible notification instead of creating another one.
- Image object deletion relies on the idempotent S3 delete operation: deleting an already absent key remains successful.
- Achievement evaluation recalculates current state and grants through the unique `(UserId, AchievementKey)` boundary with `ON CONFLICT DO NOTHING`.

The worker records claimed, reclaimed, processed, retried, dead-lettered, and dispatch-timeout outcomes. Error persistence and ordinary logs keep only stable exception classification; provider messages and payload contents are not copied into outbox diagnostics.

## Executable Governance

- Every concrete domain event lives in `FoodDiary.Domain/Events`, is a sealed immutable `*DomainEvent`, exposes `OccurredOnUtc`, and must be raised by domain source code. Declared-but-never-raised events fail architecture tests.
- Domain events may reference domain primitives, IDs, enums and value objects, but not Application, Infrastructure, EF, HTTP or serializer types.
- Concrete integration events must live in an `Events` folder and use the `*IntegrationEvent` suffix. They are cross-process contracts, not aliases for telemetry records, webhook payloads or database entities.
- A domain event handler participates in the source transaction. Durable provider work must create an outbox record; best-effort client refresh is queued as a post-commit action.

`RecommendationCreatedDomainEvent` follows this lifecycle: the Recommendation aggregate raises the fact, `IUnitOfWork` dispatches pending events before the EF save, the transactional handler adds notification/outbox state to that same save, and the unread-count refresh is queued only after commit. The EF interceptor remains a fallback for direct persistence workflows. The command handler does not duplicate that side effect.
