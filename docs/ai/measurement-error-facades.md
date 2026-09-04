# Measurement error facade retirement

## Boundary

Baseline: `f0c9f306e1e1b491a7c7b273a5e419eb7117318a`, clean master.
BodyMetrics already owns WeightEntryErrors and WaistEntryErrors; Exercises owns
ExerciseErrors. Their three central Errors wrappers only delegated to these factories.
All ten production call sites are inside the respective owners. The wrappers are
removed and eight handlers now call the unchanged factories directly.

The source audit compares each caller with the exact baseline after only receiver
replacement and import removal. Conditions, arguments, normalization, cancellation,
authorization, repository calls and side effects remain identical. The three factory
sources are unchanged. NotAccessible still has ErrorKind.NotFound; duplicate dates
remain Conflict. Codes, messages, null details and date formatting are preserved.

The three public nested C# facade types intentionally disappear. There are no external
repository consumers per the user; rebuild all hosts/consumers together. This is not
old-binary compatibility. HTTP transport, Swagger snapshots, DI, SQL, EF mappings,
domain types, migrations and provider behavior are unchanged. No deployment, push or
coverage collector is part of this task; rollback is a source rebuild, not data work.

## Dependency evidence

Central Application.Abstractions has 18 direct references instead of 20: the BodyMetrics
and Exercises owner Abstractions exports are removed. Twenty-two explicit existing
owner dependencies are added in 18 consumers, so 19 project files change including
central. Every added edge has baseline graph reachability and an existing emitted
assembly reference in the preceding commit's recorded PE inventory. This is historical
navigation evidence, not a substitute for the current build and tests.

Consumers include both owner adapters/tests and existing Dashboard, Statistics, Tdee,
WeeklyCheckIn, Users/Dietologist tests, presentation and central infrastructure tests.
This exposes existing contract use; it grants no new aggregate mutation capability.
The exact matrix and four independent application reference-list guards are updated,
retaining equality assertions. No new project or package is introduced.

Rebuilt PE metadata was inspected for all 324 projects without executing assemblies.
Across 201 production assemblies the only reference-set change is central losing the
two selected owner assemblies. No emitted use of either owner lacks a direct reference.
This is dependency-set evidence, not byte-identical binaries or exhaustive detection
of inlined constants. Source and project XML audits independently bound the edits.

## Tests and verification

Nine new cases live in the two existing owner application suites. They verify factory
assembly/namespace and all ten error members against literal code/message/kind/details
expectations. BodyMetrics checks leap-day formatting under en-US, ru-RU and ar-SA,
restoring the current culture in finally. Existing handler authorization/conflict tests
remain in their owners. The mixed central compatibility tests are not relocated.

RetiredErrorFacadeTests now protects all three removed wrappers, declarations and
central owner references. Before removal, its three new rows failed and the previous
three passed. The obsolete delegation rows are replaced by retirement and owner
contract checks, not simply dropped.

The first full build passed with zero warnings/errors (230 seconds), as did the
independent trimmed central build. First full Architecture had four stale exact
reference-list failures, corrected by adding only the audited BodyMetrics edge.
Infrastructure's first run had one OpenFoodFacts cache-expiry KeyNotFoundException;
the full suite then passed without source changes. Both runs are retained. No exact-base
reproduction was performed, so this is not described as a proven baseline defect.
Actual EF comparison reports no pending model changes; the existing 10.0.10 tools /
10.0.11 runtime warning is retained, with no tooling upgrade.

Final runtime: 17 unfiltered suites, 4244 passed, zero failed/skipped. This includes
BodyMetrics 103, Exercises 37, Architecture 1085, central Infrastructure 550, complete
PostgreSQL 93 and full HTTP/Swagger 182. Counts select the latest actual TRX per project;
initial failures and repeated executions are not added to the total. Architecture was
rebuilt after its four expectation edits (zero warnings/errors) and rerun in full.
Whole-solution NuGet audit exits zero: 324 projects, zero vulnerable entries/problems.
All retained resolved versions in 147 changed existing lockfiles remain unchanged.

Final affected Wiki update/verify, source reviews, current context assessment and strict
delivery validation/critique are required before the ordinary hooks-enabled commit.
Their final logs/receipts are retained separately; intermediate status is not a pass.
Actual commands, failures, TRX, hashes and source/graph audits are retained under
`.artifacts/measurement-errors-evidence`; current governed state is under
`.artifacts/llm-wiki/tasks/measurement-errors`. No assertions, timeouts or policies are
weakened to pass verification.

## Wiki findings

Native start/research/brief/decision/test-plan/design ran before product edits. Design
recorded the source-grounded compatibility boundary and reported ready. Initial brief
classified the unedited scope low risk; initial test-plan suggested the previous
Fasting/Hydration error tests and mixed central compatibility suite. Those references
are useful precedents, not the right execution scope for this change.

A second test-plan using exact current handler/test paths correctly identified
BodyMetrics and Exercises, their application suites and architecture/full build. It
also proposed unrelated frontend cards; no frontend source or HTTP contract changed,
so those suggestions are not execution evidence. Ownership identified the eight actual
affected modules but reported no downstream modules. Privacy returned a broad candidate
inventory, not proof of unchanged authorization or error classification; direct source
comparison supplies that proof. Dependencies returned no manifest changes for that
invocation; the XML/PE audits establish the actual reference changes. No generic Wiki
generator, ranking, threshold or policy repair is included.

## Deferred work

Shared Errors taxonomy, resolver/UoW seams and other facade owners remain. Cycles has
an Export consumer; Meals has a Favorites error consumer; Notifications still returns
a Dietologist invitation error for a missing/inaccessible notification. Their removal
requires a separate cross-module error-contract review, not silent code/message changes
or new foreign aggregate dependencies in this batch.
