# Wiki tool performance and reliability audit

Status: **complete, including the compact handoff followup**. All
410 current sources were read in full. Followup proposals are separate from the fixes
that have execution evidence; this is not a claim that every proposal was implemented.

The audit now covers **410 files** (408 in its initial inventory), with **74,954 lines in its initial baseline**:
the Wiki facade, every tool/library/regression source under `.llm-wiki/tools`,
the complete Development MCP runtime, and its launcher/CI integration. Generated
navigation is not an authority. Relevant policies, contracts and tests are
inspected with their owning tool.

## Coverage and evidence

- [Inventory](wiki-tools-audit-2026-10-06/inventory.json) records source hashes,
  functions, parameters and expensive-operation anchors. Automated inventory
  does **not** mark a file reviewed.
- [Reviews](wiki-tools-audit-2026-10-06/reviews.json) records complete-source
  inspection and specific performance/reliability observations for each file.
  Current checkpoint: **410 reviewed; 0 pending**; exact source hashes pass the coverage guard.
- [Findings](wiki-tools-audit-2026-10-06/findings.json) includes concrete triggers,
  consequences, test gaps, remediation and evidence references. All thirty corrections
  have passed focused regression checks; final integrated results are recorded below.
- [Coverage check](wiki-tools-audit-2026-10-06/Test-Coverage.ps1) rejects stale
  reviewed hashes, duplicate/missing scope entries and incomplete claims when
  `-FailOnIncomplete` is supplied. It checks evidence identity, not the truth
  of a reviewer's conclusions.

The initial baseline is `bca936eeb975345daa4a455a0e5fb14b49a6581a`; the refreshed inventory records the current checkpoint HEAD separately. The ledger records
worktree content and explicitly distinguishes it from the committed baseline.
The remaining review scope has not been reduced to frequently used tools.

## First shared-runtime pass

1. Process-tree cleanup changed native exit state from failure **37 to 0** on
   both Windows shells. It now preserves failure, success and absent state in
   `finally`, while the owned-child termination test remains mandatory.
2. Identical JSON values containing an apostrophe produced different PS5/PS7
   fingerprints. The correction preserves ordinary existing PS7 hashes and
   converts only actual apostrophe escapes from the legacy serializer.
3. Source enumeration threw in PS5 because `.NET Framework` lacks
   `EnumerationOptions`. A compatible one-directory-at-a-time path now applies
   the same hidden/system/link/exclusion rules and propagates non-permission
   I/O failures. The optimized modern enumeration remains available.
4. Literal `\uABCD` and `\uabcd` strings produced the same JSON fingerprint.
   Escape normalization now distinguishes literal text from actual Unicode
   escape sequences.

The process, portable JSON and source-inventory regressions pass on installed
PowerShell **7.6.5** and **5.1.26100.9549**. An initial legacy source-inventory
test passed its assertions but failed during junction cleanup; nonrecursive
`Directory.Delete` fixed the fixture, and both full reruns passed. These are
Windows results; Linux execution is not claimed.

Counterbalanced modern source-enumeration runs returned exactly the same
**4,406 source paths**. Timings ranged from 1.09 to 2.69 seconds for the reference
and 1.12 to 1.30 seconds for the update; the first reference pass was cold. This
does **not** establish a speedup. Measurements and raw regression logs remain
under `.artifacts/wiki-tool-audit-20261006`.

## Second pass: cache, template scope and receipt integrity

- With `Retain=5`, twelve distinct writes left **5 results and 12 diagnostic
  metadata files**. Metadata is now pruned to the same Retain bound.
- A held reader lease caused retention to throw `IOException` **after the new
  result was successfully published**. Optional cache cleanup can therefore
  fail an otherwise successful query. Delete-sharing reads and deferred sharing-conflict cleanup now preserve publication and retry retention after the lease is released.

Both cases were reproduced in an owned fixture inside this worktree and the complete query-cache regressions pass on PS5/PS7. Conditional template scope moves now invalidate frontend contract navigation, and NUL-safe receipt fingerprints detect tracked/untracked Unicode edits with equal size/time. The focused index-selection and verification-receipt suites pass on PS7. Next,
inspect publication, pruning and caller error handling together, add meaningful
concurrency/retention regressions, and then continue the file-by-file review
through orchestration, generators, queries, governance, graph and MCP transport.

Full-tool audit completion remains unproven until every current source entry
has a fresh complete-source review and each reported finding is validated or
clearly classified as an unresolved suggestion/question. Focused tests do not
prove coverage of the remaining 188 files.

The first shared-runtime publication gate passed all eight stages in 136.21 seconds, including all three selected smoke groups. This proves that first change set only; later fixes receive their own gate. The full 408-source audit is still active.

## Generator and graph-helper pass

All thirteen generators have now received full-source inspection. This pass also covers the small Node graph libraries and the compiled reference, Roslyn and SQLite reader helpers. A cached architecture index describing drift now fails Check, and extracted module pages retain declared business edges. Both corrections have focused regressions. The main 2,987-line graph manager has now received full-source inspection. The complete MCP runtime, including its 1,878-line SQLite query/ranking implementation, has now received full-source inspection.

The next performance candidates to measure are pre-pruned forced C# inventory, shared generator content/newline maps, bounded per-group SQL result hydration, and eliminating diagnostics-only counts when diagnostics are disabled. They are proposals in the per-file ledger, not claimed speedups.

## MCP path, snapshot and cache pass

Seven modern-root parser cases and three rename/scope cases failed before the fix; the corrected parser/snapshot suites pass 42 checks. Both expiry/reinsertion cases reproduced incorrect live-value eviction; the cache/query-service focused suites pass 71 checks. A concurrent refresh/metrics regression is also included for the subsequent full MCP run. FIFO metadata is now bounded with the 128 active entries; no lock-throughput or latency speedup is claimed.

This pass adds 24 complete-source reviews, including the full SQLite MCP reader and query/governance adapters. Candidate followups include query-invariant hoisting, pinning ordinary SQLite reads to a transaction, readiness propagation in context-explain, command-owned source-review lookup/atomic publication, and staged/Unicode phase-status evidence. They require focused validation before being treated as defects or optimizations.

Seven Node regression sources and four graph/workflow adapters have also received complete-source inspection. The Node tests protect ordering, bounded hydration, incremental/full projection parity, WAL snapshot integrity and recovery; their names do not establish measured performance improvements. The full main graph manager is reviewed in the subsequent graph-core pass.

The subsequent complete MCP run passes **453 tests with zero failures and zero skips** on Windows, including concurrent cache refresh/read/metrics. Five additional packet/ownership/fleet/migration/policy tools bring the checkpoint to **164 of 408 files**. Their ledger entries retain concrete validation candidates; the remaining 188 sources have not been marked reviewed.

## Graph-core and navigation pass

All 2,987 baseline lines of the main graph manager were inspected, together with fifteen context/privacy/quality/design/metrics/dependency adapters. A real Node CLI fixture exposed snapshot mismatch on tracked renames; Unicode variants also revealed OEM decoding and inconsistent case conversion. Both endpoints, explicit UTF-8 and shared case-sensitive ordinal ordering now pass 31 snapshot checks. This changes opaque fingerprint values and forces older projections/cache entries to refresh; no latency improvement is claimed.

The ledger retains concrete followups for writable schema initialization on read actions, stale-lock recovery races, missing compiled-source cleanup, per-process deadlines, complete JSON freshness reads, broad recall/materialization, repeated ranking work, and caller freshness/partial evidence. Each requires targeted validation or measurement before implementation. The checkpoint is **180 of 408** sources; no pending entry was marked reviewed by inventory scans.

The complete MCP rerun now passes **458 tests without failures or skips** on Windows. Seven further readiness/report/graph-regression/adaptive/measurement tools bring full-source coverage to **187 of 408**; 221 sources remain pending. In particular, concurrency metrics currently measure PowerShell-plus-Node process paths and must not be presented as native-reader timings.

The graph publication gate passes all eight selected Wiki stages in **313.27 seconds**, including graph-core (190.77s) and trace-output (90.51s); 12 impacted pages are current and reviewed. Graph-core phase observations identify 118.57s in search/query/test-plan integration checks, versus 33.71s for build/projection/no-op checks. These are local phase timings for this test scope, not comparative speedup claims. Three further export/completion/source-trace reviews bring the checkpoint to **190/408**, with 218 pending.

## Research and workflow composition pass

Thirty more full-source reviews cover the large diff/research/test-plan/brief implementations, extraction/readiness/impact, task graph/schedule/metrics and their execution/report adapters. The current catalog has zero legacy and 34 extracted modules. A new diff-layout regression exposed Admin dependencies and module-owned tests being omitted; declared graph edges and modern Git test roots now pass the complete focused SQLite diff suite.

Per-file followups identify repeated source/receipt/packet parsing, whole test-tree reads, plan truncation and opaque readiness declarations, missing own process deadlines, and metadata publication/cleanup races. These are grounded validation/measurement candidates, not confirmed vulnerabilities or timing gains. The refreshed checkpoint is **220 of 408** files, with 188 still pending.

## Pipeline cache delivery checkpoint

A valid whole-pipeline receipt reproduced a false-green architecture Check despite a nonzero drift counter. The fast path now enforces the same three summary counters as the generator. All three drift fixtures and the clean cache-hit fixture pass on Windows PowerShell 7 and 5.1; the clean case asserts that receipt reuse still occurs. This adds one complete-source coordinator review, bringing current coverage to **221 of 408**, with **187 pending**. The broad audit remains in progress; this checkpoint is the bounded change set being delivered to local master.

## Coordinator, receipt and launcher pass

Thirty-two more complete-source reviews cover the smoke/read-only coordinators and their regressions, baseline/contract/cache/session adapters, telemetry and corpus helpers, Markdown repair/scanning, and MCP launch/CI gate scripts. Current coverage is **253 of 408**, with **155 pending**. No pending source was marked reviewed from inventory or test names.

Three reproduced reliability defects are fixed: unknown groups previously published a success receipt after zero tests; contaminated snapshot removal released its lease before deletion; and verify-fast cache reuse missed equal-size/time Unicode edits. The full affected-smoke planning, read-only guard and verification-cache suites pass on PS7. The snapshot lease also passes a focused PS5 probe.

Scoped publication no longer hashes full-gate inputs before leaving full verification unchanged. Two before samples were **5508 and 3434 ms**; five after samples were **94 ms first call and 7-9 ms warm**. These are local in-process measurements of this one no-op path, not a whole-verify benchmark or a claim about Linux. Generation, full Verification and Status keep exact fingerprints; their complete publication fixture passes on PS7 and PS5.

Remaining source-backed proposals include per-invocation composition inputs, bounded worker output/deadlines, atomic metadata publication, current test cohort classification, citation line-count reuse, and launcher lease/publication races. These remain validation or measurement candidates in the per-file ledger.

Eight further complete-source regression reviews were performed during the prior running gate and stored separately to keep its inputs stable. Their source hashes were revalidated before import. Current checkpoint: **261/408**, with **147 pending**.

## Scheduler and governed assessment pass

Twenty-seven additional full-source reviews cover scheduler mutation and lineage, evidence execution/cache/journal, policy validation, telemetry/risk/quality, and context confidence/budget/benchmark/experiment/security. The new bounded lock regression expands scope to **409 files**; current reviewed coverage is **288**, with **121 pending**. Original inventory history remains intact.

A fresh ownerless registry lock reproduced immediate mutation rejection. Seven managers now retain stable exclusive OS leases; owned fixtures prove recovery after killed holders, rejection of recent/old live holders, and stable file identity on PS7/PS5. The existing bounded catalog executes this suite for all seven managers and its receipts bind their changed sources. Linux execution remains unverified because Docker is not running locally.

A second regression reproduced cached policy acceptance after an equal-size/time referenced check-ID change, despite uncached validation rejection. Both validated JSON caching and the workspace-policy stage now bind change-policy inputs. PS7/PS5 cache tests and the stage invalidation suite pass, including absence/creation, malformed/empty definitions and restoration.

The largest remaining measured-work candidates in this pass are repeated nested bundle/budget/benchmark validation, repeated per-source context-security scans, repeated registry joins and complete telemetry hash/serialization on each append. Ledger entries separate these source-backed proposals from confirmed defects; no runtime latency gain is inferred from code inspection alone.

Acceptance and change-manifest owners were then read in full. Their exact-scope composition reuse and evidence/plan validation limits are recorded separately. Current checkpoint: **290/409**, with **119 pending**.

The first integrated scheduler gate exposed a third defect: three empty prune responses accessed BaseName on an empty collection under StrictMode. Explicit enumeration fixes the shape, and the complete seven-manager fixture now enables StrictMode and passes on PS7/PS5. The full Orchestration shard reached all lifecycle scenarios but initially failed its obsolete requirement to delete an orphan lock; that assertion now requires immediate fresh-orphan recovery with unchanged stable file identity. Final integrated reruns remain required. The 5,334-line legacy audit source is still pending full-source review; this bounded test edit does not mark it reviewed.

The Full Orchestration integration shard passes in **179.07 seconds**. Its frozen inventory review proves the same **712 total assertions** (335 Core, 376 Governed, one common; governed partitions 279 Workspace and 97 Orchestration) with exactly one assertion replaced to preserve fresh-orphan stable lock identity. The corresponding two hashes were intentionally refreshed; no assertion count changed. Nine additional query-regression sources and the full shard guard bring reviewed coverage to **300/409**, with **109 pending**. The subsequent ordinary Wiki gate passes **8/8 selected stages**, including seven smoke groups, in **226.89 seconds**. This does not establish full-audit completion.

## Context and evidence composition pass

Twenty-four further complete-source reviews cover context bundles/feedback/strategy/outcomes, memory/learning health, evidence lineage/session identity, requirement/proof/conformance, repair/prediction/cost/plan/routing, critique/retrospective/impact and task similarity. Current reviewed coverage is **324/409**, with **85 pending**. This pass changes the audit ledger only; no new runtime optimization or speedup is claimed.

Recurring candidates are immediate rebuild-after-create validation, repeated verification of the same registry/artifact inputs through nested tools, per-item array searches and deep JSON serialization. The ledger identifies exact call chains and where request-owned immutable inputs, ID maps, token sets or coherent source snapshots could help. Source-backed correctness candidates remain explicitly unverified, including transaction boundaries, stale/absent input sets, immutable execution-source identity and exact check-set validation. Observational model/instruction/outcome scores are not treated as independent quality or causal evidence.

## Regression-source coverage pass

Thirty-nine additional complete regression reads and the TypeScript extractor cover actual receipt/concurrency/cache/freshness, parser/projection, routing, scope, recovery and telemetry assertions. The checkpoint is **364/409**, with **45 pending**. Source inspection is not a claim that these suites were rerun in this documentation-only pass.

Per-file entries identify which tests execute real public commands, which use isolated controlled helpers, and which only inspect text/schema/counts. Coverage gaps remain explicit: status-only mutation checks, vacuous empty loops, child cleanup/deadlines, exact result membership and unsupported/malformed parser inputs. None of the existing assertion counts, case inventories or runtime thresholds were reduced.

## Workspace lifecycle and facade receipt pass

Seventeen additional complete-source reviews cover workspace initialization, import, status/refresh, migration, evidence invalidation/lineage, doctor validation and the remaining bounded ownership/research/scope/corpus/facade contracts. Current coverage is **381/409**, with **28 pending**. The seven earlier-read regression sources were reread and recorded against current content; no inventory entry was marked reviewed from its filename.

A real successful facade receipt reproduced a false-green cache hit after its shard guard was changed to fail. The group now declares the broad graph/runtime/CI dependencies its tests actually use. The complete stage fixture proves stable receipt reuse, execution of the changed failing guard, and edit/deletion invalidation for all nine direct regression sources. Both PS7 and PS5 pass. This correction makes reuse more conservative; it is not a claimed latency improvement.

The PS5 run also exposed ambiguous NUL splitting on .NET Framework, which retained a trailing empty status record and failed Substring. Explicit char-array/options binding fixes the runtime path. The test retains the same Cyrillic path using code points and resolves its default tools root inside the script body, avoiding separate PS5 parsing/default-parameter failures. The full focused fixture passes with `-File` on both shells; existing Unicode/rename/delete and changed-during-check assertions remain.

Source-backed measurement candidates include repeated nested status assessments, duplicate raw/parsed artifact reads during migration/refresh, per-result policy/content hashes and native runtime-version probes, and repeated rule/requirement/path-affinity searches. Exception rollback does not prove crash-atomic or concurrent mutation behavior; related fixtures and exact final-state reporting remain explicit followups in the per-file ledger. The repository-wide full audit is still incomplete.

The complete Markdown lint owner and its diagnostic regression, plus memory isolation, were subsequently read and recorded. Current checkpoint: **384/409**, with **25 pending**. Their ledger retains parser/IO/containment/deadline and mutation-fixture gaps; those are unverified followups, not newly claimed defects.

The ordinary gate subsequently passes **8/8 selected stages** in **139.60 seconds**, with both selected smoke groups executed. Three complete-source evaluation/promotion/snapshot reads were held separately during the gate and imported only after matching their current hashes. The final documentation-only update brings coverage to **387/409**, with **22 pending**; runtime sources remain exactly those verified. Benchmark comparability, corpus read coherence, event-journal atomicity and Unicode/native Git provenance remain classified followups. No full-audit completion or new speedup is claimed.

## Dispatch, delivery and learning experiment pass

Five more complete-source reviews cover dispatch, delivery workflow, learning experiments/promotion and instruction experiments. The checkpoint is **392/409**, with **17 pending**. Each ledger entry distinguishes measured defects from source-backed optimization and reliability proposals.

The actual public dispatch list reproduced two defects in an isolated repository with controlled policy/lease/context readers. A valid persisted event with a zero-fraction UTC timestamp failed its own hash after JSON date coercion; restoring the original UTC representation fixes that persistence contract. On a valid event chain, context drift then returned `driftedCount=0` and a successful `-FailOnInvalid` exit. Both drift states now contribute to the aggregate. Healthy, packet/context drift and malformed timestamp fixtures pass on PS7/PS5; existing metrics reuse and UTC-day assertions remain.

The largest source-backed performance candidates here are repeated event-prefix view reconstruction during experiment validation, full journal round-trip/rehashing on promotion, repeated dispatch context validation and deep workspace backup reads. They need bounded-history benchmarks and concurrency/read-coherence fixtures before implementation. Existing observational scores and self-reported outcomes are not independent proof of causal quality gains. Integrated orchestration and ordinary Wiki verification remain required for the two new dispatch fixes.

The dispatch fixes pass the complete **Orchestration integration shard in 156.78 seconds** and the ordinary Wiki gate **8/8 selected stages in 80.15 seconds**. These are local verification timings, not comparative speedup measurements. All 712 frozen legacy assertions remain unchanged.

Five further complete-source reviews cover the full task audit/handoff and delivery/implementation/extraction regressions. Their current hashes were verified before import after the running gates finished. Coverage is **397/409**, with **12 pending**. Compact handoff currently truncates output after all full assessments; audit repeatedly validates global journals per workspace. Per-file entries retain grounded benchmark and consistency candidates. This final update is documentation-only; runtime sources are identical to the verified ones.

## Remaining individual contracts and regressions

Nine complete-source reviews cover adaptive routing/experience, change-policy and API compatibility owners, real DTO selection/release-acceptance fixtures, governed authentication/extraction, path-layout ranking, SQL/.NET evaluation and test-plan precision. Coverage is **406/409**; only CI (1,411 lines), the facade (2,909) and legacy runner (5,334) remain, totaling **9,654 pending lines**. This pass changes documentation only and does not claim these suites were rerun.

The ledger retains exact-source candidates for case-sensitive API comparisons, complete schema facets, native Git/read-set provenance, rule/check dependency unions, batching Roslyn parsing and reducing repeated plan/workspace composition. Retrieval parity compares two active readers and must remain; committed historic quality/runtime figures are not a substitute for its fresh Node/.NET execution. Existing corpus counts, thresholds, assertions and command selection were unchanged.

The complete **1,411-line CI workflow** was subsequently read, including every Wiki/backend/frontend/setup/failure path. Coverage is **407/409**; the facade and legacy runner account for the remaining **8,243 lines**. Wiki aggregate checks preserve all applicable focused/full results. Repeated independent setup/compiler preparation is a timing candidate; pinning the Wiki .NET runtime, explicit focused timeout and exact failure attribution/log propagation remain source-backed reliability proposals. No CI job, test partition, notification behavior or threshold was changed or newly executed in this documentation-only pass.

## Complete facade pass

Every line and route in the **2,909-line facade** was read. Coverage is **408/409**; only the **5,334-line legacy runner** remains. Concrete per-file proposals cover request-owned dispatch/context reuse, duplicate trace probes/rendering, argument/alias consistency, detached launch portability, environment restoration, worker identity/cleanup and receipt publication validation.

Actual route execution reproduced a strict-publication contract defect: both `verify-strict-affected` and `ui-finalize` omitted `NoCache` on their smoke call. Both now forward `NoCache=true`; the regression also preserves their exact baseline/path scope. The pre-fix checks fail separately for each route and the corrected strict regression passes. Ordinary resumable verify keeps its existing cache policy. Integrated Wiki verification remains required after this facade change; no new speedup or full-audit completion is claimed.

The facade correction passes the ordinary Wiki gate **8/8 selected stages in 73.29 seconds**; eleven impacted pages are current/reviewed. This is scope-specific verification timing, not a speedup claim. The final report update is documentation-only and keeps runtime source identity unchanged. The legacy source has been read continuously through **line 1,360**, including every truncated gap; lines **1,361–5,334 remain pending**, so its whole-source ledger entry is deliberately still absent.

## Complete source coverage checkpoint

The remaining **5,334-line legacy runner** was read completely against its unchanged source hash. Every partial/truncated gap was recovered. The per-file ledger now covers **409/409 current source files**, with no pending source entry. `Test-Coverage.ps1 -FailOnIncomplete` must validate the current file set and reviewed hashes before completion. Complete source reading does not by itself prove final gate execution, product journey coverage or implementation of every proposal.

The legacy suite has substantial tamper, lifecycle, rollback and lineage controls; copied task evidence, manually resolved checks, conditional paths, count-only assertions and synthetic screenshot headers remain bounded mechanism fixtures. Repeated nested assessments and full registry/index reads are performance candidates. No assertion inventory, corpus count, threshold, shard membership or test-selection requirement was reduced in this read-only pass. Final scope-wide verification and the synthesis below remain required before the overall audit is declared complete.

## Prioritized implementation candidates

The per-file ledger is the complete analysis; this table groups recurring candidates into practical followup changes. Every row below is a **source-backed proposal**, not a reproduced defect or measured speedup. The 29 reproduced corrections remain separately documented in `findings.json` with their focused evidence.

| Priority | Owners | Proposed change | Evidence required before accepting the change |
| --- | --- | --- | --- |
| 1 | Task audit/handoff/workspace status, delivery assessment | Reuse one immutable, validated per-invocation packet/artifact/registry snapshot; compute compact handoff from the data it actually exposes; use ID maps for joins. | Cold/warm timings on fixed task histories; identical verdicts, errors, lineage and mandatory checks; reject inputs changed before publication. |
| 2 | Learning/instruction experiment and promotion journals | Replace repeated event-prefix replay with a single-pass state reducer; materialize each view once; preserve exact event/hash compatibility. | Bounded histories of increasing size, exact old/new output and tamper/transition parity, migration/rollback fixtures; no historical identity changes hidden as cleanup. |
| 3 | Context bundle/budget/benchmark/security | Reuse coherent source text, hashes, line maps and security assessments within one request; apply output budgets before optional hydration. | Required-source coverage, exact anchors/redaction/quarantine/budgets, missing/stale/partial distinctions and equal-size/time mutation fixtures. |
| 4 | API DTO parser, stage fingerprints and launchers | Batch Roslyn before/after sources; reuse request-owned native runtime information; render existing trace results instead of repeating the query. | UTF-8/Unicode and case parity, native error propagation, cancellation/deadlines, helper implementation/dependency identity and unchanged cache reuse controls. |
| 5 | Generators and source inventory | Measure forced inventory pruning and shared content/newline maps; preserve exact Git path and full/affected selection contracts. | Exact path membership including both rename endpoints, locale/migration pairs, hidden/link exclusions, no-op suppression and PS/runtime portability. |
| 6 | SQLite graph/MCP query composition | Hoist query-invariant ranking work, bound per-group hydration, and avoid diagnostics-only counts when diagnostics are disabled. | Current Node/.NET rank/top-five parity, all corpus thresholds/cohorts, readiness/freshness, SQL snapshot integrity and memory/latency measurements. |
| 7 | Wiki CI and full verification | Measure repeated compiler/dependency preparation; evaluate exact-input runtime/artifact caching across isolated jobs without sharing mutable checkouts. | Source/toolchain/OS cache keys, all 37 focused groups and every applicable Full shard retained, failed/cancelled/skipped aggregate rejection and actual job/step durations. |
| 8 | Governance mutation and publication | Introduce stable writer leases and crash-safe publication for journals/multi-file state; strengthen exact read-set/source identity checks. | Concurrent/killed writer and reader fixtures, recovery and retention bounds, no lost updates, coherent rollback and explicit unavailable/partial results. |
| 9 | Policy/API/facade contracts | Validate rename endpoint scope, same-ID requirement dependency unions, case-only/null/schema facets and complete facade argument/alias forwarding. | Small public-command counterexamples first; then precise regressions and existing API acceptance, scope and publication contracts unchanged. |
| 10 | Regression fixtures and cleanup | Add exact membership/nonvacuous controls, malformed/cancelled worker paths and containment/environment restoration where the ledger identifies gaps. | Stronger assertions without reducing counts, thresholds or scenario inventories; owned process and fixture cleanup demonstrated on both success and failure. |

The previously measured scoped publication no-op improvement (seconds to single-digit warm milliseconds) concerns one path only. Source inspection, different verification scopes, cached reruns and parallel job durations are not valid whole-Wiki speedup comparisons. Benchmarks should record exact source/input/toolchain identity and independent before/after samples.

## Completion evidence contract

Completion requires the current scope/hash checker with `-FailOnIncomplete`, a concrete performance/reliability entry for every source, classified findings/proposals, unchanged frozen assertion inventory, and applicable final Wiki checks. The final exhaustive verification uses the existing CI partition: Core, Workspace and Orchestration in isolated snapshots, plus the complete focused catalog/full index gate. Their union retains the 712 frozen assertions; executing a shard does not claim that unrelated product journeys or Linux runtime behavior were tested locally. All required process handles have now reported terminal success; the final results are recorded below.

## Final retrieval regression correction

The complete Core, Workspace and Orchestration audit shards finished successfully in **244.87s**, **575.72s** and **254.39s**, respectively. The final focused gate detected a real source-growth regression: the existing scheduler identity rule matched only `scheduler`, excluding the production `Schedule` tool while favoring the new scheduler-lock test. The unchanged unseen cohort fell from 13 to 12 top-one Wiki results.

WTA-029 corrects the existing rule to the common `schedule` stem. It does not add a path/query-specific rule or change any corpus, threshold, rule count or assertion inventory. Among the same 100 unseen cases, only case `unseen-v2-030` changes rank (2 to 1); Wiki top-one coverage returns to **13/16**, overall top-one is **73/100**, and all existing corpus gates pass. Exact current Node/.NET rank and top-five parity passes for four corpora totaling **302 cases**. Promoted evaluation retains **485/494 top-one** and **494/494 top-ten** results.

The final complete focused/index gate is being rerun against this correction. Core is also being rerun because it exercises retrieval; the unchanged Workspace and Orchestration runtime fixes retain their full successful shard evidence. Raw diagnostics and before/after query results remain under `.artifacts/wiki-tool-audit-20261006`. These concurrent Windows timings are execution evidence, not comparative speedup measurements.

## Final verified result

Complete source coverage passes **409/409** with no pending entries, stale reviewed hashes or PowerShell parse errors. All **29 reproduced defects** have focused verification; the final complete Focused gate passes every **37/37 catalog group**, all **12 index generators in Check mode**, and source-impact/change-policy/failure-registry checks in **672.53 seconds**. Its runtime and index inputs were held unchanged through execution and rechecked before delivery.

The full frozen audit retains **712 assertions** and the CI aggregate guard passes **64 result combinations**. Full Core passes again after the ranking correction in **213.07s**. Full Workspace (**575.72s**) and Orchestration (**254.39s**) passed the unchanged runtime fixes before the final policy-only stem correction. The correction itself passes all retrieval corpora and exact current Node/.NET parity on **302 cases**; the existing full MCP suite has **458 passed, zero failed and zero skipped**, with no subsequent C# runtime/test source edits. The per-file performance/reliability proposals remain classified followups, not unmeasured implementation claims.

These are local Windows execution results. Linux CI and deployment are not claimed, and concurrent timings do not establish whole-Wiki speedups. Historical checkpoint sections above record the progressive review; this final result supersedes their earlier pending counts.

## Compact handoff followup, 2026-10-07

WTA-030 implements the measured compact-handoff improvement. Missing confidence, critique and impact artifacts no longer trigger full-only synthesis for Compact. Saved artifacts retain the original verify branch, and full handoff still assesses missing artifacts. Readiness, continuity, pending evidence, journal decisions, context anchors and resume commands retain their public shape.

The current-source ABBA fixture records before **29.917s/16.499s**, after **6.897s/6.460s**, with exact compact JSON parity except generatedAtUtc. The first baseline includes cold work; the conservative warm comparison is **16.499s to 6.460s (about 2.55x)** on this one owned synthetic task. No whole-Wiki speedup or existing-receipt verification shortcut is claimed.

The dedicated regression passes on Windows PowerShell 7 and 5.1, including saved/malformed optional artifacts, required readiness failures, supplied snapshots, states and output formats. It joins the existing governed-delivery group; all 37 focused groups and the frozen legacy assertion inventory remain intact. Source scope grows to **410** with the new complete-source reviewed regression. Final integrated verification for this followup passes: all 37 focused groups, all 12 index Check stages, change policy and 21 current/reviewed affected Wiki pages. Full Workspace passes in 1235.12s, exercising real saved artifacts, lineage, handoff and refresh. The focused group runner records 1266.03s; concurrent gate durations are not speedup measurements. The frozen 712-assertion inventory and 64 CI result combinations remain intact. Runtime/index source hashes were held unchanged during these gates and rechecked before handoff.

## Master integration, 2026-10-07

The compact correction is committed as `6c48bdf68`. Integration retains inherited master `f777062a6` (user API SDK/lazy bindings). Only generated quality output and source-review provenance required reconciliation; both original review ledgers are retained locally and current-input reviews are refreshed. The merged runtime implementation, focused runner and regression have exact bytes from the fully verified compact commit. The combined affected Wiki gate passes **8/8 stages**, including governed-delivery, facade-contract and tool-contract, with current merged indexes and source provenance. Existing main-checkout working changes are preserved separately during the final fast-forward.
