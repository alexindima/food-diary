# Recipes extraction: Wiki findings and verification

Base: `a11d9a5d2c4dce2682abb4691b2b23fb6b38b9a6`, exact initial local master and HEAD. Worktree: `C:/Users/alexi/.codex/worktrees/1db9/FD`, initially clean. No master edit, push, production access or coverage collector. The [ownership inventory](recipes-ownership-inventory.md) was recorded before production edits.

## Physical boundary

Five production projects: Recipes Application, Application.Abstractions, Contracts, Infrastructure and PersistenceModel; one module Application.Tests project. Application uses the current `FoodDiary.Modules.Recipes.Application` project convention with legacy `FoodDiary.Application.Recipes` AssemblyName and CLR namespaces. Relocated contracts retain their CLR namespaces but move assemblies; coordinated rebuilding is required, not a binary compatibility promise for old consumers.

No independent Domain project is possible without breaking public central navigations. Recipe, RecipeStep, RecipeIngredient, IDs, value objects and nutrition events remain central alongside User.Recipes, Recipe.MealItems, MealItem.Recipe/ApplyRecipeSnapshot(Recipe), Product.RecipeIngredients and the RecipeCommunity RecipeComment.Recipe dependency. RecipeCommunity is unchanged as an owner. Shared DbContext/DbSets, historical migrations/snapshot, user cleanup and mixed projections remain central. This is not full domain or database isolation.

The existing shared Product/Recipe advisory lock stays central; explicit friend access grants only the new Infrastructure assembly access to the same internal helper. Repository methods, SQL, cancellation, mutation transaction boundaries, media lifecycle/outbox calls, rounding, servings and access filters are retained. Central Infrastructure does not reference Recipes Infrastructure. API/Initializer use complete module registration; JobManager registers persistence only, preserving its existing application handler set.

## Wiki usefulness and limitations

- True positives: Recipes generated page locates the old application project, aggregate repository ports, three entities/configurations and actual HTTP routes. Research/trace identifies the mutation transaction runner, nutrition, Images cleanup/access and Products lookup collaborators. Test-plan recommends full Architecture and central PostgreSQL integration, plus application tests; these are planned checks, not execution evidence.
- False negative: generated source areas omit `FoodDiary.Infrastructure/Services/RecipeAccessService.cs`, `RecipeLookupService.cs`, the shared `RecipeCompositionTransactionLock` and central User/MealItem/Product inverse navigations. Direct source and project graph inspection supplied them before choosing the central Domain seam.
- Misleading classification: `Origin: extracted-project` describes the prior application assembly only. It does not prove layer extraction. Public-surface output says zero DTO/read-model/projection types while listing four record models. Neither an empty consumer graph nor these counters establish isolation.
- Trace false positive: UpdateRecipeCommand also returns RecipeComments/Requests/UpdateRecipeCommentHttpRequest as a heuristic presentation match. The Recipes controller/mappings are the verified transport owner; RecipeCommunity does not move.
- `start` used a real 12-element PlannedPath array, captured zero pre-existing changes, and created a governed architectural task with 72 grounded paths and seven phases. Its generated acceptance checklist contradicts itself: AC-001 retains application source at FoodDiary.Application.Recipes while AC-005 requires that folder to contain no sources. The intended replacement is Modules/Recipes/Application, justified by current user authorization and ADR 0016, not by generated text.
- The first `decision` invocation supplied PlannedPath but considered the current document delta only, classifying docs/ai/recipes-ownership-inventory.md as Ai. Repeating with explicit ChangedPath selected the intended Recipes source scope. Facade arguments are not uniformly consumed by all diagnostics.
- Cold checkout prerequisites: `start`/research support JSON fallback without TypeScript. `ownership` subsequently failed inside Get-LlmWikiDiffContext because its nested call requires the missing SQLite compiled-index projection even with JSON selected outside. Both original and explicit-scope attempts failed; neither is a passed check. Repository-pinned npm ci and graph-build supply the normal prerequisites. No tool/policy/ranking fix is included.
- The first design command incorrectly omitted the required Intent; it failed and was rerun with the source-backed compatibility decision. This is an invocation error, not a Wiki defect.
- Topology with Query=Recipes abstained against a nonempty global index, appropriately avoiding a claim of no runtime dependencies. Source inspection shows no Recipes-owned provider or scheduled job; Images and JobManager remain separate owners.

No module-specific generator cases, holdout query changes, ranking or threshold adjustments are permitted. The measured base holdout failure is documented in `module-integration-exercises-mealplanning-recipecommunity.md`; current verification must report its own exit code and measured outcomes before any known-baseline exception is used.

Eight eval source-path references and one development-bundle planned directory follow exact source relocations. `eval-path-relocations.json` records each old/new pair and verifies destination existence. Queries, case identities, rankings and thresholds are untouched; this is path maintenance, not retrieval tuning. Graph-build rejected an attempt while implementation edits were still in progress, as designed; it must be rerun on stable sources.

## Test ownership and regression intent

Recipes validator, nutrition and Update-handler suites move without duplication. The mixed RecipesFeatureTests partial suite stays central because its overview scenarios instantiate the real Favorites read service; the module test project has no Favorites project reference. Test-local support helpers are copied as needed; there is no reference to another test assembly. Domain tests remain central because the aggregate remains central. Existing RecipeRepositoryIntegrationTests includes Product lock coordination and Favorites relationships, so it remains with the shared PostgreSQL fixture. HTTP/Presentation, DI and mixed consumer tests retain their current owners.

New real-PostgreSQL regression checks nested manual-nutrition per-serving calculations, denied foreign read/delete/duplicate, HTTP used-recipe mutation rejection with unchanged persisted state, plus indirect cycle rejection through real PostgreSQL lookup adapters, independent duplicate step identities, usage-based deletion rejection and deletion after both parents are removed. A separate mixed persistence test checks Meal snapshot stability after Recipe nutrition changes and materializes the retained User/Recipe/MealItem inverse navigations.

## Verification evidence

Evidence/results: `.artifacts/recipes-extraction-results/`. The single reusable .NET output scope is `.artifacts/recipes-extraction/`.

Force-evaluate full solution restore completed with exit 0. Initial full build exposed missing RootNamespace on the new Infrastructure/Model projects; corrected to match preserved namespaces, without analyzer suppression. Failed logs are retained separately from successful final runs. Final checks below distinguish successful runs from retained failures. Governance and commit status are recorded separately; a filtered recovery never changes the original full-run result.

Pinned npm ci reported three high-severity frontend dependency vulnerabilities. No package upgrade or npm audit fix is part of this backend extraction; the required NuGet audit is independent. The parent-task messaging tool is absent from this task's ALL_TOOLS and `tools.mcp__codex_app__send_message_to_thread` is undefined; no notification is claimed without an actual successful call.

## Compatibility and delivery review

ADR 0016 already permits logical modules with documented central compatibility seams; this move applies that decision rather than creating a new domain boundary. ProjectDependencyMatrix and explicit physical-ownership guards cover the new graph. The central Abstractions project remains a compatibility reference bridge to Recipes ports/contracts. No new package family or resolved version is introduced; the force-evaluated lockfile comparison is empty for resolved-version changes.

This is a coordinated build/deployment of existing hosts: no independently rolling old binaries against the relocated contract assemblies. API and Initializer compose the complete module; JobManager composes persistence only. Docker restore/source COPY entries include all five production projects. No configuration, secrets, scheduled job, provider, retry, cancellation, route, request/response snapshot or migration changes are required. Roll back by deploying the previous complete host artifacts; there is no data migration to reverse. Deployment itself is not performed here. After a future deployment, smoke checks should exercise private recipe read/mutation, nested nutrition, favorites/explore, community access and meal snapshot reads using the existing logs and health endpoints.

Privacy and source review preserve user predicates, visibility, private notes, media authorization and the Images deletion outbox. No health data is copied into new logging, caching or provider paths. Query review preserves SQL shape, tracking, paging and parameterized filters; the existing shared Product/Recipe advisory lock and transaction save boundary are unchanged. The explicit PostgreSQL tests verify rollback/access/deletion and cross-module snapshots, rather than treating textual references as executed coverage. No separate Security scan was run.

The affected Wiki verify also exposed stale Recipe scopes in compiled-index SQL parity, SQL shadow, code-graph and extraction-readiness regression fixtures. Their scope/project/expected-path values now follow the exact Application and Infrastructure relocations. Query text, assertions, ranking algorithms, expected ranks and thresholds are unchanged. Older benchmark/help examples still mention the pre-base FoodDiary.Application/Recipes root; those are documented legacy navigation limits, not a reason to refactor retrieval tooling during this extraction.


## Actual .NET verification

Final ownership full solution force-evaluate restore and build returned exit 0 (build: 0 warnings, 0 errors). EF has-pending-model-changes returned exit 0: no model change since the last migration. The installed EF tools report 10.0.10 versus runtime 10.0.11; no upgrade was made. NuGet vulnerability audit returned exit 0. The lockfile comparison found zero resolved-version changes.

| Suite | Passed / total | Failed |
| --- | ---: | ---: |
| Recipes.Application.final | 63 / 63 | 0 |
| Application.final | 1129 / 1129 | 0 |
| Domain | 910 / 910 | 0 |
| Favorites.Application | 78 / 78 | 0 |
| RecipeCommunity.Application | 33 / 33 | 0 |
| ContentReports.Application | 12 / 12 | 0 |
| Infrastructure.Unit.final | 764 / 764 | 0 |
| Presentation | 825 / 825 | 0 |
| WebApi.Unit | 247 / 247 | 0 |
| JobManager | 168 / 168 | 0 |
| WebApi.Integration.final | 176 / 176 | 0 |
| Architecture.final-ownership | 792 / 792 | 0 |
| Infrastructure.Integration.full | 116 / 117 | 1 |
| Recipes.Postgres.HTTP.verified | 2 / 2 | 0 |

All listed runs have zero skipped tests. Counts come from VSTest TRX, not file references. Module 63 plus central Application 1129 equals the original combined 1192: the 77 mixed RecipesFeatureTests cases stay central, without duplication. The HTTP suite explicitly excludes PostgresPerformanceBaselineTests; it is not an unfiltered full HTTP claim. The central Infrastructure integration suite ran exactly once, unfiltered: 116/117 passed, one unchanged first-page latency check measured 280.9 ms against 250 ms (25m33s total). The new Meal snapshot regression passed. That full-run exit 1 is retained and belongs to both equivalent required integration check IDs. It is not the known Wiki baseline exception.

Initial build/analyzer, architecture-path, DI-fixture and new HTTP-test expectation failures remain in separate logs. Final suites supersede those implementation diagnostics. The final HTTP test preserves the existing used-recipe mutation guard, checks cycle validation through real PostgreSQL lookup adapters, and reads nested nutrition through the GET projection rather than assuming POST eagerly materializes totals.

## Delivery blockers and Wiki gate results

Delivery is **blocked**, not an all-green completion. The full PostgreSQL run returned exit 1 for the unchanged Recipes 250 ms budget. A separate filtered recheck passed 2/2 (Recipes and Products first-page tests), but it is supplemental recovery evidence and does not turn the single unfiltered 116/117 run into a pass. No second full integration run is issued under the requested exactly-once limit.

The unchanged holdout-100 corpus was measured with FailOnRegression and actually exited 1: top1=94/100, top10=98/100, MRR=.953, acceptedPrecision=.9615, errorCapture=.5. Misses are holdout100-domain-006 (DailyAdvice, rank 11) and holdout100-domain-010 (Notification, rank 18), matching the committed base integration report's measured failures. Thresholds remain top10>=100 and errorCapture>=.9. This alone would qualify for the native known-baseline status, but the additional Wiki failure below prevents claiming that it is the only failing Wiki gate.

After source-proven fixture path maintenance, the actual bounded/resumable `wiki verify` still returned exit 1: extraction-readiness cannot find Dietologist. `Get-LlmWikiExtractionReadiness.ps1:13-17` accepts only FoodDiary.Application/<Module> or FoodDiary.Application.<Module>; it does not discover Modules/<Module>/Application. The implementation is byte-identical to base. Base Git tree already contains Modules/Dietologist and neither legacy donor path. This is a separate general discovery defect exposed by the newly active regression group, not a missing Recipes source file. The user requires parent approval before a generic Wiki defect fix; the parent messenger is not callable here, so no implementation workaround, dummy legacy directory or skipped regression is introduced. The current Wiki check remains failed rather than passed-with-known-baseline-failures.

Interrupted smoke groups and trailing verification stages are recorded separately where executed. Their successes cannot overwrite either failed full-run result. Remaining approval/work is a bounded generic logical-module discovery fix and an explicit decision about further full persistence verification under the exactly-once constraint. No schema, production query, timing budget, retrieval ranking or threshold is changed to force acceptance.

The separately executed context-bundle group passed SQL parity 9/9 and SQL shadow, then failed on the same holdout gates. Context-cache and workflow-recovery regressions passed. The code-graph regression itself passed (7121 files, 31542 symbols), but its group failed the unchanged broad backend trace regression. The exact query `Recipes handlers storage consumers boundaries` returned zero symbols/consumers and ranked FavoriteRecipes repository contracts plus the frontend FavoriteRecipeService under the Recipes boundary; moved Recipes application slices were absent from that bounded result. This is a concrete post-move false-positive/false-negative set, not evidence of isolation. The trace fixture, query and ranking are left unchanged; generic routing/discovery work needs parent review. Remaining smoke work after that failure is not claimed as passed.

Final whitespace verification returned exit 0 after normalizing changed C# line endings with the штатный formatter; workspace-load warnings are retained in the log. The final complete solution build returned exit 0 with zero compiler warnings/errors. The final architecture run passed 792/792. All .NET outputs use only `.artifacts/recipes-extraction`; TRX/logs, actual command metadata, native governed evidence, context assessment and handoff/cleanup receipts are retained separately under `.artifacts/recipes-extraction-results` and `.artifacts/llm-wiki/tasks/current`. Required evidence uses canonical Definition plus actual Command from the native lineage builder. Supplemental test results are not invented active policy check IDs.
