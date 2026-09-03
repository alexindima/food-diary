# Dead-letter replay stream boundary

Baseline: clean master `6638a5aa5fe64d2bbd6b6ecf03619f2429e31080`.

## Decision

Keep OutboxDeadLetterReplayService as the shared coordinator. Introduce the narrow
IOutboxReplayStream extension and OutboxReplayEntry in existing Infrastructure,
which all three owner adapters already depend on for FoodDiaryDbContext. Do not
grow the dependency-free IOutboxMessage primitive into an EF/application contract,
add a new assembly, or make central Infrastructure reference module adapters.

Images owns image-deletion SQL and object-key metadata; Notifications owns web-push
SQL and notification-id metadata; Gamification owns evaluation SQL and user-id
metadata. Generic email envelopes are produced by several owners, so email remains
shared. Its adapter supplies the unchanged replay rejection reason: sensitive
payloads have already been removed. Regenerate email through its originating flow.

Adapters register once per scope through existing module entrypoints. All three
hosts already compose these owners. Explicit order preserves the original
email/image/push/achievement tie order independently of host DI order. Duplicate
names fail rather than silently select a provider. No service locator, reflection
dispatch, provider invocation or new host/job/configuration is introduced.

## Preserved behavior

The coordinator retains name/limit/id/operator/reason/attempt validation, history,
stale-attempt and dead/unprocessed checks, clock, audit creation, MarkReplayed,
SaveChanges and relational transaction commit/disposal. Adapters use the same
scoped context and cannot save or commit. FOR UPDATE SQL, no-tracking list
projections and per-stream Take remain unchanged; merged lists keep global Take.
Preview metadata is captured alongside the same tracked lifecycle record.
Attempt count and achievement revision are not reset by this structural change.

Shared processing/claim/retry, stream lifecycle classes, migrations/model/context,
public application contracts, HTTP/authorization and delivery policies are not
changed. Coordinated host rebuild is required; no binary-forwarder guarantee.

Source review during extraction found a pre-existing defect: each non-locking Find branch called
SingleOrDefaultAsync on the entire DbSet without a messageId predicate. With
multiple rows preview can throw; with one row it can return that row for a
different id. Relational replay's FOR UPDATE query does filter by id. The extraction
preserved both branches; the separate corrective follow-up below adds the missing
predicate with single/multiple/missing-id regression tests.

## Verification and Wiki

Keep existing public replay assertions and mixed PostgreSQL replay coverage.
Replace the obsolete private concrete-type-switch reflection test with unknown
stream rejection through all four public service operations. Add owner adapter
tests plus shared real PostgreSQL ordering/metadata/audit, failed-save rollback,
competing replay and cancellation-during-lock cases. DI tests cover both ordering
and single scoped registrations; architectural guards prevent ownership regression.

Actual builds, unfiltered suites, EF comparison, NuGet audit, Wiki/Git baseline
measurements, failures and final governance receipts belong under
`.artifacts/outbox-replay-evidence`. No coverage collectors, deployment, external
calls or push. Keep failed runs separate from successful verification evidence.

Wiki research/design was used before editing; design is ready with the boundary
above. Initial ownership returned empty arrays despite the explicit module paths;
test-plan includes unrelated module fixtures. Current code and host composition
are authoritative. No deterministic ADR trigger matched; this is an extension of
the existing shared-engine/module-adapter pattern, documented here and in guides.
No generic Wiki generator, ranking, corpus or threshold changes are planned.

The first full build failed on two task-introduced analyzer issues: explicit
ordinal string comparison and matching interface/implementation cancellation
defaults. These were corrected without query/transaction changes; the failed log
is retained, not described as a baseline failure.

The second build exposed test-only analyzer requirements (async disposal, explicit
ordinal collection comparers, named literals, interface/var declarations and
TimeProvider-aware bounded waits). These were fixed as one test packaging pass;
the third full solution build passed with zero warnings and errors. Earlier
failures are retained separately and are not counted as successful verification.

### Completed runtime evidence

Both force-evaluate and locked restores passed. Full solution build passed in
42.93s (zero warnings/errors). Seventeen unfiltered suites passed 4,213 cases,
zero failed/skipped, with no collector:

| Suite | Passed |
| --- | ---: |
| Gamification Infrastructure / Application | 8 / 45 |
| Notifications Infrastructure / Application | 77 / 111 |
| Images Application / PostgreSQL | 40 / 11 |
| Identity / Dietologist / Admin Application | 171 / 249 / 52 |
| Central Infrastructure / Application | 527 / 361 |
| Presentation / WebApi unit / JobManager | 825 / 247 / 168 |
| Architecture | 1,047 |
| Full central PostgreSQL | 92 |
| Full HTTP integration / Swagger | 182 |

Central PostgreSQL includes the four new replay boundary tests; do not count them
again. EF comparison reports no pending model change; the existing tools10.0.10
versus runtime10.0.11 warning is retained. NuGet audit process and JSON are green:
319 solution projects, zero vulnerable entries/problems. Architecture health:
891 production edges, 542 test edges, no enforced drift. Full-build outputs were
recycled before provider work and cleaned afterwards using the one task-owned
repository-level scope; evidence and shared Wiki cache remain intact.

### Wiki result and limitations

Actual affected `wiki verify` passed 7/7 selected stages (exit0, 28.87s outer
duration), reviewing 48 affected pages. It selected zero focused tool-smoke groups;
this is not a full-tools or retrieval-quality certification. Graph refresh found
7,324 files / 31,994 symbols. No Wiki tooling, policy, corpus or ranking changed.

The separately executed frozen100 snapshot process returned0 but its semantic
quality gate remains false: clean exact baseline and changed graph both have
96 top1, 99 top10, MRR .9719, sole miss `holdout100-conv-001` at rank45
(`top10=99<100`). Corpus, ranking and retrieval-source fingerprints are identical;
each graph matches its change-set fingerprint. This unchanged supplemental
limitation is distinct from the actual green affected Wiki check; no failed check
is relabeled passed and no known-baseline exception is needed for that check.

Initial inferred HTTP/notification/job criteria are broader than this ownership
change. Compatibility is supported by unchanged source plus full existing
HTTP/notification/JobManager suites, not a new route, job or exactly-once promise.
Only migration Designer/snapshot-update criteria are inapplicable because no
migration/model change exists. Native final acceptance/context/delivery receipts
are retained with the evidence, separately from runtime results.

## Corrective follow-up: address non-locking lookups by message ID

Baseline: clean master `67f46de86490a5edc39f87f02f3be755638843dd`.

The four non-locking adapter branches filter by `message.Id == messageId` before
SingleOrDefaultAsync. Filtering belongs in the database query, not after loading
rows or through FirstOrDefault. Each table already has an Id primary key; no new
index or migration is needed. Tracking, cancellation, list queries and the exact
FOR UPDATE SQL remain unchanged. The coordinator still owns validation, preview
eligibility, transactions and audits; adapters still cannot save or reset rows.

Initializer's show-dead-letter and replay-outbox commands consume the preview.
Unknown IDs must never return another record; active records still have no
dead-letter preview. Non-relational replay also uses the non-locking branch:
an unknown ID must throw not-found without resetting another row or writing an
audit. Relational replay already uses the correctly filtered FOR UPDATE branch.
Email replay remains rejected after payload purging. No HTTP contract, provider,
message lifecycle, DI, dependency or persistence-model change is part of this fix.

Evidence is separate from the extraction: `.artifacts/outbox-lookup-evidence`.
New central regression theories cover all four streams and the three replayable
non-relational streams. A real PostgreSQL preview case uses one then multiple
records per stream, checks the requested metadata and unchanged tracked entity,
and verifies active/missing IDs and absent audits. Owner adapter tests additionally
check that Find still returns an active record (eligibility belongs to preview).
Existing provider lock/concurrency/rollback/cancellation cases remain intact.

Wiki develop/research/brief/test-plan/journeys and the required start/design route
were used. The first verbose intent caused overbroad architectural classification;
the explicit four-branch correction still selected governed feature work. Start
generated eight generic acceptance criteria instead of the supplied seven concrete
ones; the concrete scenarios are mapped to the primary-outcome criterion with
test evidence. No classification, ranking, threshold or generator code is changed.

### Lookup-fix runtime evidence

The first test build stopped on three task-introduced explicit-type analyzer
errors; these were corrected before execution. The original production then
failed all 11 new unit cases and the new PostgreSQL case: wrong-row preview,
multiple-row SingleOrDefault exception and wrong-row non-relational replay.
Those actual failed TRX/logs remain separate from final verification.

After the four predicates, six complete, unfiltered suites passed 1,774 cases,
zero failures/skips: central Infrastructure 538, central PostgreSQL 93, Images
PostgreSQL 11, Notifications Infrastructure 77, Gamification Infrastructure 8,
Architecture 1,047. The final PostgreSQL run took 4m41s and includes the new
preview case plus the unchanged lock/rollback/concurrency/cancellation cases.
Each project was restored in locked mode and built successfully with isolated
outputs; no package/lockfile edits or coverage collector. No old extraction run
is counted as execution evidence for this fix.

The production audit reverses exactly one predicate addition in each adapter to
recover its baseline source. No other production/project/model/host/DI/lifecycle
file changed. Query-only correction does not require another migration/model
comparison or HTTP snapshot change; this turn does not claim a new EF or HTTP
suite execution. Native clean removed this run's build outputs successfully
with zero warnings/errors, preserving logs/TRX and shared Wiki caches. Final
Wiki, context and delivery receipts are retained beside the runtime evidence.
