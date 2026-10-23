# 0056: Required content revisions for shopping-list updates

Date: 2026-10-23

Status: Accepted

## Context

Shopping-list PATCH replaces the submitted item collection. PostgreSQL `xmin`
already detects writes that overlap after loading an aggregate. It cannot detect
a client that reads an old list, waits for another request to commit, and then
submits the old collection. Accepting that request silently removes the other
client's additions and can revert checked state, quantities, notes and ordering.

The user explicitly approved requiring a revision from every PATCH client,
including older packaged applications. This is the narrow compatibility exception
to the general installed-mobile-client policy in `docs/frontend/API_SDK.md`.

## Decision

Every detailed shopping-list response includes an opaque `revision`. Every
`PATCH /api/v1/shopping-lists/{id}` must supply its original snapshot's
`expectedRevision`, a 64-character lowercase hexadecimal SHA-256 value. Missing,
blank and malformed values return 400. The handler loads the owned aggregate and
compares the precondition before changing its name, resolving products or
mutating any items. A mismatch returns 409 with `ShoppingList.RevisionConflict`.
A valid precondition does not grant access: another user's list still returns 404.

The revision hashes a versioned, explicit canonical representation of the exact
detailed application model: list identity, name and creation instant; every item
field, including checked time and sort order; and every source-provenance field.
Nulls are explicit and numeric values retain round-trip double precision without
extra rounding. Items use sort order and identity; sources use day, ordinal label
and identity to remove enumeration-order differences. Timestamps retain their
UTC-instant meaning and use Npgsql's persisted microsecond precision: truncate
ticks relative to the PostgreSQL 2000 epoch towards zero, retaining the Min/Max
infinity sentinels. The canonical writer never serializes the revision itself.

Both tracked aggregate loads and detailed projections use a single SQL query for
the list/item/source chain. Their returned content and revision therefore describe
one database snapshot. Creation, generation and PATCH responses calculate the
revision from their own mapped immutable snapshot, including timestamp
normalization, before the existing transaction commits. They do not attach a
newer database token to an older response body.

The existing tracked `entries.Update(list)` and parent `xmin` compare-and-swap
remain mandatory, including for item-only edits. They reject two requests that
both passed the content precondition before either committed. The existing
transaction boundary rolls back all child writes and other staged changes from
the losing request. No schema column, migration, post-commit query or new
repository capability is introduced.

Clients retain the baseline revision with their editable snapshot and replace it
only after an accepted response or explicit reload. Nonselected summary actions
first fetch the detailed snapshot. On 409, clients preserve the local draft and
stop automatic retries; they must not fetch a fresh revision and attach it to an
old full replacement body. Users choose an explicit reload or reconcile their
changes against the latest content.

## Consequences

The current web bundle supplies the precondition. An old open tab must load the
updated application before writing; normal update/reload is sufficient. Manual
cache clearing is not a data-concurrency remedy. Packaged mobile clients that do
not send `expectedRevision` need an application update. The server and new web
bundle should be released together; the older client safely receives 400 instead
of silently overwriting another client's data.

The value is a content revision, not a monotonic counter or authorization secret.
An identical state has an identical revision, including after a change is later
reverted. No-op writes may retain the token. New persisted response fields require
reviewing the canonical format and its tests. Future item/source writers must
participate in the parent concurrency boundary. Foreign-key side effects, such
as Product `SET NULL`, are not claimed to acquire the parent lock automatically;
their already-committed visible content is included in the next revision.

Application tests cover every field, collection ordering, nulls, cultures,
neighboring floating-point values, validation, ownership and rejection before
mutation. Real PostgreSQL tests cover provider timestamp round trips, generated
response tokens, item-only parent `xmin`, competing saves and rollback. Real HTTP
tests cover sequential stale snapshots, current/create/detail/overview parity,
checked-state response persistence and required revisions. These tests must
actually execute; a Docker skip is not successful relational evidence.
