# RecipeCommunity extraction: Wiki findings and verification

Base: `3851a98e0340fd12191c56cf41f261a678a632c8` (exact local master supplied by the parent). Worktree: `C:/Users/alexi/.codex/worktrees/b134/FD`; branch: `codex/recipe-community-module`. No pre-existing changes, master changes, or push. See [ownership inventory](recipecommunity-ownership-inventory.md).

## Implemented boundary

Five production projects under Modules/RecipeCommunity: legacy-named Application, Application.Abstractions, Domain, Infrastructure and PersistenceModel. Two new module test projects own the existing application suites and comment domain invariants. Mixed SocialInvariantTests and relational/HTTP/host tests remain central, with their references updated. RecipeComments and RecipeLikes remain separate feature groups.

RecipeComment and RecipeLike plus IDs safely leave central Domain: User and Recipe have no inverse CLR navigation; configuration uses WithMany() without an inverse. Domain depends one-way on central Domain. User, Recipe, their IDs, shared DbContext, migrations/model snapshot, HTTP transport, user cleanup and ContentReports reportability remain central/current-owner seams. ContentReports still reads comment.Recipe visibility and ownership. No Recipes extraction, schema redesign, shared outbox change or provider behavior change.

Application AssemblyName and all existing CLR namespaces are preserved. Central Errors.RecipeComment is a compatibility facade referencing the module-owned errors. No separate Contracts project is justified: existing comment/like read services are implementation-local query collaborators; transport consumes application models. Hosts use AddRecipeCommunityModule, which composes AddRecipeCommunityApplication and scoped repository aliases. The shared context explicitly applies the module model.

## Wiki observations

- True positives: the generated RecipeCommunity page identifies Notifications/Recipes/Users abstraction dependencies and API/Initializer/JobManager/Presentation consumers; research surfaced the existing create/comment flow and recent extraction precedents.
- False negatives on the exact base: generated page declares no owned entities, no HTTP routes, and only RecipeCommunityModuleExtractionTests. Actual sources include two entities/IDs, two repositories, three application test files, RecipeCommentsControllerTests, RecipeCommentHttpMappingsTests, RecipeLikeHttpMappingsTests, ContentReportRecipeLikeControllerTests, and relational RecipeSocialRepositories_AddQueryUpdateAndDeleteLikesAndComments. Manifest domainAreas=RecipeSocial did not describe actual Recipes/Social folders.
- Durable correction: module ownership manifest now names the logical root, both entities and real layer paths; scoped guides and module map state compatibility seams. No module-specific Wiki generator exception was added.
- Prerequisites: first start used the supported read-only JSON fallback because a fresh worktree had no TypeScript install. It created a governed workspace with 50 grounded paths, 7 phases and 12 criteria (not an empty task). Initial read commands using Module alone yielded zero scope; they were repeated with explicit Intent/PlannedPath arrays.
- Reproducible limitation: ownership invoked after explicit JSON selection required a missing SQLite compiled-index projection; prerequisite message requested graph-build. This was treated as a tooling failure, not a passing gate. npm ci installed the repository-pinned prerequisites and Husky launcher; index/graph rebuild followed.
- Research's generic resolve-design-boundary question was answerable from code: the design checkpoint records preserved schema/access/notification semantics and one-way navigation extraction. No extra user approval was needed for the already authorized boundary.

## Review conclusions

Moved handlers/repositories retain their behavior. Current-user validation, recipe access, author/owner deletion, normalized pagination, tracking and CancellationToken propagation are unchanged. Sequential desired-state like retries remain idempotent; PostgreSQL unique index still protects concurrent inserts (existing conflict behavior, not a new upsert guarantee). A like intentionally has no recipe FK in the existing schema. Comment notifications still use INotificationWriter for a different recipe owner; Notifications delivery and shared outbox are unchanged.

Added PostgreSQL regression verifies duplicate like rejection, navigation materialization, recipe-to-comment cascade and user-to-like cascade. Historical migrations and snapshot are untouched; EF comparison must independently confirm no schema drift. No unrelated product bugs were changed.

## Verification

- Force-evaluate solution restore passed in the single `.artifacts/recipecommunity-extraction` scope.
- Full solution build: 0 warnings, 0 errors (`build.log`). Initial path/analyzer/test-helper failures were fixed; they are not baseline failures.
- RecipeCommunity Application: 33/33; Domain: 9/9.
- Full ArchitectureTests: 759/759, including matrix, ownership, Docker, model and source-placement guards.
- Central Application: 1329/1329; Domain: 1027/1027; Presentation: 825/825; Infrastructure: 764/764 (including all previously repaired Billing/OpenFoodFacts/Wearables DI cases).
- ContentReports application: 12/12; Notifications application: 111/111.
- API unit tests: 247/247; HTTP/Swagger contract filter: 55/55. Snapshots untouched. Initial batch: 5175 passed across 12 VSTest runs, zero failed/skipped; the additional full integration run is recorded below.
- PostgreSQL relational filter: 4/4, no skips. Exact filter: `FullyQualifiedName~RecipeSocial|FullyQualifiedName~ContentReportRepository_CoversStatusFiltersAndTrackingUpdatePaths|FullyQualifiedName~RecipeRepository_CoversSearchIncludesUsageNutritionExploreAndDeleteBranches`.
- EF pending model check: no changes since last migration. Tool/runtime version warning 10.0.10/10.0.11 retained; not a model difference. ArtifactsPath environment was set to the restored scope, rather than using an empty default output.
- NuGet vulnerability audit: exit 0; no vulnerable packages reported with the restored ArtifactsPath.
- Scoped whitespace formatter: exit 0, workspace-load warning. `git diff --check`: exit 0.
- All tests ran through VSTest without coverage collectors. Exact TRX files and a parsed summary live in `.artifacts/recipecommunity-extraction/results` and `test-summary.json`; historical failed architecture TRX is named `Architecture.initial.trx` and is not final evidence.

Source comparison confirmed 37 relocated production files unchanged modulo whitespace. After preserving TRX/logs, dotnet clean on the explicitly resolved own artifacts scope reclaimed 5.98 GiB. Commit hooks follow the parent-coordinated Exercises/RecipeCommunity/MealPlanning queue. No claim of a green gate is inferred from a command with missing prerequisites.

## Source impact and rollout review

This applies the accepted incremental extraction architecture (ADR 0016); it does not introduce a new data-consistency or deployment policy. All new project edges are explicit and acyclic. Package lock changes are the transitive project graph, not version upgrades. Docker copies include each actual project directory before restore and publish. One deployment unit, database and migration host remain; rollback is the prior application image, with no data rollback/migration required. No configuration, credential, endpoint, provider, retry or queue-policy change is introduced.

The generated HTTP section still has a known generic discovery limitation: it matches the canonical module name against controller name/feature folder and does not match RecipeComments/RecipeLikes to RecipeCommunity. Explicit adapterAreas preserve source navigation, but an empty generated HTTP section must not be used as evidence that there is no HTTP surface. Parent-owned Windows argv batching work is intentionally not duplicated here.

## Governance audit

Wiki ownership, test-plan, topology, privacy, dependencies, rollout and trace were run against current sources. A temporary read-only SQLite snapshot cleanup lock affected the first dependencies diagnostic; the sequential retry completed. Architecture-health passed: 540 production project edges, 284 test project edges, no enforced drift. Wiki verify passed all seven affected/resumable stages: 259 changed paths, zero policy violations, 56 affected pages reviewed. This is the normal affected gate, not an exhaustive full-tools audit.

Delivery replan refreshed the nonempty original task to 259 observed paths. Acceptance has 10 satisfied criteria and two explicitly inapplicable migration criteria. The first delivery-validate passed packet freshness, requirements, acceptance, plan conformance and proof-of-change but failed evidence lineage. The first delivery-critique returned approve-with-notes, 85/100, for missing context trust assessment; the subsequent context-security creation was valid with zero findings/quarantined sources.

Confirmed generic receipt limitation: Manage-LlmWikiEvidence.ps1 supplies the actual execution command as both Command and Definition. Test-LlmWikiEvidenceLineage compares Definition with the canonical policy requirement, so harmless output/build flags produce a mismatch. The existing New-LlmWikiEvidenceLineage.ps1 builder already separates these fields. Its canonical default Definition was retained while preserving actual execution Command; no signatures were manually constructed, no policy/validator was changed. Full architecture (759) and full solution NuGet audit are scope-equivalent despite output/no-build/no-restore flags. In contrast, the four filtered PostgreSQL tests do not satisfy either canonical full integration requirement; both checks were reset to pending until an unfiltered run could complete.

The entire prior bundle, including the failed/pending history and supplemental results, is retained in `.artifacts/recipecommunity-extraction/evidence-before-lineage-repair.json`. Module tests and EF receipts are also retained in `supplemental-evidence.json` because the governed validator accepts only policy-active check IDs. Both are linked here rather than discarded or used to imply a full-suite pass. `wiki-lineage.json` preserves the original 14 issues; `wiki-lineage-repaired.json` shows none after separating the two valid full-scope receipts and leaving integration pending. Parent was notified of this generic defect; no shared Wiki helper fix was duplicated.
The additional unfiltered Infrastructure.IntegrationTests run passed 116/116, zero failed/skipped, in 9m21s after a fresh project restore/build (0 warnings/errors) in the same artifacts scope. This supplies both `data-access-integration-tests` and `infrastructure-integration-tests`, without counting them as two separate executions. Final runtime evidence totals 5291 passed executions across 13 TRX files (the earlier four focused relational cases are also part of the full suite). `test-summary.json` records exact per-run counts. The pending intermediate bundle is preserved as `evidence-integration-pending.json`.

Canonical-to-actual check correspondence:

| Check | Canonical Definition | Actual Command and scope evidence |
| --- | --- | --- |
| architecture-tests | `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj` | Same full project with `--no-build --no-restore --artifacts-path .artifacts/recipecommunity-extraction`; 759 passed against the verified current solution build. |
| nuget-vulnerability-audit | `dotnet list FoodDiary.slnx package --vulnerable --include-transitive` | Same solution/package scope with `--no-restore` and ArtifactsPath set to the restored own scope; exit 0, no vulnerable packages. |
| data-access-integration-tests | `dotnet test tests/FoodDiary.Infrastructure.IntegrationTests/FoodDiary.Infrastructure.IntegrationTests.csproj` | Same full project with `--artifacts-path .artifacts/recipecommunity-extraction --no-build --no-restore --logger "trx;LogFileName=Infrastructure.Full.trx" --results-directory .artifacts/recipecommunity-extraction/results`; no filter, 116 passed after current restore/build. |
| infrastructure-integration-tests | Same full integration project command | Same single unfiltered 116-test run as the preceding requirement. |

The own scope was cleaned again with dotnet clean after the full integration run and preservation of TRX/logs; no foreign outputs, caches, processes or worktrees were modified.

Final delivery-validate passed: zero unresolved checks/reviews, zero lineage issues, all six delivery gates passed. The staged Wiki verify also passed 7/7 in 82.99s (215 normalized changed paths after rename detection, 56 affected pages reviewed). Final critique output is retained in wiki-delivery-critique-final.log and reported with the commit handoff. Hook execution is reported with the resulting SHA; no hooks are disabled.

A critique attempt captured the context receipt before the report's final edits and rejected that stale trust assessment (security-context-invalid, 14/100). Its output is preserved in wiki-delivery-critique-context-stale.log. Context security was regenerated only after freezing the final report, then critique was rerun; no validator or trust policy was weakened.
