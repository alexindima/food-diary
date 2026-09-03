# Users administrative read adapter

Baseline: `875dc39362a1c066a06ddf1ad5892bd897ef6345`, clean `master`.

## Boundary and compatibility

`UserAdministrationReadService` belongs to Users and consumes
`IUserAdminReadModelRepository`; Admin handlers consume that semantic service.
Names alone do not make account projections Admin-owned. Both administrative
ports move together to `Modules/Users/Infrastructure/Persistence/Users/` in
`UserAdministrationReadRepository`, because their entity/model methods already
share one paging and summary implementation.

| Registration group | Owner and scoped identity after the move |
| --- | --- |
| IUserRepository, IUserLookupRepository, IUserWriteRepository, IUserGoogleIdentityRepository | Same central UserRepository instance |
| IUserAdminReadRepository, IUserAdminReadModelRepository | Same Users UserAdministrationReadRepository instance |
| IUserAccessTokenSecurityReader | Existing independent Users security-state reader |

All use the same scoped FoodDiaryDbContext. Existing AddUsersPersistence and all
three hosts' AddUsersModule calls compose the adapter. No project, dependency,
package, port, model, migration, HTTP, authorization or provider change is needed.
The old concrete UserRepository no longer exposes administrative read methods;
coordinated host/consumer rebuild is required, not a binary-forwarding promise.

The exact method bodies, constant and DTO mapper are preserved. The small
UsersWithRoles query shape remains available independently to both tracked lookup
and detached reads; the paging engine is not duplicated. Preserve:

- explicit Active/Inactive/Deleted filters; All and unknown enum values retain the
  old unfiltered fallback. The bool overload's false means Active, not merely
  non-deleted;
- trimmed, case-insensitive literal ILIKE matching across email and profile names,
  with backslash, percent and underscore escaped;
- normalized page/size, count before paging, descending CreatedOnUtc, page-ID
  order restored after detached role hydration. Equal timestamps have no newly
  promised tie-breaker;
- no-tracking reads with roles but without weight/waist goal histories; read
  models query persisted state even if the shared context tracks unsaved edits;
- summary counts over all users, active/deleted predicates, distinct premium
  memberships regardless of account state, and recent non-deleted users including
  inactive accounts. No counter/filter redesign;
- all DTO fields and cancellation propagation. Shared UnitOfWork, save semantics,
  domain state and security reader remain unchanged.

## Verification design

Three existing PostgreSQL paging/role/summary tests move to the existing Users
Infrastructure.IntegrationTests project with only their constructed type changed.
Mixed lookup/write/read/login/refresh integration stays central, changing only
the administrative adapter call sites. Original assertions remain intact.

New provider cases cover status/order/page boundaries, literal search escapes,
persisted profile/roles and absent goal history, premium/recent summary semantics,
empty data and cancellation through both ports. Scoped registration tests protect
both composition orders, two aliases sharing one reader, separation from the
tracked repository/security reader and the four unchanged central aliases.
The prior security-reader test keeps its applicable assertions; administrative
alias assertions now belong to the focused reader tests. Roslyn ownership guards
keep the two responsibilities and registrations separate.

Run a full solution build, complete owned/donor/consumer suites, full shared and
Users PostgreSQL suites, full HTTP suite, EF model comparison, NuGet audit and
architecture tests without coverage collectors. The final per-suite TRX hashes,
source equivalence checks and process receipts are retained separately under
`.artifacts/users-admin-reader-evidence`. Failed attempts are not combined with
final passed counts. No benchmark, deployment or external security scan is claimed.

The first build exposed test-only analyzer violations (named paging parameters,
tuple deconstruction/types, and an inlinable local); the first correction left
tuple/bool-argument issues for a second correction. Both failed build logs are
retained. The complete solution then built with zero warnings/errors before the
final test runs. These were task-introduced test-style errors, not baseline failures.

The first Users persistence run passed 17 of 18 cases; the added profile fixture
used `male`, which the existing domain correctly rejects because its supported
codes are `M`, `F` and `O`. Only that test-data literal was corrected to `M`;
domain validation and assertions were not relaxed. The failed TRX remains in
evidence. After another full solution build, the complete Users persistence and
architecture suites passed. Other complete runtime suites used unchanged code.

Final runtime evidence: 4,425 passed cases across 15 unfiltered suites, zero failed
or skipped. These include Users persistence 18, shared PostgreSQL 95 (three
existing cases moved to Users), HTTP 182 and architecture 1,041. The final build
had zero warnings/errors; EF reported no pending model change, with the existing
10.0.10 tools / 10.0.11 runtime warning. The NuGet audit parsed all 319 solution
projects with zero vulnerabilities or reported problems. Counts select the latest
successful run per suite; earlier runs are retained, not summed twice.

## Wiki evidence and limitations

Wiki research found the immediately preceding Users security-reader commit,
Admin role-audit projection and Dashboard adapter precedents. Source review
establishes ownership and shared query semantics; the design records the decision
to move both ports rather than duplicate/delegate the query engine. No new ADR
trigger was inferred. Ownership again returned empty arrays, test-plan included
unrelated Admin registration tests, and privacy supplied broad account/health
leads rather than the full DTO boundary. The trace command returned no text for
the service query; that is not proof of no consumers.

The clean-base frozen100 snapshot and a fresh final snapshot are retained
separately from bounded affected Wiki verification. Do not equate a successful
affected verify with exhaustive tools/retrieval-quality success. No query, ranking,
threshold or evaluation-target changes are part of this extraction. Record native
reviews, acceptance and final delivery gates against the actual source diff.

## Next boundary

The remaining UserRepository is tracked aggregate lookup/write plus Google
identity lookup using the same role graph. Review its physical ownership as one
coherent Users persistence boundary, including Identity consumers and role-audit
writes, instead of splitting it by provider method names. Shared outbox replay
is a separate design, not part of this move.
