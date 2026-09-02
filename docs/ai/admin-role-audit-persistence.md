# Admin role-audit persistence follow-up

Baseline: `f886dce18cb4fcdf81cb56fd8f3be163992cfccf`, clean master.
Scope: one read projection and its two scoped DI aliases, focused tests and owner
metadata. Identity replay, outbox, UserRepository and other audit adapters are not
part of this change.

## Ownership and compatibility

AdminUserRoleAuditRepository moves unchanged from central Infrastructure to
`Modules/Admin/Infrastructure/Persistence/Admin`. Its CLR namespace is preserved;
its assembly owner changes. The existing module project already has the required
one-way dependency on the central context. No production project reference is
added. API/Initializer already call AddAdminModule; JobManager already calls
AddAdminPersistence. Both repository aliases retain the same scoped instance.

UserRoleAuditEvent, User, role membership and their EF mappings remain in Users.
Central DbContext/DbSets, migrations and snapshot remain unchanged. The projection
is Admin-owned even though it joins Users-owned data. AdminUsersController retains
the Admin-role requirement; application validation/existence checks and HTTP
payloads/statuses remain unchanged. Coordinated host rebuilds are required; no
old-binary forwarding promise is made. This follows the accepted module ownership
policy and verified Identity precedent f886dce18, not a new ADR-level constraint.

The query body, user predicate, left actor join, descending date sort, 1-50 limit,
no-tracking, projected fields and cancellation are unchanged. No new collection,
sharing, logging, retention, storage, provider or background work is introduced.

## Tests

The isolated audit helper assertions leave the central mixed repository test;
all other repository branches stay there. A dedicated module PostgreSQL case
retains its zero-limit, single-result, actor ID and non-null email assertions and
checks every projected field. Additional real-provider cases cover another user's
newer event, descending order, the fifty-row cap, absent actor, unknown user,
no-tracking and cancellation. A module unit test proves scoped alias identity,
cross-scope isolation and physical assembly ownership. Architecture tests separate
Users entity ownership from Admin projection ownership and reject the donor path.
The two new test projects reuse central settings/shared provider fixtures and
central package versions. No coverage collector is used.

## Wiki evidence and limits

Native start captured a clean baseline and created a separate governed workspace.
Design is ready with source-proven preserved boundaries. The decision reader found
no deterministic new ADR trigger. The test plan found the mixed provider test,
but also suggested unrelated Ai/Billing/Fasting cases; direct consumer/source
review determines the final scope. Privacy output included unrelated Dashboard
data instead of a precise role-audit lifecycle; the source assessment above is
authoritative. These are navigation limitations, not execution evidence.

Exactly one expected path in context-search-holdout-100 is updated for this
content-identical move (holdout100-conv-002). Queries, IDs, cohorts, ranking,
thresholds and frozen history are not changed. Git blob equality proves the
production move; reversing that single expected-path substitution reproduces the
original corpus exactly after Git line-ending normalization.

A native measurement on a locked, clean read-only snapshot at the exact baseline
f886dce18 produced top1=96/100 and top10=99/100. Its only live gap is top10<100;
holdout100-conv-001 (Shared DomainGuard) ranks45. That baseline source remained
clean before/after measurement. This does not certify the changed graph: final
Wiki and any comparison receipts remain distinct from the baseline result.

## Verification evidence

Forced/locked restore and full solution build passed with zero warnings/errors.
The first build failed on one unused central DI import left after relocation;
the import was removed and the complete solution rebuilt successfully. Both logs
are retained; no compiler rule or test assertion was relaxed.

Twelve final unfiltered suites passed: 3704 cases, zero failures/skips.

| Suite | Passed |
| --- | ---: |
| Admin Infrastructure | 1 |
| Admin PostgreSQL | 4 |
| Admin Application | 52 |
| Users Application | 156 |
| Users Domain | 278 |
| Central Application | 361 |
| Central Infrastructure | 563 |
| Presentation | 825 |
| JobManager | 168 |
| Full Architecture | 1015 |
| Full central PostgreSQL | 99 |
| Full HTTP/Swagger integration | 182 |

EF has-pending-model-changes exited0, no changes. The existing tools10.0.10 versus
runtime10.0.11 warning is retained. NuGet audit exited0: 318 projects, zero
vulnerable entries/problems. Only two new test lockfiles were added; production
project files, existing locks, migrations and HTTP snapshots are unchanged.

Actual commands, exits, all12 TRX, source/fixture audits and final Wiki/governance
results are retained under `.artifacts/admin-role-audit-evidence`. Runtime success
does not imply Wiki acceptance or measured coverage. No collectors, push,
deployment or production access were performed.
