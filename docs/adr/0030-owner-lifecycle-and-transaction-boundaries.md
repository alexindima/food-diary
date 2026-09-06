# ADR 0030: Owner lifecycle, recipe snapshots and explicit transaction entry

- Status: Accepted
- Date: 2026-09-06
- Related: ADR 0027, ADR 0029

## Context

Physical module extraction left UserCleanupService mutating other owners' tables,
Recipes loading mutable Product aggregates, TDEE consuming Dashboard statistics,
and top-level transaction runners implicitly saving pending caller changes.
The Application API graph excluded owner contract references.

## Decision

Keep the shared context, database, migration history and per-user purge transaction.
Users selects and locks eligible accounts, validates the reassignment target,
invokes ordered IUserDataPurgeParticipant extensions, removes its own rows and
commits. Purge requires clean entry and resets tracking before retry attempts and
after failed accounts so rolled-back additions cannot leak into a later commit. Each participant lives in the data owner's Infrastructure and cannot
save or start a transaction. Products and Recipes request image ownership transfer
through Images' narrow capability before Images deletes remaining assets and queues
object deletion. Host registrations install the twelve required participants;
integration tests verify their composition, reassignment and rollback. Do not call
these technical lifecycle capabilities from ordinary user-facing use cases.

RecipeIngredient stores ProductId and an immutable, transient product snapshot.
Recipe repositories populate snapshots with one no-tracking batch projection;
overview queries join Products explicitly. Product/User, Product/USDA and Recipe/User
relationships retain scalar keys and their original relational constraints in
PersistenceModel, with no foreign CLR aggregate navigation in these domain types.
Snapshot metadata changes require no schema migration. Other modules' remaining
foreign navigations are not changed by this tranche.

Meals supplies the daily calorie projection used by TDEE. Keep the existing UTC
range endpoints, daily buckets and rounding. TDEE no longer consumes Dashboard
contracts. Dashboard retains its other readers and existing wire models.

Billing, Wearables, Products, Recipes and WeeklyGoals top-level runners require
no pending tracked changes or existing relational transaction on entry. Enter the
runner before mutations, including Billing failure/idempotency status updates.
Unchanged tracked entities remain valid inputs. Their invoked owner capabilities
still participate in the same unit of work; this is not an isolated per-module
DbContext and is not an implicit nested transaction/savepoint API.

Record direct owner Contracts and Application.Abstractions project references
separately from Application API edges. Validate the exact combined graph and its
known strongly connected components. Existing contract cycles are acknowledged
composition debt; a green API-only graph is not a claim of full module isolation.
The exact edge manifest prevents enlarging these components without review.

## Consequences and verification

No new runtime project, API shape, provider call or database column is introduced.
All hosts must be rebuilt together. Foreign type consumers add explicit references
where they previously relied on transitive Domain references. Preserve the existing
cleanup disposition matrix and FK behavior. Run architecture and owner/consumer
tests, PostgreSQL query/tracking/reassignment/rollback tests and the EF pending-model
check in the combined verification pass.

Future work may remove the remaining contract cycles and foreign navigations after
their individual behavior and ownership are established. These residual boundaries
are not hidden by the new graph or claimed as solved here.
