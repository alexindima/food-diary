# Admin extraction: Wiki evaluation and verification

Worktree: `C:/Users/alexi/.codex/worktrees/5990/FD`.
Base: `4c1b1c2c31a0886cf7e01d8bba7ec00231e6b149`.
Branch: `codex/admin-module-extraction`. Evidence: `.artifacts/admin-evidence`.
Runtime extraction is verified; Wiki quality and governed delivery are reported separately below.

## Source-grounded scope

See [ownership inventory](admin-ownership-inventory.md). Admin owns application
orchestration, billing-report/impersonation/mail-reader ports, the independent
AdminImpersonationSession entity, its mapping and reporting/session/handoff adapters.
Identity Email, Users role audit, shared runtime/DbContext/migrations, transport,
MailInbox client integration and mixed tests retain their owners.

## Actual Wiki use

- Root and scoped guides, index, ADR 0006/0009/0016 and ownership sources were read
  before production changes. The generated Admin page found useful source areas,
  but legacy namespace attribution overstated ownership of Email/User audit.
- `start` used a real eight-element PlannedPath array: legacy Admin application,
  central Admin ports, Admin entity, central Admin persistence, Modules/Admin,
  central Admin tests, FoodDiary.slnx and backend-modules.json. Actual exit 0;
  cold checkout explicitly selected JSON. It created the governed `tasks/current`
  workspace with 71 inferred paths and 16 acceptance criteria. Notification and
  migration criteria need applicability review; receipts are not execution proof.
- Exact-source `research`, `brief`, `test-plan`, `decision`: actual exit 0.
  Research correctly located the impersonation handler and requested a boundary
  decision. Design recorded the source-grounded decision and rejected moving
  foreign Email/Users ownership; actual exit 0, semantic `ready=True`.
- Exact-source test-plan found central Admin tests and both relevant shared
  PostgreSQL suites. It also suggested unrelated frontend meal/recipe/measurement
  card specs. These are relevance false positives, not mandatory runtime proof.
- `privacy`, `topology`, and Backend `trace` executed separately. Trace found the
  handler, shared JWT/Users/SSO-adapter dependencies and concrete mixed/presentation
  tests. Privacy found IP/user-agent and credential fields. Topology returned broad
  provider leads which were checked against actual ownership rather than moved.

## Reproducible limitations

1. Planned non-existent module directory is treated as a file. The actual `start`
   above wrote `^Modules/Admin$` in allowedPathPatterns, excluding descendants.
   Expected scope includes the explicitly planned logical module's contents.
   Original contract and native scope delta are retained as
   `task-contract-original.json` and `task-contract-scope-delta.json`. Parent
   authorized native task-init/replan correction for this task only. No generic
   generator, fingerprint or receipt was edited manually.
2. `ownership -PlannedPath <exact Admin files> -CompiledIndexSource Json -Format Json`
   failed with actual process exit 1 in Get-LlmWikiDiffContext.ps1:160:
   `compiled-index-projection-missing`. The facade did not forward JSON to the
   nested diff. Its empty redirected output is not a success receipt. The parent
   was informed; general Wiki tooling remains unchanged. Later commands from that
   aborted chain were not run and must be recorded separately.
3. `decision` with PlannedPath returned current documentation/Ai diff evidence,
   rather than the planned project boundary. Its no-trigger result does not remove
   the architecture review requirement; ADR0016 and the source inventory govern it.

## Verification status

Static XML/path audit: 265 unique solution projects, all csproj and explicit
ProjectReference paths exist; expected project-matrix sets match the edited graph.
This is not an MSBuild or test run. Production source moves preserve CLR namespaces;
there is no migration or snapshot edit. Actual build, test and EF results are recorded below; Wiki/gov outcomes remain separate.

Known supplied master Wiki quality is non-green (frozen100 top10 96/100,
acceptedPrecision .9481, errorCaptureRate .5). Those historical measurements are
not a new Admin measurement, and unrelated failures cannot be assumed baseline.

## Recorded intermediate execution

The initial isolated force-evaluate restore and locked restore exited 0. Full
FoodDiary.slnx build exited 0 with zero warnings/errors. Admin Application tests:
52 passed; Admin Domain: 9 passed; neither skipped tests. Native clean exited 0.
These are the first-pass results; final donor/consumer/EF verification is separate.

Under the next exclusive slot, npm ci, native Wiki update and graph-build each
exited 0. The graph contains 7,154 files and 31,577 symbols. Native clean exited 0.
One bounded retry after graph creation succeeded for ownership, Backend trace and
exact-source test-plan (each process exit 0). Delivery-replan exited 0 with
applied=True, 361 observed/planned paths, seven phases and six required checks.
The original failed cold-index reproduction remains retained. The new test-plan
still includes the deleted donor AdminValidatorTests path alongside its new path,
and unrelated frontend suggestions; process success does not imply perfect recall.

npm ci reported three high-severity vulnerabilities and four deprecation warnings:
Angular animations/platform-browser-dynamic and Angular Webpack tooling. Its log
contains no advisory IDs or vulnerable package names. package.json/package-lock.json
are unchanged; no npm audit/fix or dependency upgrade was run. This has not been
compared with the exact baseline and is not a baseline-confirmed security result.
It is separate from any NuGet audit. Evidence: npm-ci.log and npm-warning-audit.json.

## Bounded retrieval compatibility maintenance

The parent authorized two source-proven structural-role path remaps. A broad Admin
application prefix would additionally boost the newly nested AdminBillingListFilter
port. The rejected broad remap is reproduced in ranking-runtime-regression.json.
The existing engine/loader already supports excludedPathPrefixes; no algorithm was
changed. The final policy remaps Commands and Application and excludes nested
Application/Abstractions from the latter. All other rules, terms, weights and
thresholds remain unchanged. The full path candidate sets match: 13 validator
sources and 135 application implementation sources. Seven engine-branch regression
cases preserve AdminBillingQueryFilters=1000 and command-validator=900, while the
new abstraction filter and foreign sources receive zero. Audits: ranking-original.json,
ranking-candidate-audit.json and ranking-runtime-regression.json.

Five eval fixtures and the SQLite synthetic test fixture receive only proven
old-to-new source paths. Queries, IDs, thresholds and frozen history are unchanged.
Original holdout100 SHA-256:
46bdce257039fe528d6a87f7d611385f0a7149298d2b8ed878e3a32e7dd08122.
Per-file hashes and mappings are in fixture-remap-audit.json. Any subsequent corpus
measurements describe this remapped corpus, not the original baseline corpus.

The second full build also passed with zero warnings/errors; module tests remained
52+9 passed. The first full ArchitectureTests run failed: 812 passed, three failed,
zero skipped. Its TRX/log remain under final-FoodDiary.ArchitectureTests.*. The
failures exposed an omitted exact Admin ports reference expectation, an obsolete
MailInbox error-factory source path and per-project Docker source COPY omissions.
All three were corrected without removing assertions; the independent transitive
COPY audit now reports no missing copies. Subsequent suite results are separate.

## Final runtime verification

All commands used the repository-level `.artifacts/admin-extraction` scope and
retained logs/TRX in `.artifacts/admin-evidence`, without collectors. Runtime2:
force-evaluate restore 0; locked restore 0; full solution build 0 with zero
warnings/errors. All 16 unfiltered test projects passed, 5,262 cases total, zero
failed/skipped. The earlier failed Architecture run is not counted again.

| Suite | Passed |
| --- | ---: |
| Admin Application / Domain | 52 / 9 |
| Central Application / Domain | 974 / 868 |
| Architecture | 815 |
| Central Infrastructure unit | 651 |
| Presentation / WebApi unit / JobManager | 825 / 247 / 168 |
| Development MCP | 220 |
| Gamification / Ai / ContentReports / Lessons Application | 45 / 59 / 12 / 18 |
| Full Infrastructure.IntegrationTests | 117 |
| Full Web.Api.IntegrationTests, including performance and Swagger | 182 |

The complete PostgreSQL suite ran against real `postgres:17-alpine`; all 117
cases passed without filters or skips (process duration 368.4s). The HTTP suite
also ran unfiltered (182 passed, 77.6s). No performance budgets or expectations
were modified. `final-runtime-summary.json` independently totals the 16 TRX files
and stores their SHA-256 hashes; `runtime-processes.jsonl` records exact arguments,
exits and durations.

EF `migrations has-pending-model-changes` exited 0: no changes since the last
migration. Its only warning was tools 10.0.10 versus runtime 10.0.11; versions
were not upgraded. ArtifactsPath/UseArtifactsOutput were set only around the EF
and NuGet commands and restored afterward. Native clean completed with exit 0
before the exclusive handle was released.

NuGet vulnerability audit completed with exit 0 for 265 projects and zero
vulnerable package entries. This is distinct from the three high npm-ci findings.
Final retained-package resolved-version comparison against Git HEAD found zero
changes. Neither npm manifests nor npm lockfile changed.

## Actual Wiki quality gate

The first final native update, graph, page reviews and architecture-health check
all exited 0. Architecture health reported 625 production edges, 319 test edges
and no enforced drift. Actual `verify` exited 1 in affected context-bundle smoke;
policy/page contracts/lint/index stages and adaptive-evals smoke passed, later
verify stages were not reached. Its snapshot writer exited 0 but explicitly
reported `liveRegressionPassed=false`: top10 92/100, acceptedPrecision .9079,
errorCaptureRate .4167. Logs and the original snapshot are retained unchanged.

Four misses were extraction maintenance omissions: expectedPaths still named two
moved focused tests, the session entity and repository, while retrieval returned
the relocated files at ranks 1/1/2/1. Only those four exact expectedPaths were then
remapped using equal source hashes from Git/source audit. Evidence:
`additional-fixture-remap-audit.json` and `final-fixture-integrity-audit.json`.
All eval JSON content equals HEAD plus exact approved source mapping; no queries,
thresholds, ranking weights or historical snapshots changed. The other four
misses are not asserted to be baseline-confirmed. A subsequent remapped-corpus
measurement must be reported independently of this preserved 92/100 result.

The complete-remap snapshot exited 0 with semantic FAIL: top1 91/100,
top10 96/100, MRR .9295, acceptedPrecision .9474, errorCaptureRate .5556;
the domain-invariants cohort gate is also unmet (3<4). Corpus SHA-256:
03dece972b46b6bf9f0e3d90f88d2a4bcdcdbbc81648c671ac0e040562626ca4.
Actual repeated `verify` exited 1 with these same gaps after 196.8s;
architecture-health exited 0, native clean exited 0 before lock release.
The four remaining misses are holdout100-mixed-005 (rank21), domain-003 (12),
domain-006 (11), domain-010 (18). No further retrieval change was attempted.
The complete and incomplete remap snapshots are both retained under
`context-evaluations/`; their recorded tree fingerprints delimit each measurement.
Final report-only edits are not claimed to have been part of that snapshot.

## Governed delivery and limitations

Native final scope validation: 363 actual changed paths, zero out of scope.
Requirements and plan conformance returned process 0 and semantic valid=true;
acceptance resolved 15 satisfied and six not-applicable criteria. Eleven required
reviews were recorded with concrete ownership/security/dependency/rollout reasons.
Context-security returned process 0, valid=true, zero quarantine findings; the
anticipated scanner false positive did not occur in this assessment.

The first native proof creation returned process 0 but valid=false: fourteen
criteria lacked explicit changed-file links. The first required delivery-validate
and delivery-critique both exited 1; critique rejected for missing change links
and the failed wiki-verify check. Those initial receipts are retained. The evidence
correction uses native acceptance-map to link each criterion to its actual source,
project, mapping, host composition or test file; no receipt or hash is handcrafted.
The subsequent sealed-* logs under the evidence directory record the repeated
native proof/validate/critique results after that correction. Wiki quality remains
an unresolved required check; this report does not claim governed delivery is green.

Executed-check lineage is built with the native lineage builder, separating policy
Definition from actual Command and keeping real exits/durations. One complete,
unfiltered PostgreSQL run supports both equivalent integration policy IDs. Native
scope/lineage checks inspect the actual changed set. No generic generator, security
policy, performance budget, query/ranking weight or quality threshold was weakened.

Runtime verification is complete; integration must account separately for the
reported Wiki quality failure, npm's three unclassified high findings and the EF
tools/runtime warning. Frozen history is unchanged. Evidence is retained in this
worktree through integration, including unsuccessful runs and the explicitly
interrupted early wrapper attempt (`runtime-wrapper-interruption.txt`). No push,
production operation or worktree cleanup was performed. The final SHA and normal
hook result are supplied in the task handoff, outside this pre-commit report.
