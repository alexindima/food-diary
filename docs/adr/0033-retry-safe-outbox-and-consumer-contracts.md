# ADR 0033: Retry-safe outbox and consumer contracts

- Status: Accepted
- Date: 2026-09-06
- Extends: ADR 0032

## Decision

Wearables executes provider work once under its session advisory lock. The final
SaveChanges uses EF's implicit transaction and execution strategy. This supports
production Npgsql retries without repeating OAuth or data-provider requests. Failed
Results discard unsaved tracking; explicit token refresh/deactivation saves retain
their existing durability. Replay instead retries its entire short database-only
transaction, reloading the dead letter and creating the audit afresh after rollback.

All four outbox records use the existing LockedBy as an EF concurrency token.
Expired workers cannot finalize, dead-letter or release a lease acquired by another
worker. A lost claim returns zero processed and records claim_lost telemetry.
Gamification's conditional SQL checks both claimed revision and owner; releasing a
newer revision still requires the same owner. Dispatch remains at-least-once:
lease fencing protects database finalization, not external exactly-once delivery.

Remove all ten foreign Application project references. Cycles, DailyAdvices and
Tdee publish their actual Dashboard read messages/projections; Export uses Cycles
read contracts. Gamification publishes its administration interface and DTOs for
Admin. Meals.Service.Contracts owns the Dashboard meal-list API. Images' three
existing consumer input helpers move into Images.Service.Contracts. No handlers,
provider clients or repository implementations move into those packages.
Ai administration interfaces move to Ai Application Abstractions; upsert returns
an immutable AiPromptTemplateReadModel instead of an aggregate. Admin's achievement
validators retain an explicit Gamification Domain reference for existing enum and
length constants; this does not grant foreign aggregate writes.

## Compatibility and rollout

Routes, payloads, error codes, DI implementations and provider call order remain
unchanged. Rebuild and deploy consumers together because type assembly ownership
changes. All three Docker restore graphs and the exact project matrix include the
new contracts. No package versions are changed. LockedBy already exists; concurrency
metadata is reflected in the snapshot and needs no relational migration or backfill.
No production database operation is part of this change. Rolling workers with the
old engine cannot honor fencing; retire them before relying on the guarantee.

## Verification

Use real PostgreSQL with retry settings enabled for Wearables and replay. Inject a
transient INSERT failure and a pre-commit failure respectively. Exercise competing
leases with deterministic barriers for stale success and failure. Retain revision
coalescing, rollback, cancellation, HTTP contract and module consumer coverage.
Architecture guards enforce zero foreign Application implementation references,
four LockedBy concurrency tokens, model/snapshot parity and immutable Images exports.
