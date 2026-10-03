# ADR 0052: Transaction outcomes, delivery ownership and independent user state

Status: Accepted

Date: 2026-10-03

## Context

The architecture audit reproduced handler replay after a successful COMMIT with a
lost acknowledgement, duplicate meals after HTTP cache-completion loss, and stale
MailRelay acknowledgements. It also found swallowed push failures, failed command
state surviving in a scope, excessive User contention, implicit persistence
participation, and Dashboard reads without a common snapshot or enforced budget.

## Decision

1. Every shared commit captures its PostgreSQL transaction ID. After an acknowledgement
   failure, dispose the transaction and check pg_xact_status on an independent
   connection with a 15-second deadline. Committed means preserve the computed result
   and callbacks; aborted permits provider retry; unverifiable means throw the
   nontransient CommitOutcomeUnknownException and never replay the handler blindly.
   PostgreSQL documents this check for [uncertain COMMIT outcomes](https://www.postgresql.org/docs/17/functions-info.html).
2. CreateMeal and RepeatMeal opt into IIdempotentAtomicCommand. An advisory lock
   serializes equal keys; AtomicCommandReceipts stores the authenticated owner,
   key/request digests and successful typed result with business writes in one
   transaction. Redis still supplies HTTP leases and response caching. Its completion
   failure retains the existing 409 Idempotency.LeaseLost response, but retry after lease acquisition replays the committed
   result during receipt retention. Incompatible payload/owner/type returns conflict.
   Other endpoints retain their existing owner guarantees; receipts are opt-in.
   Retention defaults to 24 hours and is bounded by seven days. A bounded JobManager
   cleanup runs every 15 minutes. User purge removes receipts in its user transaction.
   Responses contain private meal data: never log them or copy raw keys.
3. MailRelay's monotonically increasing AttemptCount fences renewal and all
   sent/suppressed/retry updates. They require the same attempt, processing state
   and an unexpired lease. Delivery renews ownership, cancels on loss, and has a
   ten-minute deadline. Polling claims no more than MaxConcurrentDeliveries (default
   ten, bounded to 1–64) and starts those items immediately. SMTP
   acceptance has a separate bounded database acknowledgement; Message-Id stays
   stable. A crash between remote acceptance and local persistence can still
   produce duplicate SMTP delivery. Fencing does not provide exactly-once SMTP.
4. WebPush reports completed subscription IDs and retryable failures. The outbox
   persists progress with retry state and skips known successes on later attempts.
   Invalid 404/410 subscriptions are removed and considered complete; unexpected
   failures and deadlines retry until dead-lettering. A crash before the progress
   checkpoint can still repeat remote delivery.
5. The outer command owns saving, callback flushing and scope cleanup. Failure
   clears every enlisted tracker and discards callbacks. Nested ordinary commands
   cannot save independently; ignored nested failures prevent the outer commit.
   Atomic commands must own the outer boundary. Retried handlers have no external effects.
6. Users owns the account principal in Users, interface state in UserPreferences,
   and health/goals in UserNutritionProfiles. Each has separate xmin and audit
   timestamps. Existing User methods coordinate validation and preserve consumer
   contracts; SQL uses mapped navigations. Independent preferences, nutrition and
   login-activity writes can commit; same-record competitors still conflict.
   Before profile writes, an owner interceptor locks the account row FOR SHARE and
   checks deletion/security state against the loaded security version. Account
   revocation intentionally invalidates previously loaded profile writes. The lock
   serializes with revocation without updating account xmin. The interceptor owns
   no save, commit, retry or foreign write.
7. Save priorities are AccountPrincipals -100, SharedRecords 0, FullModelComposition 1,
   ModuleOwners 100; peers retain enlistment order. Owners can enlist during handlers
   and domain-event dispatch, including inside a live transaction. Enlistment once
   coordinated saving starts is rejected before it can omit a participant. Acceptance
   follows successful shared persistence; failure cleanup covers all owner trackers.
8. Production Dashboard reads profile and sections sequentially on the shared
   connection inside REPEATABLE READ, READ ONLY. Late-enlisted owners join that
   transaction. Each snapshot has a 15-second deadline and 32-query limit. SQL count
   and duration use the FoodDiary.Persistence.ReadSnapshots meter exported by both
   hosts. Favorite state is projected with the meal page, which has an ID tie-breaker.
   Budget increases require measured evidence. Do not parallelize EF on this connection.
   The populated HTTP regression measured 22 SQL queries in each of 13 time-zone
   and daylight-saving scenarios, below the 32-query limit.
9. Current architecture guides and the roadmap describe these protocols and canonical
   module project names. Historical migration plans remain in Git history.

## Migration and rollout

Both implementation and Designer are retained for:

- 20261003001656_AddDurableAtomicCommandReceiptsAndPushDeliveryProgress.
- 20261003003938_SplitUserPreferencesAndNutritionProfile.

The split creates tables, copies all 34 moved fields and audit timestamps, then drops
old columns. Down restores the latest independent values before dropping the tables.
The new owner foreign keys cascade on physical account deletion.

Use a coordinated release: stop API, JobManager and other writers; back up; migrate;
start compatible binaries. Old binaries cannot write the split schema. Downgrade
with Down while writers are stopped before starting old binaries. Dropping durable
receipt/progress state loses retained replay/progress guarantees. Preparing this
change does not deploy it or alter production databases.

## Verification

Regression coverage includes successful COMMIT with lost acknowledgement; HTTP
create/repeat after cache completion loss; stale MailRelay updates; actual push sender
through its outbox; failed and ignored nested commands; independent/conflicting User
writes and revocation; complete migration upgrade/downgrade; read snapshot consistency,
query/deadline limits; and save-priority/enlistment rollback. Exact dependency,
persistence capability, lifecycle, host composition and API contract guardrails remain.
