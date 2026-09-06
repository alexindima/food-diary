# ADR 0035: Runtime collaboration, owner projections and concurrent recipe composition

- Status: Accepted
- Date: 2026-09-06
- Extends: ADR 0030, ADR 0031, ADR 0032

## Decision

Products and Recipes execute their existing top-level mutation callbacks in
PostgreSQL Serializable transactions. Remove the global recipe-composition advisory
lock. Keep row locks, access/usage/cycle validation, caller-owned capabilities, and
the existing execution strategy with whole-attempt tracker/event/callback reset.
All reads used to validate composition run against the same database transaction;
the product and recipe lookup services use uncached overview queries. Provider I/O
must remain outside replayable callbacks. PostgreSQL serialization/deadlock failures
retry the entire operation with fresh reads; do not retry only SaveChanges.

Serializable protects concurrent mutations participating in this boundary, including
opposite recipe edges and product-update/reference races. It does not turn unrelated
read-committed writers into serializable participants. Existing foreign-key and
snapshot rules still apply to Meals, USDA links and operational purge. New composition
writers must use this boundary. Unrelated operations can overlap; predicate-lock
granularity can still cause conservative aborts under contention.

Users Infrastructure supplies persisted, no-tracking projections for current-user
access and AI, Dashboard, Dietologist, Gamification, Hydration, TDEE and WeeklyCheckIn
profiles. Preserve the existing active/not-deleted query predicate and InvalidToken
result for filtered/missing users. These capabilities do not observe unsaved tracked
mutations. Owner mutation and full profile/history workflows retain UserContextService.
The User domain calculation has scalar overloads so TDEE projections use the same
formula without materializing credential fields, roles or goal collections.

Meals owns IMealItemDisplayReadService and the immutable MealItemDisplayReadModel.
The batch reader enforces user ownership, keeps item order and the existing Dashboard
snapshot/legacy fallback and food-quality semantics, and reads without tracking.
Dashboard performs only wire-model mapping. This does not redefine the distinct
legacy recipe fallback used by the meal-detail repository. HTTP contracts and
database mappings are unchanged.

The Application API/contract-reference graph remains acyclic. The separate
runtime-module-boundaries.json inventory records consumer-owned ports implemented
by foreign modules, their parameter-type consumers and reviewed transaction policy.
Architecture tests detect additions/removals and changed implementation ownership.
This conservative static inventory complements the reference graph; it is not a
dynamic trace, DI reachability proof, or assertion that runtime collaboration has
no cycles. In particular Users requests Identity session revocation while Identity
uses Users authentication capabilities. Ordinary synchronous mutations join the
caller's unit of work; purge participants join Users' ordered purge transaction.
The existing Gamification relational enqueue is an immediate SQL command: it uses
an ambient transaction when present and otherwise autocommits. The inventory records
this exception explicitly; a shared DbContext alone does not make it atomic with
the caller's later SaveChanges. This tranche does not redesign that delivery flow.

## Verification and rollout

Run one combined backend verification after the changes: module and cross-module
unit/provider tests, architecture checks, and HTTP/OpenAPI consumers. PostgreSQL
regressions cover independent transaction overlap, opposite-edge write skew,
product/reference races, retry reset, projected persisted state, no tracking,
tenant filtering and snapshot fallback. Keep the EF pending-model check.

No migration or configuration change is required. Deploy coordinated application
binaries: mixing the former global-lock protocol with Serializable composition is
not supported. Observe serialization retries, exhausted retries and latency under
load. Roll back coordinated binaries if necessary; the database schema is unchanged.

PostgreSQL's [Serializable isolation documentation](https://www.postgresql.org/docs/17/transaction-iso.html#XACT-SERIALIZABLE)
describes the consistency guarantee and requirement to retry whole transactions.
