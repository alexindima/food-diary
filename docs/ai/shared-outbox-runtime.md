# Shared outbox runtime

`Shared/FoodDiary.Outbox.Infrastructure` owns the existing generic outbox engine,
PostgreSQL claimer, retry policy, completion result, processing options and outbox
instruments. Images, Notifications and Gamification now depend on that narrow
project. It references only Outbox.Abstractions, Outbox.Management.Contracts and Persistence.Abstractions.

Central Infrastructure still owns email processing and multi-stream replay with
shared unit-of-work/audit coordination. Stream adapters and records remain with
their existing owners. There is no new queue, schema, migration or configuration.

The claimer preserves clean-entry ordering before tracker reset. A supplied
coordinated context implements IModuleScopeGuard to check every live participant;
a dedicated context checks its own pending changes and transaction. Owner adapter
callbacks continue to inspect the shared scope before every claim. Direct calls
with FoodDiaryDbContext therefore preserve the same protection as before.

Claims still use FOR UPDATE SKIP LOCKED and the exact four-table allowlist.
Finalization is fenced by LockedBy (and owner Revision), uses its independent
timeout, and never retries external dispatch. Retry/dead-letter policy and safe
error messages are unchanged. Outbox telemetry retains the FoodDiary.Infrastructure
meter name, instrument names, tags and units, with a single outbox-owned gauge state.

Verification covers architecture closure and save allowlists, outbox unit cases,
dedicated and coordinated contexts, lease competition, revision races and telemetry.
Executable receipts for this batch live under .artifacts/boundaries-wave6-results.

The technical replay-stream extension contract and tracked-record metadata live here as well; the central coordinator retains all replay transaction, audit and save ownership.
