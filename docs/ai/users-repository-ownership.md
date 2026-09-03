# Users tracked repository ownership

Baseline: `de0f11423fce89f5124de462fa423ad3bddda2eb`, clean master.

## Decision and source evidence

Move the complete remaining UserRepository from central Infrastructure to
`Modules/Users/Infrastructure/Persistence/Users/UserRepository.cs`. The source is
unchanged: same CLR namespace, all query/write bodies, no assembly forwarder.
Its four existing repository/lookup/write/Google aliases share one scoped instance
registered by AddUsersPersistence. Existing hosts already call AddUsersModule.
Administrative and security-state readers remain separate scoped adapters; all
use the same shared FoodDiaryDbContext. Coordinated host rebuild is required.

Users owns the aggregate, roles, goals and credential state. Google issuer/subject
lookup is stored Users data, not a provider implementation. Source verification
corrected the initial planning shorthand: Identity application does not directly
consume aggregate ports. It consumes Users authentication capabilities, whose
Users implementations use these ports. No Authentication/Identity protocol,
authorization, credential algorithm, provider, HTTP or semantic contract changes.

Preserve active/non-deleted predicates on ordinary email/id/Telegram reads,
inclusive variants for lifecycle workflows, exact issuer AND subject matching,
split role loading, ID-specific goal hydration, and tracked identity/pending state.
Add and both Update overloads only stage changes. The caller still owns
SaveChanges, transaction boundaries and user/role-audit atomicity. Cancellation
behavior is unchanged, including existing write methods without database I/O.

The central registration file is removed and AddFeatureRepositories no longer
calls AddUserPersistence. No project/package/lock/solution/model/migration change
is needed. The central mixed Users/Identity integration flow is unchanged.

## Tests and verification

Seven existing Users repository/concurrency/role-membership tests move as a whole
file into the existing Users provider project without changing method bodies.
Five added PostgreSQL cases cover filtered/inclusive lookup and composite Google
identity, tracked reuse/goal loading/pending fields, staged add/detached update,
user-plus-audit commit/rollback, and cancelled lookups. Scoped DI tests retain
both composition orders and now verify module assembly, four alias lifetimes,
separation from both readers, and absence of all Users registrations centrally.
The central mixed DI fixture adds the already-used Users persistence entrypoint.
Existing reader guards are retargeted to the actual owner, not weakened; a new
guard rejects legacy ownership and implicit repository SaveChanges/transactions.

Run full solution build, complete module/donor/consumer/architecture suites, full
Users and central PostgreSQL suites, full HTTP integration, EF model comparison
and NuGet audit. No coverage collector, performance benchmark, deployment, push
or external security scan. Actual logs/TRX/source comparisons live under
`.artifacts/users-repository-evidence`; failed attempts are retained separately.

The first build failed because the relocated role-membership tests needed the
existing internal service's friend-assembly access in their new test assembly;
Users Infrastructure now grants that exact IVT, preserving service visibility and
test source. Two new test locals also required `var` under repository style.
These were task-introduced packaging/style failures, not baseline regressions.
The initial failed build log is retained; no runtime pass is attributed to it.

The first runtime pass exposed one unupdated central composition allowlist and
one incorrect new-test expectation. The allowlist drops only the moved Users
registration. A detached User loses EF's shadow `xmin`, so Update stages it but
SaveChanges rejects the missing concurrency version. The test now explicitly
checks the lost token, rejection and unchanged persisted row; no production
concurrency behavior is changed. Initial failed TRX files are retained, followed
by complete Users provider and architecture reruns after a full solution build.

Final runtime ledger: 4,431 passed cases in 15 unfiltered suites, zero failed or
skipped. Users PostgreSQL passed 30, central PostgreSQL 88 (seven moved cases),
HTTP 182 and architecture 1,042. The final full build had zero warnings/errors;
EF reported no pending model changes, retaining the existing tools 10.0.10 /
runtime 10.0.11 warning. NuGet audit covered all 319 solution projects with zero
vulnerable entries/problems. Latest successful per-suite TRX files are counted
once; earlier failures/reruns are retained but not double-counted. Own build
outputs were cleaned using dotnet clean, while logs and Wiki caches were retained.

## Wiki use and limitations

Research found the prior administrative-read split and role-audit extraction;
it ranked a broad security/deployment commit above the immediate ownership
precedent. Ownership returned empty arrays despite exact source paths. Current
source proves Users capability consumers and scoped identity; inferred names are
not ownership authority. The first planned manifest path was incorrect; native
contract expansion records the discovered actual manifest/docs/eval paths before
their edits. No claim that initial planning anticipated these paths.

One source-proven expectedPath in context-search-postfix-control-30 moves with the
byte-equivalent repository. Reverse substitution must reproduce the old corpus;
queries, cohorts, thresholds, ranking policy and frozen100 are unchanged. Keep
clean-base and final frozen100 measurements separate from bounded Wiki verify.
No generic Wiki tool/ranking repair is part of this extraction.

Actual affected Wiki verify exited 1 after 272.64 seconds at context-bundle:
frozen100 top10=99<100. This is not a clean Wiki pass. The exact clean baseline
and changed graph both measured top1=96/100, top10=99/100, MRR=0.9719, with the
same sole miss holdout100-conv-001 at rank 45. Corpus, ranking and retrieval
runtime fingerprints match; this is correctness evidence, not a timing benchmark.
The native passed-with-known-baseline-failures status applies only to that
executed Wiki failure, with the original exit 1/log and comparison retained.
It does not create an aggregate PASS receipt or executed-pass cache entry.

Adaptive-evals passed (77.71 seconds). The separate relocated postfix control30
evaluation passed semantically: top1=28/30, top10=30/30, moved repository case
postfix-db-005 at rank 1. SQLite retrieval 9/9 and SQL shadow passed before the
frozen100 failure; later context-bundle assertions were not reached and are not
claimed complete. Final report/index reviews and the remaining failure-knowledge,
change-policy and source-impact stages are recorded separately. No full verify
retry, ranking adjustment or gate relaxation conceals the known quality failure.

## Remaining shared infrastructure

The shared context/migrations, unit of work, locking, generic audit storage,
outbox engine/claiming and shared SSO storage retain their owners. Review the
four-stream dead-letter replay dependencies separately before introducing stream
adapters; do not move or redesign that engine as part of Users ownership.
