# Wiki tool performance and reliability audit

Status: **in progress**. This is a complete-scope code audit, not a claim that
all tools have been reviewed or optimized.

The audit now covers **409 files** (408 in its initial inventory), with **74,954 lines in its initial baseline**:
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
  Current checkpoint: **364 reviewed; 45 still pending**.
- [Findings](wiki-tools-audit-2026-10-06/findings.json) includes concrete triggers,
  consequences, test gaps, remediation and evidence references. All twenty-three fixes
  have passed focused regression checks.
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
