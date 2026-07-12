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

## Executable Governance

- Every concrete domain event lives in `FoodDiary.Domain/Events`, is a sealed immutable `*DomainEvent`, exposes `OccurredOnUtc`, and must be raised by domain source code. Declared-but-never-raised events fail architecture tests.
- Domain events may reference domain primitives, IDs, enums and value objects, but not Application, Infrastructure, EF, HTTP or serializer types.
- Concrete integration events must live in an `Events` folder and use the `*IntegrationEvent` suffix. They are cross-process contracts, not aliases for telemetry records, webhook payloads or database entities.
- A domain event handler participates in the source transaction. Durable provider work must create an outbox record; best-effort client refresh is queued as a post-commit action.

`RecommendationCreatedDomainEvent` follows this lifecycle: the Recommendation aggregate raises the fact, the transactional handler creates notification/outbox state, and the unread-count refresh is queued only after commit. The command handler does not duplicate that side effect.
