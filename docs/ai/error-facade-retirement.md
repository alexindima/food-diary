# Retiring owner-only central error facades

## Boundary and compatibility

Baseline: `cca556d95d7034e6afd3d50ea36d4a4668bbd982`, clean master.
DailyAdvices, Fasting and Hydration already own their factories in Application/Abstractions.
Their central `Errors.DailyAdvice`, `Errors.Fasting` and `Errors.HydrationEntry` wrappers
only delegated to those factories. All production callers of these three wrappers
are inside their respective modules. Remove the wrappers and call the owners directly.

The source audit covers 29 receiver substitutions in 11 production files. After
substituting the receiver and excluding import directives, caller source must equal
the exact baseline, including predicates, arguments, cancellation and side effects.
The three owner factories are unchanged. Codes, messages, locale/default handling,
not-found privacy, timestamp formatting and ErrorKind remain unchanged.

This deliberately removes three public central nested types. There are no external
repository consumers per the user; all hosts/consumers must be rebuilt together.
It is not a promise of old-binary compatibility. No HTTP, schema, provider, DI,
domain model or transport source changes are included. Rollback is a source rebuild,
not a data migration. No push, deployment or coverage collector is part of this work.

## Dependencies and tests

Central direct ProjectReferences fall from 23 to 20. Fifteen explicit references in
13 consumers represent existing use of the three owner assemblies, not new write
capabilities. Including central, 14 project files change. Each new edge has a path
witness in the exact baseline project graph and an existing emitted assembly use in
the preceding commit's recorded PE inventory. That inventory is historical evidence,
not a new execution; current compilation and tests are still required.

Current rebuilt PE metadata was inspected for all 324 projects without executing
assemblies. Across 201 production assemblies, the only reference-set delta is the
removal of these three owners from central Application.Abstractions. No emitted use
of the three owners lacks a direct reference. This is dependency-set evidence, not
a claim of byte-identical binaries or exhaustive discovery of inlined constants.

The central mixed compatibility suite loses only the eight DailyAdvices/Fasting/
Hydration factory ownership and exact-value cases. Equivalent checks now live in
the three existing owner Application test projects; they assert exact code, message,
kind and null details directly rather than comparing a factory to its wrapper.
Dietologist and Meals compatibility cases stay central. No new test project is needed.

`RetiredErrorFacadeTests` checks actual C# declarations through the repository syntax
reader, file absence and removal of central owner references. Its first compile exposed
a culture-sensitive diagnostic formatting issue; after fixing that, all three cases
failed on the original facades/references as expected. Existing facade guard rows are
replaced by these retirement assertions, not simply removed. Factory ownership guards
continue protecting all five original owners and the two remaining facades.

## Explicitly deferred

Notifications' MarkNotificationRead currently returns Dietologist.InvitationNotFound
for missing/foreign notifications. Favorites' AddFavoriteMeal uses Meal.NotFound.
Removing those facades would require an explicit cross-module error-contract decision;
silently changing their error codes or adding foreign aggregate ports is not part of
this tranche. Shared validation/authentication taxonomy, CurrentUserAccessResolver,
unit-of-work and the other central error facades remain.

## Verification evidence

Actual commands, failed attempts, source/reference audits, TRX and governed evidence
are retained under `.artifacts/error-facades-evidence`. The first full solution build
found redundant Fasting imports because its GlobalUsings already imports the owner
namespace, plus the legacy RootNamespace of the DailyAdvices test project; both were
corrected. This is an implementation failure, not a baseline exception.

Final runtime: 15 unfiltered suites, 3852 passed, zero failed/skipped. This includes
full Architecture 1085, DailyAdvices Application 22, Fasting Application 169,
Hydration Application 52, full central PostgreSQL 93 and full HTTP/Swagger 182;
the mixed donor, affected adapter/host and shared Results suites also pass.
Counts use the latest actual TRX per project, never initial failed attempts or
duplicate runs. The three-case red architecture run was deliberately filtered;
the final suites are not. No coverage collector ran.

Full solution build passes with zero warnings/errors (80 seconds), as does an
independent central build with 20 references. EF reports no pending model changes;
the existing tools 10.0.10/runtime 10.0.11 informational warning is retained.
Whole-solution NuGet audit exits zero, with no vulnerable package entries. All
retained resolved versions in the 147 changed existing locks remain identical.
The exact source/project/compiled-reference audits pass. No test assertion,
runtime timeout, package version or validation policy was weakened.

Wiki update/verify, source-impact reviews, current task-context assessment and
strict delivery validation/critique are required on the final diff before the
ordinary hooks-enabled commit. Their actual logs and final receipts are retained
with the runtime evidence; an intermediate exit code is not a substitute for a
completed gate.

## Wiki observations

Native start/research/decision/test-plan/brief/design ran before product edits; design
reported ready. Initial brief classified the unedited source scope as low risk and
returned only root/central/test guides, while test-plan listed the mixed compatibility
suite and Architecture, with no module list. Direct source inspection established the
three owners and required their own suites. No generic Wiki code, ranking, threshold
or policy change is included. Generated navigation is not execution evidence.
Subsequent ownership returned five direct, seven transitively impacted and two
downstream modules; direct source/compiled audits establish which dependencies
actually change. Privacy returned a broad repository candidate summary, not a
proof of changed data flow. One initial discovery wrapper incorrectly passed a
PowerShell array across `pwsh -File`; the corrected native array call completed
dependencies/rollout/ownership/privacy. That invocation error is not a Wiki defect.
