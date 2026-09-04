# Module error factory ownership

Five unchanged error factories now belong to the existing Application/Abstractions
projects of DailyAdvices, Dietologist, Fasting, Hydration and Meals. Legacy
namespaces, method/property signatures, default values, codes, messages and
ErrorKind values are preserved. Central Errors.* classes remain unchanged
delegating facades; no duplicate factory or new project is introduced.

DailyAdvices and Meals add direct shared Results references. Dietologist replaces
its obsolete central Application.Abstractions reference with Results: its source
uses only own models, owner Domain entities/IDs and Results. Central facades now
reference all five owners; the reverse edge is forbidden by architecture tests.
All other consumer and host references remain unchanged.

Compatibility means a coordinated rebuild of repository consumers/hosts, not old
precompiled declaring-assembly compatibility. The owner confirmed there are no
external consumers. No HTTP implementation, authorization, serialization,
repository, transaction, Domain, EF mapping, migration, snapshot or provider change
is made. In particular, Hydration inaccessible entries remain NotFound and Meals
InvalidData remains Internal; this relocation does not redesign error policy.

ModuleErrorFacadeTests exercise every moved factory member against literal
codes/messages/kinds and compare the unchanged central facade. Assembly tests
verify actual module ownership; structural tests reject donor duplicates and
reverse central references. Runtime logs, source-equivalence and lock audits,
initial failures if any, and Wiki/governance receipts are retained separately in
`.artifacts/module-errors-evidence`. No coverage collector is used.

Wiki start/research and design were used before changes. Research inferred a
generic blocking design decision and unrelated Admin paths from the broad central
folder; the explicit source-grounded compatibility decision resolved it without
inventing a product decision. Current code and compile checks remain authoritative.
The canonical manifest drops only the five now source-less central ownership keys;
Git does not track empty local directories. No generic Wiki tooling or ranking
change is needed for this move.

Initial acceptance incorrectly inferred four notification-delivery requirements
(recipient, idempotency, retry safety and delivery tests) from broad shared-area
context. They are explicitly not applicable to this source-identical error-factory
move: no notification producer, payload, recipient selection, outbox, delivery
provider or retry code changes. They must not be represented as passed delivery
tests. Actual error ownership and compatibility criteria are verified separately.

The initial test-plan suggested only central Application and Architecture projects;
the five owner application suites and Presentation were added from source consumer
evidence. Five independent owner-Abstractions builds prove the new one-way graph,
including Dietologist without the central dependency. No database behavior is
changed, so this tranche does not claim a new PostgreSQL/EF-model execution.

The first full solution build stopped on MA0003 in the newly added test's explicit
null locale argument. Naming it `locale: null` fixes the readability diagnostic;
the initial log is retained. No production change or analyzer suppression was used.

Initial architecture verification found the missing root link to the new scoped
Dietologist guide and JobManager's missing restore/source COPY for its new
transitive Hydration Abstractions dependency. Both exact metadata omissions were
fixed, without relaxing tests. The native task contract was extended to those two
paths before editing; the original contract and failed TRX are retained.

## Final runtime verification

Eight complete unfiltered suites: 2,901 passed, zero failed/skipped. Counts use
only the final run for each suite: DailyAdvices 18, Dietologist 249, Fasting 167,
Hydration 50, Meals 161, central Application 373, Architecture 1,058 and
Presentation 825. The 12 new central tests protect cross-assembly facade
compatibility; five new architecture cases enforce ownership/dependency direction.
Initial architecture was 1,056 passed / 2 failed; the full rerun passed 1,058.

Force-evaluate and locked restores passed. All five owner Abstractions projects
built independently. Successful full solution rebuild: zero warnings/errors,
43.57 seconds after the test-only MA0003 fix. Subsequent root-guide/Dockerfile
metadata corrections were checked by the complete architecture rerun; no C#
source changed after that successful build. All 150 changed existing lockfiles
retain resolved package versions. Actual audit/Wiki/governance/hook results are
stored separately; navigation test hits are not execution evidence.
