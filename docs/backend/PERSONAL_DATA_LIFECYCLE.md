# Personal Data Lifecycle

## Collection and ownership

Personal data is owned by the feature that creates it. User identity and account state belong to Users; diary, body tracking, cycle, collaboration, notification, billing, wearable, and authentication records remain in their feature-owned persistence areas. New user-linked tables must declare a foreign key deletion behavior and be included in the user-cleanup integration tests.

## Export

Authenticated users can export diary data through `GET /api/v1/export/diary` and cycle data through `GET /api/v1/export/cycle`. Export handlers resolve the current user, constrain every read by that user id, and generate portable CSV/PDF data without exposing password hashes, refresh tokens, provider secrets, or protected wearable credentials.

## Account deletion and retention

Account deletion is soft first. The recurring `UserCleanupJob` permanently purges accounts after `UserCleanup:RetentionDays` (30 days by default) in bounded batches. This delay provides a recovery window and prevents a large deletion from monopolizing the database.

Independent operational retention applies to:

- login events: 180 days by default;
- marketing attribution: 365 days by default;
- notifications: category/read-state-specific retention;
- orphan image assets: age-based cleanup followed by durable object-deletion outbox delivery.

All values are validated options and can be shortened to meet a deployment's policy.

## Purge guarantees

`UserCleanupService` runs each user purge in its own database transaction. It removes or reassigns user-owned public catalog content according to configuration, deletes user-dependent diary/tracking data, queues object-store deletion before removing image metadata, and finally removes roles and the user row. A failure rolls back that user's transaction and processing continues with the next account.

The database foreign-key model is the final safety net for dependent feature rows. Schema changes that add a user-linked table must update cleanup coverage before release.

## Operational evidence

Cleanup jobs emit execution outcome, duration, and affected-row telemetry. Durable object deletion uses the shared outbox with retries and dead-lettering. Dead-letter replay requires an operator identity and reason and persists an immutable replay audit record.
