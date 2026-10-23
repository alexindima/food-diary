# Personal Data Lifecycle

## Collection and ownership

Personal data is owned by the feature that creates it. User identity and account state belong to Users; diary, body tracking, cycle, collaboration, notification, billing, wearable, and authentication records remain in their feature-owned persistence areas. New user-linked tables must declare a foreign key deletion behavior and be included in the user-cleanup integration tests.

UserPreferences and UserNutritionProfiles cascade on account deletion. The generic
AtomicCommandReceipts runtime table stores a GUID owner and private response data
without a dependency on Users scalar types; its explicit owner purge participant
deletes these rows inside the user-cleanup transaction. Normal expiry defaults to
24 hours, is bounded by seven days and is cleaned in bounded batches every 15 minutes.
Raw keys and persisted response bodies must never appear in logs. See ADR 0052.

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

`UserCleanupService` runs each user purge in its own database transaction. A valid
configured successor retains the existing full catalog reassignment behavior.
Without that successor, referenced catalog rows are selectively transferred to a
reserved system owner: recipes used by other users or global plans, their owned
nested-recipe closure, and products used by surviving recipes, meals, favorites or
shopping lists. Recipe favorites, likes and comments also retain their source.
Legacy/gallery images move only when their persisted owner is the purged account.
Visibility is preserved, including private dependencies; unrelated private catalog
content is deleted. Diary/tracking data and credentials are deleted, object-store
deletion is queued before removing unused image metadata, and roles/user rows are
removed last. Any failure rolls back all content changes for that account and
processing continues with the next account.

The system owner is permanently inactive, has no login identity or credentials,
cannot receive roles or administrative mutations, and is excluded from human-user
lists and registration/retention metrics. Its minimal row is provisioned once in a
separate coordinated transaction before content FK updates and may remain empty
after a failed purge. This principal and retained catalog content are long-lived;
automatic collection after the final consumer disappears is not implemented.
Strict local owned-fixture cleanup refuses foreign-linked content instead of
transferring it. See ADR 0054.

Before selecting retained rows, owner adapters lock all source recipes followed
by products with `FOR UPDATE`, in stable ID order. Foreign references committed
before the locks remain visible; later FK inserts wait for the source decision.
If references appeared after an outer negative preflight and no system principal
was provisioned, the item is deferred before purge effects and re-evaluated in the
next full cleanup pass. Fixture cleanup uses the same lock/check boundary and
returns a refusal when foreign references exist.

Fixture cleanup validates the whole explicit identity set first, then retries
remaining IDs in stable order only while progress is made, for at most the initial
candidate count. Dependencies among those fixture users can converge in one call;
external references or cycles with no progress remain refused.

RecipeLikes has a recipe FK with the ordinary cascade behavior of recipe deletion.
The introducing migration preserves valid likes and removes only existing likes
whose source recipe is absent. Apply it through the normal migration procedure;
baseline adoption and its frozen fixtures remain unchanged.

Owned meal items and meal plans are removed before recipes, and recipes before products, so retained source foreign keys do not block the delete path. User-linked marketing attribution is removed in the same transaction; its normal 365-day retention does not retain records linked to a physically purged account. Anonymous attribution and other users' records keep their normal retention policy.

The database foreign-key model is the final safety net for dependent feature rows. Schema changes that add a user-linked table must update cleanup coverage before release.

## Operational evidence

Cleanup jobs emit execution outcome, duration, and affected-row telemetry. Durable object deletion uses the shared outbox with retries and dead-lettering. Dead-letter replay requires an operator identity and reason and persists an immutable replay audit record.

Purge SQL now lives in the owning modules' ordered `IUserDataPurgeParticipant`
adapters. Users retains eligibility locking, ordering, final user deletion and the
single transaction. Images owns content asset reassignment and queues deletion of
remaining assets. Participants never save/commit; a participant failure rolls back
all relational changes for that account. See ADR 0030.

Ordered `IUserContentRetentionParticipant` adapters lock owned content before
selective cleanup: Recipes first, then Products. Retention runs when the system
successor is needed. ReadModel.Composition
returns scalar IDs only; owner modules perform every transfer in the account's
transaction. Provisioning uses a fixed reserved identity and advisory lock, and
refuses a conflicting account state. The technical account uses the existing schema;
the RecipeLikes reference constraint is the accompanying schema migration.
