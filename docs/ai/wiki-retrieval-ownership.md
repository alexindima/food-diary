# Retrieval across module ownership boundaries

## Scope and decision

Baseline: `cf6c6e7c5593d2a9833d61bf6ee6497cdd4b8607`, clean master.
This is the follow-up to `provider-adapter-ownership.md`, not another runtime
provider change. No application code, HTTP contracts, project references,
packages, credentials, migrations or coverage collection are involved.

The batch addresses structural search behavior rather than individual targets:

- Preserve the true module identity after relocation. A neutral, module-qualified
  Backend/Any request may enter through an Application interface, so waive the
  existing abstraction/interface penalties for that entry point. Explicit domain,
  HTTP, storage, integration, provider, options and test intent keeps the original
  penalties and layer preferences. No new module/file-specific boost is added.
- For an explicit test request, common vocabulary must not swamp a directly named
  subject. Compute integer inverse-frequency buckets from distinct candidate paths
  and direct filename terms; reuse the existing direct-filename score and cap.
  Duplicate code/query-document records cannot change document frequency. This
  does not filter candidates, alter recall limits or depend on requested top-N.
  Test-scaffolding tokens (`test/spec/feature` and their plurals) are excluded:
  an initial measured pagination regression exposed that these are not subjects.
- Mirror the existing Node Providers-to-Integrations selector alias in the MCP
  reader. Preserve the actual path and actual module identity in both readers.
- Recognize shared domain primitives with the existing domain selector and
  Shared/Tooling test locations with existing test selectors. Exact segments and
  test exclusions prevent unrelated folders from acquiring production ownership.

Policy weights, query normalization, corpus queries, acceptance thresholds,
historical measurements and public search result shapes remain unchanged.
The helper regression uses Inventory/Shipping/Acme/Zephyr rather than benchmark
queries. Measurements on previously observed corpora are regression evidence,
not a new blind or human-authored quality benchmark.

## Source-proven fixture maintenance

Sixteen obsolete targets in four corpora follow Git renames below. Only quoted
path literals change; reverse substitution must reproduce each original file.

| Targets | Rename evidence |
| --- | --- |
| ImageAsset and its PostgreSQL tests | `2650ae47f`, R100 |
| FoodQualityGrade | `902a43978` then `40c65d7bc`, R100 at both steps |
| ShoppingListItemId | `5ae722642`, R100 |
| RecipeUpdate and RecipeNutrition | `47d0dd4d3`, R100 |
| User.Lifecycle, UserRole, UserAiTokenLimitUpdate, UserProfileMediaState and UserSecurityVersionTests | `7a9a3fad8`, R100 |
| MealAiItem | `d3a1e7c65`, R100 |
| EmailAddress | `40c65d7bc`, R095; the rename diff changes only the namespace |
| ReportTargetType | `6803aeefa`, R100 |
| CycleId | `40c65d7bc`, R100 |
| PostCommitRecentItemUsageRecorder | `c0afb0edb`, R100 |

Exact old/new paths are retained in the task evidence mapping. Frozen100 has no
path maintenance and remains byte-identical. No accepted-target alternatives
are added, and no expected Application contract is replaced by its provider.

## Verification plan and evidence

Evidence: `.artifacts/wiki-retrieval-evidence`. Isolated .NET outputs:
`.artifacts/wiki-retrieval`. Native governed workspace:
`.artifacts/llm-wiki/tasks/wiki-retrieval-ownership`.

Before source edits, a fresh current graph and all eighteen search corpora were
measured. Synthetic layout regression failed before the implementation and
passed after it. Final verification will consolidate the full MCP suite, all
search corpora, exact Node/MCP rank and score parity, affected Wiki verification,
source-impact review and native delivery gates. Actual results and any remaining
failures will be recorded below; process exit zero alone is not semantic success.

## Measured results

The consolidated eighteen-corpus comparison contains 770 cases. Top-1 improves
from 708 to 723, Top-10 from 747 to 765; no previously found Top-10 target is lost.
Some improvements repair stale targets rather than ranking: those are explicitly
separated in `comparison-final.json` by `sameTargets` and the relocation audit.

| Diagnostic | Baseline | Final |
| --- | --- | --- |
| Unchanged frozen100 Top-1 / Top-10 | 96 / 99 | 96 / 100 |
| Target-aware unseen100 Top-1 / Top-10 | 60 / 82 | 72 / 95 |
| Validation50 Top-1 / Top-10 | 48 / 49 | 49 / 50 |
| Image30 Top-1 / Top-10 | 28 / 28 | 29 / 30 |
| Post-tuning30 Top-1 / Top-10 | 25 / 29 | 26 / 30 |
| Business20 Top-1 / Top-10 | 20 / 20 | 20 / 20 |

OpenFoodFacts' unchanged Application-entry query now returns its interface first,
score 1608, ahead of the provider at 1580. The provider remains correctly owned
by OpenFoodFacts. The YooKassa test target returns from rank 17 to rank 9.
The shared DomainGuard target enters Top-10 without changing frozen100.
An initial frequency implementation regressed the pagination probe from rank 1
to 3 by rewarding `FeatureTests`; excluding scaffolding restored probe7 to 40/40.
All original failed and intermediate measurements are retained.

Final Node and MCP readers have zero exact-rank or ordered Top-5 path/score
differences over all 770 cases (`parity-final/summary.json`). The final synthetic
fixture adjustment changed no search rows or features: complete table hashes
before/after are identical, not merely assumed equivalent.

The initial full MCP run passed 261/261. The scaffolding test extension then
exposed two over-constrained synthetic comparisons (259/261): subject and role
vocabulary differed simultaneously. The fixture now holds the provider/client
role equal while varying the subject versus scaffolding, retaining both its
Top-1 and no-generic-specificity assertions. All three focused cases pass. A
final complete, unfiltered MCP rerun then passed 261/261 with no skips
(`mcp-sealed.trx`); the earlier failed full run is not relabeled green. The
intermediate ledger uses VSTest test IDs because some long data rows have
identical truncated display names. Analyzer compilation failures are retained.

Architecture: 1138/1138; locked solution restore passed. NuGet's full-solution
audit reports 325 projects and no vulnerabilities. Both scoped format checks
pass. Runtime application/provider code remains unchanged; the prior extraction's
4138 runtime tests were not redundantly replayed for this search-only change.

## Remaining quality limitations

This is not an all-green search-quality claim. Existing strict requirements still
fail, even though aggregate retrieval improves:

- Image30 requires every target at rank 1; its relocated persistence test is
  rank 3 (29/30 Top-1, 30/30 Top-10).
- Probe6 remains exactly 28/30 Top-1 and MRR .9611, below .95/.97 floors. The
  Gamification read-service and Notifications factory targets are ranks 3 and 2.
- Unseen100 now meets aggregate floors, but integrations-persistence remains
  7/10 Top-10 against the minimum 8. Its two residual misses, a MailInbox test
  requested as an implementation and Export registration, have the same ranks
  23 and 62 in the exact baseline. Other remaining unseen misses concern an AI
  DTO, a MailRelay test requested as implementation, and a queued command versus
  its handler. They are not silently replaced with current first results.
- Post-tuning30's JSON threshold is permissive, but the full SQL script's
  stricter MRR floor .925 still exceeds current .9222. JSON `passed=true` does
  not certify that footer.

The actual facade outcome and native delivery outcome are retained in
`wiki-runs.json` and governed evidence. No failing check may be marked passed
solely from the aggregate gains. No thresholds, historical baselines, queries or
accepted alternatives were changed to clear these residual requirements.

The full facade actually exits 1 at the unchanged unseen100 cohort requirement
`integrations-persistence=7<8`. Adaptive evaluation and the complete code-graph /
trace-output smoke pass; context-bundle fails on quality, not a timeout. Standalone
failure-knowledge, change-policy, source-impact and architecture-health results
are retained separately. The final documentation-only closure does not rerun or
relabel this failed full verification. Governed `wiki-verify` remains failed;
this patch is a verified improvement checkpoint, not all-green Wiki acceptance.
