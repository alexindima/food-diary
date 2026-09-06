# ADR 0032: Retry isolation and image reference integrity

- Status: Accepted
- Date: 2026-09-06
- Extends: ADR 0030 and ADR 0031

## Decision

Keep automatic transaction retries in Products, Recipes, WeeklyGoals and Billing.
A top-level runner requires a clean unit of work and no inherited post-commit actions.
Each attempt starts with an empty tracker and domain-event dispatch registry. Failed
attempts discard tracked mutations and queued best-effort actions, including after
SaveChanges succeeded but commit failed. Failure Results do not commit. Callbacks
must reload mutable inputs inside the attempt; Billing renewal and inbox callbacks
reload captured subscriptions/events. External provider calls remain outside retries.
The API, Initializer and JobManager compose the same scoped post-commit queue.

Images deletion must not remove a successfully referenced asset. Change the six
image FKs (Products, Recipes, RecipeSteps, Meals, MealAiSessions and Users profile)
from SET NULL to NO ACTION with EF ClientNoAction, so EF cannot null a tracked foreign FK before database validation. Existing FK indexes remain. A concurrent reference and
deletion serialize through PostgreSQL referential integrity; a losing deletion fails
atomically with its outbox inserts. Ordinary cleanup preserves caller-owned saving.
Orphan cleanup uses a fresh scope per candidate, rechecks usage and saves only that
candidate. A failed candidate cannot poison later candidates; cancellation propagates.
Users explicitly clears its own profile FK before invoking the ordered image purge.

Images.Service.Contracts owns IImageAssetAccessService and immutable Id/Url results.
Keep Images.Contracts ID-only. Consumers no longer receive the ImageAsset aggregate;
Ai, Products, Recipes and Meals remove their direct Images.Domain references.
Existing optional/null, ownership, confirmation and wire-error behavior stays intact.

FD0015 and FD0016 also inspect EF method groups before a capability can be handed to
a delegate. Untyped write delegates remain prohibited; exact reviewed technical
fingerprints permit the same technical method-group operations as direct calls.

## Deployment and verification

Deploy the FK migration before relying on the new deletion guarantee. It preserves
all data, columns and indexes but takes DDL locks on six referencing tables. Schedule
it with normal migration controls and inspect lock contention. No production migration
is applied by this development task. Down restores SET NULL and therefore restores
the old concurrent-reference risk; roll back application binaries independently when
possible. Updated contracts require coordinated consumer rebuilds; HTTP snapshots do
not change.

PostgreSQL regression scenarios cover failures before SaveChanges and between save
and commit for all four runners, one durable outbox/callback result after retry,
a competing committed image reference, batch failure isolation and Users purge.
Compile-time cases cover foreign/untyped/owned EF delegates and reviewed technical
sources. Architecture checks constrain the service package, module graph and model.
