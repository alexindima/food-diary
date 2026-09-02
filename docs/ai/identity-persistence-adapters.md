# Identity persistence adapter tranche

Baseline: `7ee80615e2bdf8b74558373597141a494e1e4ba3`, clean master.
Scope: only UserLoginEventRepository and EmailTemplateProvider, their DI and
focused tests. The broader audit is in
`docs/architecture/infrastructure-boundary-audit.md`; deferred items are not moved.

## Compatibility and ownership

Both production class bodies and CLR namespaces move unchanged. The existing
Identity adapter project acquires them without a new production dependency.
Central Infrastructure still owns the shared context/model composition and must
not reference Identity's adapter project. All three executable consumers already
call AddIdentityPersistence; that method now owns the three scoped login-event
aliases and singleton template provider registration.

Identity owns login events/templates. The repository's reporting join to Users is
a read projection, not a transfer of User aggregate ownership. Existing canonical
guides retained the repository centrally as a transitional seam; this tranche
closes that seam using the already-existing one-way dependency graph. No new ADR
is needed: this implements the accepted modular-monolith owner-placement policy,
not a new database, deployment or application-contract boundary.

SQL, filtering, paging, grouping, deletion cutoff/batch size, cancellation, cache
duration, normalization and English fallback are unchanged. No new identity data
collection, logging, retention rule, external call or authorization path is added.
Cache isolation/lifetime and repository alias identity are explicitly tested.
Coordinated host rebuilds are required; no old-binary forwarding guarantee is made.
EF entity/configuration/context/migration/snapshot sources are unchanged.

Two provider unit cases and three real PostgreSQL repository cases move without
duplicates. The unit test namespace is aligned with its new project; its test
bodies are unchanged. New module-owned unit tests cover module assembly ownership, scoped
aliases, singleton lifetime and cache reuse across database scopes. Shared fixtures
are linked, not copied; mixed central integration and Admin/JobManager/HTTP tests
remain central. No coverage collector is used.

## Wiki observations

Research located both adapters, the existing Identity registration, focused tests
and three downstream consumers. Design became ready after explicitly recording
the preserved boundaries. The decision query was noisy because it included the
preceding test-layout change; direct source/project evidence determines this scope.
Privacy listed identity fields but included many unrelated Users models.

The first start overlapped the preceding commit's completion and the graph guard
correctly refused changed inputs. That failed log is retained. The fresh start
captured zero existing changes at the baseline above and created a separate native
workspace under `.artifacts/llm-wiki/tasks/identity-persistence-adapters`.

Before this tranche, full Wiki verify failed frozen100 at 99/100: the valid
Shared DomainGuard target ranked45. This is a measured pre-tranche result, not a
reason to mark the final Wiki check passed. Ranking/thresholds and unrelated
fixtures are outside this tranche. Actual final results will be recorded below.

## Verification

Forced and locked solution restores passed. Full solution build passed with zero
warnings/errors. The first build exposed four test-only packaging/style errors
(two namespace locations and two unnamed bool arguments); they were corrected,
with the initial log retained and a complete successful rebuild.

Eleven final unfiltered suites passed, 2733 cases, zero failures/skips:

| Suite | Passed |
| --- | ---: |
| Identity Infrastructure | 5 |
| Identity PostgreSQL | 3 |
| Identity Application | 171 |
| Users Application | 156 |
| Dietologist Infrastructure | 11 |
| Central Application | 361 |
| Central Infrastructure | 563 |
| JobManager | 168 |
| Full central PostgreSQL | 99 |
| Full HTTP/Swagger integration | 182 |
| Full Architecture | 1014 |

EF has-pending-model-changes exited0, no changes. The existing tools10.0.10 versus
runtime10.0.11 warning remains; no tools were upgraded. NuGet audit exited0 with
316 projects and zero vulnerable packages/problems. Only two new test lockfiles
are added; existing lockfiles and all production project references are unchanged.
Git blob comparisons prove both production moves and the PostgreSQL test are
content-identical after normal Git line-ending normalization.

The affected Wiki verify route passed7/7 and architecture health reported no drift
(889 production, 535 test edges). This route does not select context-bundle or
frozen100, so it does not fix or waive the separately recorded DomainGuard search
limitation. Final report freshness and governed validation are separate receipts,
not implied by these runtime results.

Logs, source audit, actual commands and all11 TRX are retained in
`.artifacts/identity-persistence-evidence`. No collectors, push, deployment or
production access were performed.
