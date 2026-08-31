# Ai extraction: Wiki observations and evidence

Base: `a11d9a5d2c4dce2682abb4691b2b23fb6b38b9a6`, exact initial local master and HEAD. Worktree: `C:/Users/alexi/.codex/worktrees/901d/FD`. No pre-existing changes. No push, production access, external provider calls or coverage collectors. Ownership and compatibility decisions are in [the inventory](ai-ownership-inventory.md).

## Boundary

Five production projects group Application, Application/Abstractions, Domain, Infrastructure/Model and Infrastructure under Modules/Ai. The application MSBuild project uses the accepted FoodDiary.Modules.Ai.Application name and preserves FoodDiary.Application.Ai assembly/CLR identity. No empty Contracts project is introduced. Existing semantic administration interfaces remain in Application/Common, including the existing aggregate return type; this is preserved compatibility, not a new public API.

AiUsage/AiPromptTemplate and IDs leave central Domain one-way. Quota ledger records remain internal persistence types in the model assembly; explicit friend assemblies preserve visibility for shared DbSets and adapters. Central User profile limits/consent/value objects, shared context/DbSets/migrations/snapshot, shared infrastructure meter and cleanup remain documented seams. MealAiSession/MealAiItem stay Meals-owned. OpenAI transport/options stay Integrations-owned without changes to request construction, models, prompts, pricing, retries or timeouts.

Three focused module unit-test projects receive existing Ai application, aggregate and mocked provider/persistence tests. Mixed Admin, Users consent, Meals, central DI, PostgreSQL and HTTP suites stay central. Module tests retain legacy namespaces and a local existing ResultAssert helper.

## Wiki usefulness and limitations

- True positives: the Ai scoped guide explicitly owns quota policy, prompt administration, usage projections and telemetry; Infrastructure instructions identify independent short quota transactions. Research on exact files found downstream runtime evidence and exposed the need to state provider/persistence/privacy compatibility.
- False negative: initial backend-modules.json described Ai as an orchestrator with no owned entities, despite two domain aggregates and a persisted quota ledger. It also did not describe the prompt provider, internal quota records or integration/HTTP boundaries. Updated source mappings reflect actual projects; no generator special case was added.
- Missing-path correction: the delegated candidate `FoodDiary.Infrastructure/Configurations/Ai` is actually `FoodDiary.Infrastructure/Persistence/Configurations/Ai`; the provider implementation is `FoodDiary.Integrations/Services/OpenAi`, not `Integrations/OpenAI`. Source search, not generated extracted-project labels, established the physical inventory.
- Scope limitation: start received a real array of planned directories and captured a clean baseline, but initialized 23 grounded paths with zero evidence checks/reviews. Subsequent brief/test-plan/decision did not turn directory hints into adequate active policy scope; decision reported only the inventory document then in the diff. These preliminary outputs are not sufficient validation. Research was repeated with exact production file paths; final policy must use the actual diff.
- Prerequisites: fresh worktree lacked TypeScript dependencies. Start used its supported read-only JSON fallback. npm ci installed locked frontend tooling. Ownership still failed with `compiled-index-projection-missing` and requested graph-build; that failed command is retained, not labelled passed. Full index update follows.
- Research's generic resolve-design-boundary question was answerable from the user's explicit instructions and current code. The recorded design decisions preserve quota/consent/provider/schema semantics. Physical moves began after source inventory while the asynchronous research result was finishing; the formal design checkpoint was recorded afterwards. This sequencing limitation is explicit.
- No ranking, query-text, cohort or threshold changes are part of this extraction. The supplied baseline holdout failure remains a separate measured requirement, not an automatic waiver of current gates.
- Twelve corpus target paths followed physically relocated, byte-equivalent C# sources, including four targets in holdout-100. `.artifacts/ai-evidence/corpus-path-audit.json` records every old/new path and source equivalence. Case IDs, query text, cohort identity, frozen measurements and thresholds are unchanged; leaving deleted targets would measure path staleness rather than retrieval quality.

## Evidence location and interim verification

All logs and comparison artifacts are under `.artifacts/ai-evidence/`, separate from reusable `.artifacts/ai-extraction/` build outputs. Initial restore failed after an unnecessary direct MemoryCache package reference lacked a central version. Removing that reference retains the same existing transitive library. The second force-evaluate solution restore exited zero. Lock comparison reports zero resolved NuGet package version changes.

Source comparison matched 77 relocated C# files: 75 unchanged, with intentional application DI and friend-assembly changes. Runtime provider/quota/prompt/entity implementations are unchanged. The first full build exposed missing test global usings and persistence-model path/namespace mismatches; those were extraction defects, not baseline exceptions. `build-verified.log` records the successful full solution build with zero warnings/errors. Six initial Architecture failures were obsolete extraction expectations/Docker COPY guards and were corrected; both initial and passing TRX are retained.

VSTest results (zero skipped throughout):

| Suite | Passed / total | Scope |
| --- | --- | --- |
| Ai Application | 59 / 59 | Entire new project |
| Ai Domain | 23 / 23 | Entire new project |
| Ai Infrastructure | 87 / 87 | Entire new project, mocked provider |
| Architecture | 788 / 788 | Entire project |
| Central Application | 1133 / 1133 | Entire project; mixed Admin/Meals/consent tests remain here |
| Central Domain | 877 / 877 | Entire project |
| Images Application | 40 / 40 | Entire project |
| Presentation | 825 / 825 | Entire project |
| WebApi unit | 247 / 247 | Entire project |
| JobManager | 168 / 168 | Entire project |
| HTTP | 175 / 175 | Explicit filter excludes PostgresPerformanceBaselineTests; not a full HTTP project claim |
| Central Infrastructure initial | 676 / 677 | Entire project; one PDF caller-cancellation failure |
| Central Infrastructure retry | 676 / 677 | Entire unchanged project; one PDF report-deadline timeout |
| PostgreSQL Infrastructure integration | 116 / 116 | One unfiltered Testcontainers run, 31m36s VSTest duration; shared evidence for both integration policy IDs |

The initial Infrastructure failure is `DiaryPdfGeneratorTests.LoadMealImageAsync_WhenCallerCancels_PropagatesCancellation`; no exception was thrown. An unchanged-code full unit retry also reported `LoadMealImagesAsync_WhenReportDeadlineExpires_ReturnsWithoutImages` timing out. These are unresolved verification gaps, not proven base failures. No PDF/Export source was modified and no repeated run is substituted for the retained failing evidence.

## New retrieval blocker

The current 100-case holdout actually returned exit 1: top1 **92/100**, top10 **96/100**, MRR **.9344**, accepted precision **.9481**, error capture **.5**. The supplied exact-base measurement was 94/98 with precision .9615 and error capture .5. Current raw results are `.artifacts/ai-evidence/holdout-current.json`.

Two additional top-10 misses follow this extraction: `holdout100-mixed-005` targets the unchanged AiQuotaReservation at rank 21 (top candidate is its EF configuration), and `holdout100-domain-003` targets the unchanged AiPromptTemplate at rank 12 (top candidate is central EmailTemplate). Existing DailyAdvice/Notification misses remain ranks 11/18. Paths exist and map to the same sources; this is not a stale/deleted-target explanation. Accepted precision now also fails its unchanged .95 gate.

This is **not** the unchanged known baseline, and must not be labelled `passed-with-known-baseline-failures`. No ranking/query/threshold adjustment or module-specific generator rule was made. The user's instruction reserves additional generic Wiki fixes for agreement with the parent. That agreement cannot be obtained automatically here because `send_message_to_thread` is absent from the available tools. Product verification continues, but governed completion/integration remains blocked by this regression.

## Review resolutions

- Authorization/privacy: source-equivalent OpenAiFoodService retains consent before provider calls, owned-image checks, image count/size/type constraints and quota enforcement. Admin handlers keep their existing authorization surface. No prompt, food content, image, email or provider payload is added to telemetry. Existing central user deletion and consent lifecycle remain intact. This is a scoped code review, not a separate Security scan.
- Transactions/resilience: repository source comparison preserves PostgreSQL period row locking, short independent reserve transactions and atomic reconciliation of quota ledger plus usage. Explicit release, duplicate requests, expired/orphaned reservations, late reconciliation and independent persistence cancellation remain unchanged. No retry, timeout, price, model or prompt adjustment is introduced.
- Query/model: existing predicates, aggregation, tracking, pagination, indexes, keys, converters and delete behavior are unchanged. Shared EF model explicitly applies the four relocated configurations; EF reports no pending model changes. No new migration or schema rollout is required.
- Observability/cache: existing quota/usage metrics, low-cardinality tags and shared InfrastructureTelemetry identity are retained. Prompt cache remains singleton with five-minute consistency; no new invalidation promise is made.
- Dependencies/ADR: the existing logical-module ADR pattern applies. Domain references central Domain solely for retained primitives/UserId; central Domain does not reference Ai. Model owns internal quota records and configurations; central Infrastructure references Model, while Ai Infrastructure references the central context. Friend assemblies are narrow compatibility seams. Admin uses the existing semantic interfaces, not repositories. No empty Contracts assembly or widened aggregate API is added.
- Consumers/rollout: hosts reference the new module project paths; all three Dockerfiles copy each new project and its source. API/Initializer compose application plus persistence; JobManager retains persistence-only registration. HTTP routes, snapshots and payloads are unchanged. Deploy the complete rebuilt binaries together; rollback uses the prior application binaries against the unchanged schema. No deployment was performed.
- Dependency versions: force-evaluate restore succeeded; resolved-version comparison is empty and NuGet audit found no vulnerable dependencies. Module assembly relocation requires consumers to rebuild; this report does not promise binary compatibility with old independently deployed assemblies.

The actual-diff Wiki replan corrected initial zero-check scope to six active checks and thirteen review obligations. Privacy returned adjacent User profile/billing candidates outside the changed semantic responsibility; topology included MailRelay/MailInbox clients. These are broad navigation candidates, not evidence that Ai owns those areas. No neighboring ownership refactor followed them.

Context-security create/assess reported 408 sources, zero findings/quarantine/truncation and valid assessment. Deleted donor paths remain explicitly `exists=false`; they must not be claimed as read current files. This standalone assessment does not prove that a bounded scheduler context bundle included all 408 sources, nor does it remove the measured retrieval misses.

## Full Wiki verification limitation

The final full update succeeded in 215.52 seconds. Architecture health reports 584 production and 305 test edges with no enforced drift. Full `wiki.ps1 verify` returned exit 1: workspace policy, page contracts, lint regression and index freshness passed, but affected smoke exceeded its unchanged 600-second outer timeout. The nested context-bundle group had not completed successfully after 416.3 seconds. Its log last reported 9/9 scoped SQLite retrieval cases and a passing SQL shadow comparison; those partial successes do not establish context-bundle completion.

Logs are in `.artifacts/ai-evidence/wiki-verify.log` and `.artifacts/llm-wiki/parallel-smoke/39740-4568181714ff46748df1436b2ed5b881/`. No timeout or ranking threshold was changed. This timeout is not claimed to be a reproduced base failure. The full invocation remains failed; standalone checks for later stages cannot turn it into a pass. Final replan marks the active Wiki check pending because two subsequently reviewed Wiki pages changed; there is no successful full verification of that final documentation state.

Standalone failure-knowledge and change-policy stages passed. Source-impact initially required review; seventeen affected manual pages were read and reviewed with specific native receipts. Primary-backend guidance now describes module Domain/ports and Ai seams; code-graph guidance labels the earlier 99/100 measurement historical instead of presenting it as current. The final source-impact check passed with 58 affected pages reviewed. No generator special case was introduced.

All .NET consumers completed before `dotnet clean FoodDiary.slnx --artifacts-path C:/Users/alexi/.codex/worktrees/901d/FD/.artifacts/ai-extraction`. Clean exited zero in 26.22 seconds, with zero warnings/errors. Logs/TRX/evidence remain under `.artifacts/ai-evidence`; Wiki caches and other worktrees were not cleaned. Free C space rose to approximately 21 GiB before commit preparation. The output directory itself is intentionally retained for reuse.

## Governed evidence and delivery

Final replan synchronized 352 planned paths and seven phases. It also normalized four evidence `command` fields back to policy definitions while preserving original executed-command lineage, causing eight command/fingerprint consistency errors. `evidence-after-replan-before-command-repair.json` and `lineage-assessment.json` preserve that state. The fields were restored from the existing native-builder receipts; no timestamps, source fingerprints, definitions, execution outcomes or receipt hashes were fabricated or replaced. No Wiki implementation fix was made. A future generic replan correction needs independent regression evidence and parent agreement.

`lineage-final.json` confirms valid lineage with zero issues after restoring the original command fields. Governed validate/critique both returned exit 1; critique returned `reject`, score 24/100, with unresolved verification. The first validate also captured the now-corrected command mismatches and an unmapped snapshot criterion; final validation is retained separately as `delivery-validate-final.log`. Acceptance AC-001 remains rejected: new holdout misses, unresolved PDF timing failures and incomplete final Wiki verification prevent completion/integration. Other mapped architecture/model criteria use actual passing evidence; no migration/snapshot change is applicable. This branch is a reviewable extraction with blockers, not a completed or all-green delivery.
