# 0054 Independent commit verification and notification reconciliation

Status: Accepted

## Context

The shared unit of work verifies uncertain PostgreSQL COMMIT outcomes under
ADR 0052. Independent AI quota/job transactions and generic outbox claims used
ordinary commits under provider execution strategies. Losing the acknowledgement
after a successful commit could replay Reserve/Claim and discard the original
result. These operations must retain their independent transaction boundaries.

JobManager persists notifications but has no local browser SignalR connections.
SignalR refresh hints also cannot guarantee delivery across API replicas or a
disconnected client. A loaded browser feed needs an explicit reconciliation path.

MailRelay has a durable PostgreSQL polling fallback, but blocking RabbitMQ topology
bootstrap prevented that fallback and HTTP from starting while the broker was down.
The primary API also waited for healthy mail components during Compose startup.

## Decision

1. `Shared/FoodDiary.Persistence.PostgreSql` owns the existing verified COMMIT
   implementation and `CommitOutcomeUnknownException`. It references only EF
   relational storage and Npgsql. Persistence.Runtime, Ai.Infrastructure and
   Outbox.Infrastructure reference it directly. Administrative dead-letter replay
   uses the same helper before returning its committed replay audit. It does not create contexts,
   own saves, run callbacks or select a retry policy. The caller preserves its
   computed result on confirmed commit; only confirmed abort can authorize retry.
   Unknown verification remains nontransient. The existing verification deadline
   and independent connection are preserved.
2. AI quota and recognition transactions remain independent and short. Outbox
   claims retain lease, worker identity and one-message processing. Both use the
   common verification helper before returning their original result. No external
   provider call is added to a retriable transaction.
3. An authenticated, non-impersonated, visible browser reconciles unread count and
   a previously requested notification feed every 30 seconds. Becoming visible triggers
   one immediate reconciliation. Existing requests coalesce; an active feed request
   is not overlapped. Session-bound request intent survives an initial feed failure,
   so the next reconciliation retries it; a never-requested feed stays unloaded.
   Failed feed refreshes remain eligible for recovery. Cleanup
   removes timers and visibility listeners; SSR performs no browser connection or
   polling. Freshness is bounded by the interval plus successful HTTP latency.
   HTTP responses are bound to the session owner and generation; hub callbacks
   require the current connection and owner. Late HTTP responses from an earlier
   account cannot replace the current account's state. Failed refreshes preserve
   the last successfully loaded data and the existing feed DOM. The initial-load
   spinner does not replace a previously loaded feed during background refresh.
   Same-session token rotation does not restart the reconciliation deadline;
   token availability is a boolean dependency of the timer effect.
   A shared in-flight disconnect task completes before reconnecting the current
   session. Re-login by the same owner during logout cannot leave the hub stopped;
   cleanup applies only to the captured connection and does not create retry loops.
   AuthInterceptor binds refresh and retry to the original owner, impersonation
   mode and session generation. A late 401 cannot replay an earlier account's
   request using the current account's credentials; token rotation within the
   same session remains supported.
   SignalR remains an immediate hint. Adding Redis/PostgreSQL pubsub for hints would
   still require reconciliation after disconnects and is not needed for this fix.
4. RabbitMQ topology bootstrap is a background retry loop. MailRelay readiness
   requires its durable PostgreSQL store, so polling and HTTP can start during a
   broker outage. Compose keeps database/initializer ordering but does not require
   mail/broker health before starting the primary API, or broker health before
   starting MailRelay. Worker errors retain broker observability.
5. Initializer fallback JSON is loaded before environment overrides, and an explicit
   connection argument remains last. Its isolated task configuration remains an
   explicit source replacement. User purge transfers both legacy recipe covers
   and all recipe/step gallery images before deleting the original owner's assets.
   Existing user scoping, image identity and per-user rollback are preserved.
6. JobManager registers MealPlanning persistence and its purge participant through
   its actual composition method. Application handlers are not added to the worker.
   Meal plans are removed before recipes so a user's own plan cannot prevent their
   scheduled deletion through a restrictive recipe reference.
7. When no valid explicit successor is configured, scheduled cleanup preserves
   source-owned catalog content referenced by other users or global plans through
   selective system ownership. Recipes computes foreign-reference roots, including
   favorites, likes and comments, then a cycle-safe closure of owned nested recipes.
   Products runs after recipe transfer and preserves products used by surviving
   recipes, meals, favorites or shopping lists. Owners transfer their selected rows
   and all legacy/gallery assets inside the same per-user transaction before ordinary
   deletion removes unrelated content. Image reassignment additionally requires the
   persisted source owner. Visibility is unchanged, including private dependencies;
   another user's aggregate or asset ownership is not rewritten.

   Users provisions one reserved, inactive infrastructure principal in a separate
   coordinated transaction serialized by an advisory lock. It has no email,
   credentials, external identity or roles; domain and administrative guards prevent
   authentication transitions, mutation and role assignment. Human administration
   lists and registration/retention metrics exclude it. Provisioning commits before
   content FK updates; the empty permanent principal can survive a failed content
   item, while all content transfer/deletion rolls back together. A conflicting
   reserved row stops the item safely. The principal uses the existing Users schema.

   Before authoritative reference selection, the owner adapters lock all source
   recipe rows, then product rows, with `FOR UPDATE` in stable ID order. Incoming
   FK-backed references committed before those locks are visible to selection;
   later inserts wait for the source decision. The missing RecipeLikes-to-Recipes
   FK is added with Cascade deletion, preserving ordinary recipe-delete behavior.
   Its migration removes only legacy likes whose source recipe is already absent,
   preserving every valid like and its metadata. Future likes participate in the
   same source-key locking protocol.

   A valid explicit successor retains the existing full reassignment behavior.
   When an outer negative preflight is overtaken by a committed foreign reference,
   the locked authoritative check defers the item before purge side effects. The
   next full cleanup pass provisions the principal in its separate transaction.
   Strict owned-fixture cleanup uses the same locks and refuses foreign-linked
   content without transferring it. Retained system-owned catalog content is
   long-lived; automatic collection after its last consumer disappears is outside
   this policy. Personal account, credentials and unrelated private content are purged.

## Compatibility and verification

HTTP routes and payloads remain compatible. The RecipeLike reference migration
must be applied through the normal backed-up, writer-stopped Initializer upgrade
before the matching application release; no baseline adoption script is changed.
Its Down removes the new constraint, while removed legacy orphan likes are recovered
through the verified backup. Backend assemblies must
be rebuilt together because the verified COMMIT types move to their narrow owner
and image ownership now requires an explicit source owner.
Provider tests inject a lost acknowledgement after a real successful PostgreSQL
commit for AI Reserve, recognition Claim, administrative replay and one/multiple
outbox messages. Existing
abort/unknown-outcome tests continue to protect the common helper. Purge regressions
cover gallery transfer, foreign-owner isolation, rollback and JobManager's actual
MealPlanning registration. MailRelay host/provider
tests start without a reachable broker; configuration tests verify the actual
Initializer precedence. Browser-service tests verify periodic recovery, visibility,
authentication, in-flight requests, SSR and cleanup.
Selective-retention PostgreSQL tests cover foreign consumers, private dependencies,
legacy cycles, image identity, unrelated-content deletion, rollback, concurrent
provisioning, reserved-ID collision and real worker composition. Gated PostgreSQL
tests cover references committed after preflight, blocked inserts after selection
and strict owned-fixture refusal. Migration tests cover valid-like preservation,
orphan-only repair, FK enforcement and normal recipe-delete cascades. Domain/application
and provider tests protect the permanent principal from authentication and admin
mutation and exclude it from human user statistics.
