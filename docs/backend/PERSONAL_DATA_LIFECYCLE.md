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

`GET /api/v1/export/personal-data` produces a ZIP of explicitly selected current-user
application projections, including consent history and normalized payment records.
It never serializes mapped entities or credentials. The uncompressed payload is
bounded at 64 MiB across sections; larger requests require the manual access route.
The manifest names the included sections and exclusions. This is not a complete
automated DSAR: original files, correspondence, provider and infrastructure records
require reconciliation. Fertility signals retain the separate verified cycle export.

## Consent

Users owns purpose/version/time consent events and the current effective choices.
The browser and server share an archived notice version. Granting an obsolete or
unseen newer notice fails; withdrawing remains possible from an older client.
Legacy AI timestamps are not relabelled as reviewed versioned consent. Their
explicit compatibility applies only to the unchanged first version.

Health HTTP access and scheduled wearable imports/reminders require a current
health choice. Optional campaign identifiers and ingestion require browser opt-in;
account-linked premium attribution checks the persisted account choice. Explicit
withdrawal cannot be undone by delayed signup attribution. Failed account withdrawal
sync is visible and retried for that account. Essential authentication/security
storage has a separate purpose.

After health withdrawal, privacy settings offer an archive download and
whole-account deletion through the existing deletion endpoint and a separate
confirmation. The offer remains available on returning to settings. Cancelling
deletion leaves consent withdrawn and does not delete the account. Deletion
uses the existing soft-delete/cleanup lifecycle and is not immediate.

**Release blocker:** health withdrawal currently pauses guarded operations. The
erasure or independent retention basis for users who decline account deletion,
lawful retention exceptions and all background processing must be finalized
before an EU/EEA release. Storage is processing; a
pause alone is not erasure. Marketing withdrawal also stops new attribution but
currently leaves historical attribution records subject to their retention job.
Purpose-specific erasure or a documented independent retention basis is required.
Do not treat this draft as completed GDPR compliance.

## Account deletion and retention

Account deletion is soft first. The recurring `UserCleanupJob` permanently purges accounts after `UserCleanup:RetentionDays` (30 days by default) in bounded batches. This delay provides a recovery window and prevents a large deletion from monopolizing the database.

Independent operational retention applies to:

- login events: 180 days by default;
- marketing attribution: 365 days by default;
- notifications: category/read-state-specific retention;
- orphan image assets: age-based cleanup followed by durable object-deletion outbox delivery.

All values are validated options and can be shortened to meet a deployment's policy.

## Purge guarantees

`UserCleanupService` runs each user purge in its own database transaction.
Both configured successors and automatic system ownership retain only referenced
catalog composition, including nested recipe/product dependencies. Owner adapters
remove identifying free text, covers and galleries before transferring those rows.
Unrelated private catalog content is deleted. Numeric composition and foreign
references remain; this minimization does not establish irreversible anonymity.
Images stay with the deleted owner and enter the object-deletion outbox. Any
failure rolls back all changes for that account before the next account is tried.
See the proposed privacy decision in ADR 0059, extending ADR 0054.

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
single transaction. Images queues deletion of the erased owner's assets; erasure does not transfer
those assets to a content successor. Participants never save/commit; a participant failure rolls back
all relational changes for that account. See ADR 0030.

Ordered `IUserContentRetentionParticipant` adapters lock owned content before
selective cleanup: Recipes first, then Products. Retention runs for either a configured or a system
successor, with the same redaction guarantees. ReadModel.Composition
returns scalar IDs only; owner modules perform every transfer in the account's
transaction. Provisioning uses a fixed reserved identity and advisory lock, and
refuses a conflicting account state. The technical account uses the existing schema;
the RecipeLikes reference constraint is the accompanying schema migration.

## Release reconciliation

Previously retained system-owned catalog rows can predate the redaction policy.
Before release, inventory those rows and their images and perform an owned,
transactional remediation; the new purge path does not retroactively scrub them.
Processed billing webhook raw payloads are cleared by the accompanying migration;
financial statutory retention and failed-webhook retention still require owner facts.
See `docs/privacy/GDPR_OWNER_INPUTS.md` and the privacy release checklist.
