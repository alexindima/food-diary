# UserRepository: security-state reader boundary

## Decision and port inventory

Baseline: `6cffe03f9feb6f78a0e2c8070092c4e3f394f590`.
Names are not ownership proof. Current consumers show:

| Port group | Current responsibility | This change |
| --- | --- | --- |
| IUserRepository / IUserLookupRepository / IUserWriteRepository | Users aggregate lookup/mutation, tracking, role audit and goals | Keep central, same scoped repository and exact remaining source |
| IUserGoogleIdentityRepository | Lookup used by Users' UserAuthenticationIdentityService | Keep same alias; Google authentication orchestration is not ownership of Users state |
| IUserAdminReadRepository / IUserAdminReadModelRepository | Administrative projections; models consumed through Users' UserAdministrationReadService | Keep unchanged; do not blindly move to Admin based on names |
| IUserAccessTokenSecurityReader | Persisted active/deleted/security-version predicate consumed by API bearer validation | Independent scoped Users Infrastructure adapter |

The seven original interface registrations become six aliases of UserRepository
and one separate scoped UserAccessTokenSecurityReader. Both resolve the same scoped
FoodDiaryDbContext. The reader's original AsNoTracking/AnyAsync expression is moved
unchanged: matching user ID, active, not deleted, exact security version, same
cancellation token. It neither materializes User nor consults an unsaved tracked
aggregate. No cache, extra SaveChanges, authorization, claim parsing or token policy
is introduced. Users owns the state; API/Identity still owns validation and issuance.

AddUsersPersistence registers the reader. All three hosts already compose
AddUsersModule, so their source and composition order stay unchanged. No new
production project, port, package or dependency edge is required. The concrete
UserRepository no longer exposes IsCurrentAsync: rebuild all consumers/hosts
together; old precompiled concrete consumers are not supported by a forwarder.
No model/schema/migration or HTTP change is introduced.

## Tests and compatibility proof

One provider test moves from the mixed UserRepositoryIntegrationTests into
Modules/Users/tests/FoodDiary.Modules.Users.Infrastructure.IntegrationTests. Only
its constructed type changes; assertions and remaining donor tests stay intact.
The new leaf reuses the established PostgreSQL collection/fixture links and test
defaults, with the same centrally pinned SSH.NET dependency used by provider-test
predecessors. It does not introduce external SSH/provider access.

Additional real PostgreSQL tests cover missing/inactive/deleted users and stale
versions, no tracking, cancellation, and unsaved-versus-persisted security state
using both scoped adapters. DI tests cover both registration orders, same-scope
identity, different scopes and all six unchanged repository aliases. Central DI
coverage additionally checks the existing Google alias. Architecture guards protect
the reader/test owner and unchanged host entrypoints. Full shared repository and
HTTP suites remain necessary; mocks/InMemory are not substituted for SQL proof.

Shared DbContext, migrations/model, User aggregate, all other repository methods,
API security validator and Identity/Admin behavior are explicitly protected in the
source audit. The existing lack of cancellation work in synchronous repository
UpdateAsync is unchanged, not silently repaired in this extraction.

Final runtime verification: force-evaluate and locked restore exit 0; complete
solution build exit 0 with zero warnings/errors. Fifteen unfiltered suites passed
4,416 cases, with zero failures/skips in their final receipts: Identity Application
171/Infrastructure 59, Admin Application 52, Dietologist Application 249, Users
Application 156/Domain 278/new provider leaf 7, central Infrastructure 523,
Application 361, Presentation 825, WebApi unit 247, JobManager 168, Architecture
1,040, central PostgreSQL 98 and HTTP integration 182. Historical architecture
retries are not summed into that total. The moved SQL case is absent from the
central 98 and present in the owned 7. No coverage collector ran.

EF reports no pending model change (existing tools 10.0.10/runtime 10.0.11 warning
retained). NuGet audit exit 0 and parsed JSON cover all 319 solution projects with
zero vulnerable entries/problems. The 321 existing lockfiles retain resolved
versions; only the new test leaf adds a lockfile. Native cleanup of the isolated
`C:/FD/.artifacts/users-security-reader` build completed successfully; evidence is
retained separately. Normal hook build/format results are recorded with the commit
receipt, not substituted for these actual test executions.

## Wiki and further work

Research found relevant Identity adapter and Admin role-audit Git precedents.
Design recorded the persisted-state/tracking boundary; inferred paths were checked
against current sources. Ownership again returned no modules despite explicit
paths. Test-plan included the mixed provider and architecture cases, but also
unrelated module registrations. Privacy supplied Users security-state leads;
neither those leads nor a matching class name proves full authorization coverage.
Decision found no deterministic ADR trigger; this continues existing Users adapter
ownership without a new architecture decision. No generator/ranking/threshold or
evaluation-target changes are made.

The first full architecture run found the newly added provider-test project missing
from the generated repository catalog (1039 passed/1 failed). Native Wiki update
corrected that catalog. A second complete run found a distinct stale canonical
entry: `applicationAbstractionOwnership.RecentItems = Shared`, while no central
RecentItems path is tracked at the exact baseline. The contracts moved to
`Modules/RecentItems/Application/Abstractions` in
`c0afb0edb14b6bfb81a45525711ec4ace67e3214`. Remove only that legacy central-area
entry; the actual RecentItems module mapping and unchanged guard remain. The
first run did not expose this filesystem-sensitive mismatch; the cause of the
untracked directory disappearing is not attributed. Both failed TRX/logs are kept.

Clean baseline frozen100 is top1=96/top10=99/MRR=.9719, sole DomainGuard miss rank45.
Runtime, exact source comparison, final Wiki mode/result and separate search
comparison are retained under `.artifacts/users-security-reader-evidence`.
Affected Wiki validation and retrieval-quality results must be reported separately.

Next: review the remaining Users administrative read projections and entity-returning
legacy surface. Preserve tracking, deletion filters, shared aliases and paging/role
semantics; do not mix that design with this stateless-reader extraction. Mixed
outbox replay is an independent candidate, not an excuse to move the repository whole.
