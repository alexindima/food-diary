# Exercises module extraction: Wiki findings and verification

## Scope and source baseline

Base `3851a98e0340fd12191c56cf41f261a678a632c8`, branch `codex/exercises-module-extraction`, isolated worktree `C:/Users/alexi/.codex/worktrees/0d67/FD`. Initial status was clean and HEAD exactly matched the requested local master baseline. No origin/master substitution, master change, production action or push.

The pre-edit inventory is `docs/ai/exercises-ownership-inventory.md`. Six production projects and two focused test projects live under `Modules/Exercises`. Application keeps its legacy project/assembly name `FoodDiary.Application.Exercises`; the other projects use `FoodDiary.Modules.Exercises.*`. No empty wrapper layers were added.

## Proven central seams

- `ExerciseEntry.User` has no inverse User navigation. The unchanged mapping uses `HasOne(e => e.User).WithMany()`. Searching central Domain for ExerciseEntry/ExerciseEntryId/ExerciseType found only the three owned source files, so extraction is acyclic.
- Central `DomainGuard` is internal. Existing module Domain IVT entries establish the seam; add only `FoodDiary.Modules.Exercises.Domain`. Keep User/UserId and DomainGuard central. Preserve the EF navigation setter using the same RCS1170 project override as other extracted domains.
- `Errors.Exercise` is a public nested partial compatibility facade and remains in central Abstractions. It delegates to module-owned ExerciseErrors. Central Abstractions references module Abstractions; module Abstractions references only Exercises Domain and Results, never central Abstractions.
- Central DbContext/DbSet, migrations and snapshot remain central. Central Infrastructure references Domain/PersistenceModel, not Exercises Infrastructure. Module Infrastructure references central Infrastructure for the shared context. `ApplyExercisesPersistenceModel` explicitly includes the unchanged mapping.
- Stable read-service/DTO CLR namespaces are preserved in Contracts; Dashboard and TDEE now reference Contracts, not Application. Repository adapter namespace changes to module Infrastructure; the adapter remains internal with test-only IVT.
- All in-repository consumers rebuild together. No precompiled external binary compatibility is promised for types moving between assemblies. HTTP/JSON shape, enum values, table/schema and business behavior are unchanged.

## Wiki discovery: useful evidence and limitations

- Wiki module page found the application slices, abstraction source area, owned EF configuration, HTTP endpoints and three central Exercises application suites. Scoped brief correctly identified Exercises plus Dashboard/TDEE, domain/persistence/privacy risks.
- False negatives: the module page omitted ExerciseEntryInvariantTests, Exercises methods inside TrackingEntryInvariantTests, relational PersistenceRepositoryCoverageIntegrationTests, DomainGuard/IVT and central Errors.Exercise reverse dependency. Current-source searches and project graph inspection supplied these missing seams.
- The supplied TrackingAndMealPlanCoverageGapTests lead is stale for this exact base: that file contains only MealPlanning tests. It was not changed. Exercises methods in TrackingEntryInvariantTests were split into the module Domain test project while Hydration methods remained central.
- Consumer classification is imprecise: the generated page described Dashboard/TDEE as host/adapter consumers despite their actual application read-service dependency. Sources remain authoritative.
- `start` used JSON fallback because TypeScript prerequisites were initially absent, but captured a nonempty 54-path architectural task workspace with 12 acceptance criteria. `npm ci` subsequently installed prerequisites and the real Husky launcher.
- A first `brief -Module Exercises` followed only the then-current inventory documentation diff and reported Documentation/Ai; rerunning with explicit PlannedPath array correctly reported Exercises/Backend/Database/Tests and Dashboard/TDEE. This is recorded as invocation sensitivity, not a proven generic generator defect.
- Research, decision, ownership, test-plan, privacy, dependencies, rollout and trace were invoked. No new provider/job/webhook/topology behavior exists; Docker/host references are packaging/composition only. Wiki broad rollout suggestions for schema/API/jobs do not imply those behaviors changed.
- No generic Wiki implementation was changed and no previously fixed Billing/OpenFoodFacts/Wearables DI failure is assumed to be baseline.

## Behavioral review

Source comparison against the base confirms unchanged entity/enum/ID/EF mapping and all application slice, validation, read-service, parser and mapping bodies. Calories remain finite 0..10000, one-decimal ToEven rounding; duration 1..1440; optional text trimming/limits and clear flags unchanged. Date normalization, user access and user-scoped lookup predicates, cancellation and shared transaction commits remain unchanged. Relational flow gained explicit foreign-user and neighboring-date assertions. No migration/snapshot or HTTP transport edit.

## Verification

Executed checks (no coverage collection):

| Check | Actual result |
| --- | --- |
| Force-evaluate restore / full solution build | Passed; 0 warnings, 0 errors; build 3m03s |
| Exercises Application | 34 passed, 0 skipped |
| Exercises Domain | 33 passed, 0 skipped |
| Central Application donor / Dashboard | 1328 passed, 0 skipped |
| Central Domain donor | 1008 passed, 0 skipped |
| TDEE | 35 passed, 0 skipped |
| Central Infrastructure DI suite | 101 passed, 0 skipped |
| Exercises presentation | 10 passed, 0 skipped |
| Full ArchitectureTests | 756 passed, 0 skipped |
| PostgreSQL tracking relational flow | 1 passed, 0 skipped; real postgres:17-alpine container |
| Full Infrastructure integration suite | 115 passed, 0 failed, 0 skipped; 9m27s; full unfiltered PostgreSQL suite |
| EF pending model | Exit 0; no model changes; tool/runtime 10.0.10/10.0.11 version warning |
| NuGet vulnerable package audit | Exit 0; no vulnerable packages reported |
| Wiki architecture health | 544 production edges, 282 test edges; no enforced drift at execution |

The initial architecture run found 5 extraction-related metadata/guardrail gaps; all were corrected and the full suite rerun. An earlier no-build architecture invocation found no assembly and is not counted as testing. The full Infrastructure integration suite subsequently ran once without a filter after force-evaluate restore and build (0 warnings/errors). Its 115 cases include TrackingRepositories_CoverDateFiltersTotalsAndFastingQueries; do not count the earlier focused run again. The disjoint final suites total 3420 passed, zero skipped. TDEE tests retain an explicit Application reference because their cross-module fixture creates the concrete Exercises read service; production TDEE references Contracts only.

Final Wiki/governance outcomes are recorded below. The commit hook runs after this report is staged; its actual outcome is supplied in the final task handoff. Actual logs and TRX are under `.artifacts/exercises-extraction-evidence`; reusable build outputs are only `.artifacts/exercises-extraction`. No coverage collector was invoked. Initial build configuration errors (namespace roots, EF setter analyzer, a donor unused using) were corrected without behavior changes.

## Integration notes

Likely conflicts with concurrent extractions: FoodDiary.slnx, project references/lockfiles, central Domain AssemblyInfo, central Infrastructure registration/model composition, Docker COPY lists, root guides, backend-modules manifest, ProjectDependencyMatrixTests, ApplicationGuardrailTests and generated Wiki. Preserve each module's independent additions. MealPlanning and RecipeCommunity production sources were not refactored. Do not replace these shared files wholesale when integrating.

## Governance progress and tool prerequisites

Delivery replan initially failed atomically because the SQLite compiled-index projection was missing. A graph-build during source/lockfile updates correctly rejected a changing worktree; a stable retry succeeded (7080 files, 31480 symbols). The subsequent replan succeeded with 253 observed/planned paths. The refreshed Exercises module page includes all six layer roots, but still misses Dashboard/TDEE consumers when legacy CLR contract namespaces are preserved; this remains a discovery limitation, not a reason to change runtime namespaces.

## Initial Wiki and governance outcomes (before verification closure)

- Wiki update succeeded; final `verify` passed all 7 selected stages in 37.6s, including workspace policy, page contracts, lint regression, indexes and source impact.
- Source-impact review recorded 16 affected pages across four reasoned review areas. The first invocation without ReviewAreaReason was rejected; it was rerun with explicit API, privacy, quality and documentation rationales.
- Stable graph rebuild succeeded after final source edits (7080 files, 31480 symbols). Missing/stale projection prerequisites were repaired using graph-build; no Wiki helper was modified.
- `delivery-validate -FailOnInvalid`: **failed**. Packet freshness, requirements, acceptance, plan conformance and proof-of-change pass. Ten criteria are satisfied; two migration criteria are not applicable because no migration is needed. Evidence gate retains two unresolved full Infrastructure integration checks and two lineage issues. The selected real relational test passed, but this report does not claim the entire Infrastructure integration suite ran.
- `delivery-critique -FailOnInvalid`: **failed**, verdict **reject**, score **19/100**. Findings: `security-context-unassessed` (warning, missing AI context trust assessment) and `verification-unresolved` (critical, unresolved verification evidence). These governance outcomes are not green and must be reviewed by the integrating task; they are not asserted to be pre-existing baseline failures or product vulnerabilities.
- No shared Windows argv-too-long helper fix was attempted; the coordinating MealPlanning task owns that work. The observed failures here were exact-path/check-ID mapping prerequisites and missing/stale SQLite projections, followed by the disclosed unresolved evidence/context gates.
- After preserving every log/TRX and EF result, `dotnet clean FoodDiary.slnx --artifacts-path C:/Users/alexi/.codex/worktrees/0d67/FD/.artifacts/exercises-extraction -m:2` succeeded (0 warnings/errors). Only this resolved build scope was targeted. Evidence remains in `.artifacts/exercises-extraction-evidence`; Wiki caches were preserved.

## Verification closure after the implementation commit

Implementation commit `569324fc95cbcaf8fe9cd5ee14d9cbd534f35f68` passed real pre-commit hooks: Wiki freshness, full solution build (0 warnings/errors, 5m14s), C# format and migration build guard. Hook scope `.artifacts/pre-commit/1948` was removed normally and the worktree was clean. The earlier failed governance results above are historical, not integration approval.

The two outstanding policy IDs, `data-access-integration-tests` and `infrastructure-integration-tests`, require the same full Infrastructure integration command. One unfiltered run passed all 115 tests and is retained as `Infrastructure-integration-full.trx`. Both receipts use the existing `New-LlmWikiEvidenceLineage.ps1` builder with policy Definition and actual Command separated; no signatures or fingerprints were handcrafted. The wrapper currently uses actual command flags as Definition and can produce mismatches. No generic Wiki implementation was changed.

AI context trust assessment uses `task-context-security-create` and `task-context-security-verify`, not a product security scan. The receipt reports valid, zero findings and zero quarantines, but **does not establish full context coverage**: of 272 selected sources, only 107 were scanned as existing; 165 were missing. Of those, 164 actually exist with their leading dot restored (`.llm-wiki` and `.nuget`), and one is the deleted legacy Exercises project. `Manage-LlmWikiContextSecurity.ps1` uses `TrimStart('./')`, which removes significant leading dots. The coordinating MealPlanning task owns the generic fix and regression. No scanner fix, policy-limit relaxation or unfinished foreign changes were copied here. The integrating task must rerun the corrected assessment; the valid receipt and zero findings must not substitute for that follow-up. Runtime test results are unaffected.

A subsequent critique reached approve, 95/100, while delivery still rejected two supplemental non-policy IDs. Native delivery-replan retired those supplemental IDs into history. Their actual focused tests and EF evidence remain retained and mapped through applicable reviews. The committed-versus-staged rename representation changed matched paths during refresh; affected receipts and reviews were reconciled against unchanged committed product content, preserving prior evidence snapshots. Migration criteria remain not applicable because the executed EF comparison proves no model change. Final `delivery-validate -FailOnInvalid` passed: 12 criteria (10 satisfied, two justified not applicable), zero unmapped/unverified criteria, zero unresolved checks/reviews/lineage issues, 218 planned changed paths and no out-of-scope paths. This tool-level result does not remove the context-scanner limitation above.

Final `delivery-critique -FailOnInvalid` returned **approve, 95/100, valid=True**. Wiki closure verify passed all seven stages. These machine outcomes do not prove complete AI context scanning; the 164 false-missing dot-path sources above still require the coordinated scanner fix and reassessment.

After the full integration run, a second `dotnet clean` targeted only the verified ordinary Exercises artifacts scope and succeeded with 0 warnings/errors. No TRX, EF logs, Wiki caches, other worktrees or foreign processes were removed. A separate documentation commit records this closure; no implementation history is rewritten. No push or master integration is performed by this task.
