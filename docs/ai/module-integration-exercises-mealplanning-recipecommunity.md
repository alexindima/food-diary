# Exercises, MealPlanning and RecipeCommunity integration

## Scope and provenance

Integration into local `master`, based on `3851a98e0340fd12191c56cf41f261a678a632c8`:

- MealPlanning: `708394e642e16470ee6eac4930e6e8833a2e15ed`.
- Exercises: `569324fc95cbcaf8fe9cd5ee14d9cbd534f35f68` and documentation closure `e355614b9d71d777cc8f585ef4b2d35ab601e981`.
- RecipeCommunity: `17ef351043ae1df3e83b5bac99cf072e8d0240b4`.

The existing coverage work remains intact. No push, production operation or coverage collector was used. Source worktrees and their evidence were not changed or removed.

Module-owned files match the completed source commits. Shared conflicts were merged as additions/removals from the common base, not by replacing shared files with one branch. This includes project references, module registrations, EF model registrations, Docker COPY paths, solution folders and architecture guardrails. The dependency matrix was reconciled from the three reviewed source matrices, not auto-generated from the resulting project graph. NuGet locks and generated Wiki indexes were rebuilt from the combined tree.

The solution has 242 distinct existing projects, including nested module test projects. Static XML/reference checks found no missing or duplicate project references. Legacy application assembly names and CLR namespaces/type names remain; relocated domain and port types belong to new assemblies, so consumers require the coordinated full rebuild validated here (not an old-binary compatibility promise). Shared DbContext, historical migrations and model snapshot are unchanged, as are HTTP snapshots. ShoppingList's public User navigation and central identity seams remain intentional; no cross-module Domain cycle was introduced. The individual ownership inventories explain the remaining seams.

## Combined-tree verification

All 19 recorded VSTest suites passed: **5,623 passed, zero failed, zero skipped**. These are combined-tree results, not a sum of the source tasks' overlapping runs.

| Suite | Passed |
| --- | ---: |
| Exercises Application / Domain | 34 / 33 |
| MealPlanning Application / Domain / PostgreSQL | 104 / 74 / 1 |
| RecipeCommunity Application / Domain | 33 / 9 |
| Central Application / Domain | 1,192 / 910 |
| Central Infrastructure unit / full PostgreSQL integration | 764 / 116 |
| Architecture | 780 |
| TDEE / Notifications / ContentReports Application | 35 / 111 / 12 |
| JobManager / Presentation / Web.Api unit | 168 / 825 / 247 |
| Web.Api integration excluding PostgresPerformanceBaselineTests | 175 |

The full central PostgreSQL suite had no filter and took 8m54s. The HTTP run deliberately excludes the unrelated performance-baseline class; this is not a claim that every test in the repository ran. No dotCover, XPlat or other collector ran.

- Forced solution restore: exit 0.
- Full solution build: zero warnings/errors, 4m45s, repository-level isolated output path.
- EF pending-model check: no changes since the last migration. Existing tooling warning: tools 10.0.10 versus runtime 10.0.11.
- Full transitive NuGet audit: 242 projects, no vulnerable packages reported.
- 131 changed lock files compared with the integration base: no resolved NuGet version changes.
- Architecture health: 570 production edges, 297 test edges, no enforced drift.
- Git overlay batching and context dot-path regressions: passed on the combined tree.
- Code graph native path-transport regression: passed, including oversized scope, equivalent SQL projection, UTF-8 and invalid-input rejection.
- Full code-graph regression passed after the two pre-existing module-path expectations were updated. A concurrent early attempt correctly rejected an index update in progress; the successful run was sequential and its log is retained separately.

Evidence is retained under `.artifacts/module-integration-exercises-mealplanning-results/`: 19 TRX files, per-suite logs, build/EF/audit logs, context assessments and Wiki diagnostics. Build outputs use the separate `.artifacts/module-integration-exercises-mealplanning/` scope.

## Wiki corrections and limitations

The MealPlanning commit supplies general fixes for length-budgeted Git pathspec batching and context-security path normalization. The latter preserves leading dots, normalizes only literal `./`, deduplicates aliases and rejects traversal. Stale Billing retrieval fixtures and 23 source-proven corpus path relocations are included; query text, cohort identities, historical measurements, ranking, thresholds and policy were not tuned.

Integration exposed a second Windows argument-length problem: the combined 462-path diff required 36,411 characters for the joined path value alone. Native Node startup failed during task refresh, surfaced by PowerShell as `StandardOutputEncoding is only supported when standard output is redirected`. Redirecting outer output did not repair it. The CodeGraph wrapper now sends oversized path arrays as UTF-8 JSON through stdin, with validation at the existing Node boundary. Smaller scopes retain their existing argv route. No paths are truncated and query semantics are unchanged. Regression tests compare both fingerprints and compiled projections and reject malformed or conflicting transports.

The broader code-graph regression also retained the removed `FoodDiary.Application.OpenFoodFacts/` application root as its expected prefix. The full Wiki run exposed this stale fixture; commit `52e6e0d6b` already relocated the application project to `Modules/OpenFoodFacts/Application/` before the integration base. The expected module application root is updated accordingly. Its notification ownership assertion likewise follows the existing relocation from central Application/Integrations into Notifications Application/Infrastructure. The test previously accepted the same WebPush sender under Integrations for a Telegram query; this channel ambiguity is not solved by path maintenance. These are module-ownership smoke assertions, not changes to frozen holdout targets or search ranking. Initial failed runs remain in the retained logs.

The full Wiki gate **failed**. MealPlanning measured the original frozen corpus on both fresh exact-base and source-task indexes: top-1 83/100, top-10 84/100, accepted precision .8846, error capture .4706. After source-proven path maintenance, both measured 94/100 and 98/100, precision .9615, error capture .5. Residual misses were DailyAdvice rank 11 and Notification rank 19; top-10 >=100 and error capture >=.9 still failed on the exact base. See the MealPlanning report for the 23-row Git/source audit.

The combined-tree full verify actually returned exit 1 at the same holdout gates. Independent current measurement in `holdout-combined.json`: top-1 **94/100**, top-10 **98/100**, MRR **.953**, accepted precision **.9615**, error capture **.5**. The same two case IDs miss: `holdout100-domain-006` at rank 11 and `holdout100-domain-010` at rank 18 (previously 19). The failing thresholds are unchanged; this is not a clean Wiki pass. Code-graph, adaptive-evals and tool-contract smoke groups passed; SQL context retrieval passed 9/9 and shadow comparison passed before the holdout failure. Checks later in the interrupted sequence are executed separately and retained, without rewriting the failed full-run result.

The interrupted checks were then completed separately: context-cache and workflow-recovery regressions, Users context lookup, the entire uncached read-only-guard group (226.49s, including cold-checkout retrieval/planning contracts), and the failure-knowledge, change-policy, source-impact and index stages all passed. These successful results do not turn the failed holdout or full-run exit code into a pass.

## Corrected context assessment

Original child scope paths were reread from each manifest and packet, not reconstructed from the old lossy receipts. The fixed scanner evaluated those scopes against the combined master working tree; every existence flag and existing-file SHA-256 was independently checked. These new assessments bind to the integration workspace/packet, not the old child's fingerprint.

| Original task scope | Sources | Existing/scanned | Genuine deleted paths | False missing / truncated / findings |
| --- | ---: | ---: | ---: | --- |
| Exercises | 272 | 271 | 1 | 0 / 0 / 0 |
| RecipeCommunity | 313 | 268 | 45 | 0 / 0 / 0 |
| MealPlanning | 359 | 356 | 3 | 0 / 0 / 0 |

All missing paths were verified against actual Git deletions in the combined diff. Exercises' previous 164 false-missing paths and RecipeCommunity's 163 are now scanned. RecipeCommunity's late scanner caveat was supplied in its handoff after its report was frozen; the old machine score of 95/100 was **not** proof of a complete scan. This report preserves that caveat and the corrected evidence.

These are standalone pattern-based context assessments, not exhaustive security audits or proof of a selected context bundle. MealPlanning's required bundle exceeded the unchanged 100-item maximum; no scope truncation, policy relaxation or selected-bundle completion is claimed.

## Final closure

The integration's own build-output scope was cleaned after all TRX and EF evidence had been saved: about 6.4 GiB recovered, about 24 GiB free afterwards. User builds, source worktrees, Wiki caches and evidence were preserved. Build outputs are reproducible; saved results were not deleted.

The sole known-baseline exception is the measured Wiki holdout failure, using the repository's `passed-with-known-baseline-failures` evidence status, never a reusable executed-pass cache or an N/A claim. Native governed validation, critique, corrected standalone context assessment and normal hook results are retained with the integration evidence. Local integration does not imply production deployment readiness or a clean full Wiki gate. Retrieval quality and notification-channel ambiguity remain separate follow-up work; no ranking change or new task was introduced here.
