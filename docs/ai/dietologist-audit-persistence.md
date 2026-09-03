# Dietologist collaboration audit ownership

## Boundary

Baseline: `cf5589740b7c41ca28917733323458d720a254d7` (clean master).
The existing CollaborationAuditInterceptor switches only on DietologistInvitation,
Recommendation, ClientTask and RecommendationBulkDispatch. Its complete source
and four existing focused Facts move into Dietologist Infrastructure/tests, with
legacy CLR namespaces preserved. No rule body or metadata changes.

Central persistence resolves EF's existing ISaveChangesInterceptor registrations
after the explicit telemetry/domain-event interceptors, replacing the former
hardcoded collaboration slot. Dietologist uses TryAddEnumerable with scoped
lifetime, so repeated module registration does not duplicate this interceptor.
Both module-before-central and central-before-module composition are tested.
Registrations must be complete before any context/options resolution.

AuditEntry, AuditEntryService, table/mapping, shared DbContext, domain-event
dispatch, migrations and snapshot remain central and unchanged. One exact
InternalsVisibleTo for Dietologist Infrastructure follows current module
persistence precedents (Users, Products, Meals). This deliberately retains an
internal storage coupling; it does not create a public entity API, another shared
assembly, or a central-to-module adapter ProjectReference. Moving generic storage
to Dietologist or calling an asynchronous writer from the synchronous interceptor
would change a different boundary and was rejected.

API, Initializer and JobManager already register AddDietologistModule and remain
unchanged. No routes, payloads, authorization, permissions, schemas, queries,
retention, external provider calls, outbox/replay policies or schedules change.
Compatibility requires coordinated host rebuilds, not old-binary forwarding.
Existing failed-save/retry semantics remain unchanged; no exactly-once audit
guarantee is introduced by moving the interceptor.

## Verification scope

- Four source-identical rule Facts move without duplicates; their inherited
  reflection-based null-context helper is unchanged, not expanded.
- New module composition tests protect scope lifetime, repeat registration,
  central-only absence and telemetry/domain-event/audit order.
- A central PostgreSQL regression exercises real DI, synchronous committed audit,
  asynchronous acceptance, a recommendation created during domain-event dispatch,
  audit inclusion after dispatch, isolation and transaction rollback, then a fresh
  scope's successful commit. It belongs centrally because it tests the shared
  context, event dispatch, audit storage and transaction alongside module rules.
- Full owner/consumer/host, central PostgreSQL and HTTP suites, architecture,
  solution build, EF model comparison and NuGet audit are recorded separately.
  No coverage collector, deployment or production access is part of this task.

Actual commands, TRX, intermediate failures and final outcomes are retained under
`.artifacts/dietologist-audit-evidence`. Planned checks above are not pass claims.
The initial build rejected new test packaging/style (missing telemetry import,
explicit types, ordinal string assertions and Assert.Single predicate usage).
These were corrected without changing production rules or analyzer policy.
The synchronous provider check uses an explicitly named synchronous helper so
it deliberately exercises SaveChanges beside the asynchronous scenarios.

The repeated full build passed with zero warnings/errors. Full architecture
passed 1,026 cases; Dietologist Infrastructure passed 18, including the relocated
four Facts and three registration cases. The complete shared PostgreSQL suite
passed 99 cases with zero skips, including the new commit/rollback regression.
The final 12 full unfiltered suites passed 3,936 cases, zero failures/skips,
including full HTTP 182. EF reported no model changes (existing tools/runtime
10.0.10/10.0.11 notice only); NuGet audit parsed all 318 solution projects with
zero vulnerable entries or problems. Existing package versions and normalized
lockfiles are unchanged. Detailed counts are in `final-runtime-summary.json`;
runtime results do not certify Wiki or replace its actual outcome.

## Wiki evidence and limitations

Native start captured a clean baseline; research included bounded Git precedents,
design accepted the explicit boundary and scope includes all implementation paths.
Decision found no deterministic new-ADR trigger. Source inspection remains
authoritative for EF timing, internal storage access and host registration.

Ownership returned empty direct/downstream modules even with explicit planned
paths. Test-plan included the existing shared PostgreSQL class and Dietologist
tests, but declared no planned test projects and also suggested unrelated
Admin/AI/Billing fixtures. These are navigation limitations, not evidence that
ownership or coverage is absent. Exact source/DI inspection determined the scope.
Diff correctly identified Dietologist, but also attributed this `docs/ai/` report
to the AI module; no AI production source is changed. Trace of the interceptor
emitted no usable flow evidence, so direct pipeline inspection was required.

The clean baseline frozen100 result was top1=96/top10=99/MRR=.9719 with the existing
DomainGuard miss. Final evaluation must be compared independently, not inferred
from runtime results. Only one generalization-corpus expected path follows the
source-identical interceptor move; frozen100, queries, ranking, thresholds and
historical measurements are unchanged. No retrieval/generator special case is
part of this extraction. Governance approval and actual full Wiki outcome are
reported separately in evidence and handoff.
