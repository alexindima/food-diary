# Images and Gamification outbox model ownership

Baseline: clean master `adfdd053b5e9b1f5ab2675f8abc386d2d800b85d`.
This tranche relocates four production files: each stream's technical record and
EF mapping. They belong to the existing Images/Gamification PersistenceModel
projects, not Domain aggregates. CLR namespaces and method bodies are preserved.

## Dependencies and unchanged mechanisms

Both model projects reference the existing dependency-free Outbox.Abstractions,
following Notifications. Images explicitly references the centrally versioned EF
Relational package used by its moved table/index mapping. No new project, shared
assembly or context-to-adapter edge is introduced. Existing
ApplyImagesPersistenceModel/ApplyGamificationPersistenceModel discover the moved
configurations. DbSets, context, historical migrations and snapshot stay central.
Coordinated host rebuilds are required; there is no old-binary forwarding promise.

Enqueue/dispatch adapters, generic processing engine, claimer, retry policy and
mixed dead-letter replay service are unchanged. Email/Notifications streams,
storage providers, schedules and host DI are not redesigned. Preserve image
IsConfirmed/object-key handling, pending achievement Revision coalescing, claim
release, due dates, retry counts and replay semantics. No exactly-once guarantee,
new data collection, logging, sharing or retention policy is introduced.

## Tests and evidence

Six isolated Images record Facts move without changing assertions from the central
ImageObjectDeletionOutboxTests into the existing Images Infrastructure.IntegrationTests
project. These plain Facts do not require Docker. Its new provider test round-trips
confirmation/lease/retry/dead-letter/replay state in migrated PostgreSQL. The five
existing enqueue/processor tests retain their shared-engine fixture centrally;
this tranche does not claim all image adapter tests have been relocated.

Gamification already owns its record lifecycle and revision-during-dispatch unit
tests. A new case verifies the model assembly and that RequestEvaluation retains
the active claim until explicit revision release. Existing shared PostgreSQL
tests cover concurrent requests, durable dispatch and pending updated revisions;
mixed replay/user-cleanup/HTTP suites retain their existing owners. Four physical
architecture cases plus the exact dependency matrix protect the new boundary.

Full solution restore/build, all selected complete module/donor/host suites,
unfiltered central PostgreSQL and HTTP suites, EF model comparison and NuGet audit
are recorded under `.artifacts/module-outbox-evidence`. No coverage collector,
push, deployment or external provider call is part of the task. Runtime and Wiki
outcomes are separate evidence, not inferred from navigation or exit-free output.

## Wiki observations

Native start captured the clean base and current-source/Git precedent research.
Design accepted the existing model-only precedent; no new ADR was indicated.
Ownership returned empty despite explicit record/model paths. Test-plan correctly
found both record suites and the mixed shared PostgreSQL tests; it did not know
the new files before implementation. These hints are checked against source.

Exactly one frozen100 expected path follows the source-identical achievement
mapping relocation. No query, cohort, ID, ranking, threshold or historical result
changes. The actual clean-base measurement is top1=96, top10=99, MRR=.9719, with
the sole DomainGuard miss at rank45. Final changed-graph measurement and actual
full Wiki verify remain separate checks; the pre-existing miss is not a green
quality gate. General search-ranking changes are outside this tranche.

The source audit confirms all four old/new Git blobs and all six moved test bodies
are equal. The generic engine, claimer, replay service, module processors, context
and migrations have no diff. Restore updates 72 dependency locks with no changed
existing resolved versions; only the Images model gains the already-used EF
Relational 10.0.11 and its Configuration.Abstractions dependency. One read-only
Wiki request hit a temporary SQLite snapshot cleanup lock; its failure is retained
separately from source discovery and runtime verification, with one bounded retry.

The first full build found two MA0003 findings in new tests; only the boolean
argument names were corrected. Initial failed build and the preliminary Wiki
attempt remain evidence, not successful final runs. Its index update completed
atomically; an attempted bounded process stop declined because the observed
reader had already exited. The subsequent style fixes invalidate that preliminary
attempt as final-source proof, requiring a fresh final update and verification.

The first complete architecture run passed 1022 cases and exposed one old central
path in ImageAndFavoriteConfigurations_StayInOwnedFolders. The exact Images
model path replaces that expectation, retaining the original existence assertion
and all cases. Initial failure and the complete rerun are recorded separately.
