# Bounded retrieval quality follow-up

Baseline: `ee6564652427375bc30e73a0ebbcc05c5ff7189f` (2026-09-04).
This follows [retrieval ownership](wiki-retrieval-ownership.md). It does not
re-author the benchmark, change accepted targets, tune ranking weights, weaken
thresholds or rewrite historical measurements. Concurrent provider ownership
work is separate from this search-engine patch.

## Proven generic defect

Module test selector aliases recognized `Infrastructure.Integration.Tests` but
not the repository's actual `Infrastructure.IntegrationTests` directory suffix.
Both Node's `rankingPathIdentities` and MCP's `GetRankingPathIdentities` therefore
lost existing legacy test selectors after relocation. The real path was present
in the index; this was a selector-equivalence failure, not missing graph data.

The fix recognizes the exact additional suffix, preserving the original path,
module-name equality, existing dotted spelling, Windows normalization and all
production/test boundaries. It does not add any selector or score to policy.
The already-existing Image integration-test role illustrates the effect, but
the regression uses Inventory/Shipping stock tests and a synthetic selector.
The Node regression fails before the helper change and passes afterward.

Affected implementation and tests:

- `.llm-wiki/tools/code-graph-path-layout.mjs`
- `.llm-wiki/tools/Test-LlmWikiRankingPathLayout.mjs`
- `FoodDiary.Development.Mcp/Wiki/SqliteWikiContextSearch.cs`
- `Tooling/tests/FoodDiary.Development.Mcp.Tests/SqliteWikiContextSearchTests.cs`

## Correct interpretation of the strict failures

The earlier report incorrectly described unseen's cohort floor as Top-10.
`Measure-LlmWikiSqlContextEvaluation.ps1` compares `minimumCohortTop1Counts`
against each cohort's `top1Count`. The integrations-persistence cohort contains
16 cases: **Top-1 7/16 against minimum count 8; Top-10 14/16**. Its two Top-10
misses are not the complete set relevant to this Top-1 floor. The earlier report
is corrected without changing its evidence logs.

Fresh pre-fix evaluation on one rebuilt graph reproduces the previous results:

| Corpus | Top-1 | Top-10 | MRR | Strict result |
| --- | --- | --- | --- | --- |
| Target-aware unseen100 | 72/100 | 95/100 | .7955 | FAIL: cohort Top-1 7 < 8 |
| Image30 | 29/30 | 30/30 | .9778 | FAIL: requires 30 Top-1 |
| Probe6 | 28/30 | 30/30 | .9611 | FAIL: Top-1 .95 and MRR .97 floors |
| Post-tuning30 | 26/30 | 30/30 | .9222 | FAIL: full SQL MRR .925 floor |

All four measurement processes exit zero without `-FailOnRegression`.
Image30 and Probe6 nevertheless report `passed=false`; unseen reports
`passed=true` but `liveRegressionPassed=false`. Post-tuning's permissive JSON
reports success while the separate assertion in
`Test-LlmWikiSqlContextEvaluation.ps1` requires MRR >= .925. These are distinct
contracts, not interchangeable success signals. This patch does not consolidate
or silently relax them.

## Residual diagnosis and bounded next decision

Some targets are genuinely more relevant than the current first result, but
that alone does not establish a safe general scoring repair. Source inspection
separates the following cases:

- **Conflicting test/implementation intent:** unseen `005` asks for a storage or
  provider implementation but requires `SmtpInboundMessageStoreTests`; `079`
  asks for an implementation contract but requires `MailRelayClientTests`.
  Production store/client results are reasonable entry points. Promoting tests
  for these phrases would conflict with the existing explicit-test distinction.
- **Narrow contract versus flow entry point:** unseen `001` requires the
  `AiUsageDailySummary` record (date and token counts), whereas service contracts
  match the general usage-summary phrase. `091` explicitly names a queued
  command but also says implementation; its handler is first. Export `065`
  names dependency injection, correctly pointing toward `ModuleRegistration`
  and its `AddExportInfrastructure`, but mixes that with storage/provider
  implementation intent. These are open intent-priority decisions, not evidence
  for file-specific boosts.
- **Real relevance gap without an isolated generic defect:** Probe6's
  `GamificationReadService.GetAsync` actually computes streaks, weekly adherence,
  badges and health score. The first Users profile interface only retrieves a
  profile. `NotificationFactory` covers recommendations, client tasks,
  invitations and fasting; the first `FastingNotificationFactory` covers only
  the fasting subset. Full behavioral coverage should be evaluated separately
  from filename/role affinity before changing generic weights or query
  normalization. No such weight change is justified by these two observations.
- **Plausible alternative flow entry points:** post-tuning `back-002` first
  returns `AdminContentReadService.GetLessonsAsync`, which delegates to the
  expected `LessonAdministrationReadService`. `front-005` first returns
  `CalorieGoalFacade`, which uses the same goals API as the expected broader
  facade. `test-005` first returns `ContentInvariantTests`, which genuinely tests
  FavoriteMeal invariants; the expected `FavoriteInvariantTests` covers products
  and recipes. The broad queries do not distinguish all these scopes.
- **Operation versus adapter role:** post-tuning `data-004` requires
  `ImageObjectDeletionOutbox.EnqueueAsync`; the delete-image handler and outbox
  processor are related but different responsibilities. Its Database selector
  gives useful intent, but changing its precedence globally requires independent
  regressions beyond this single corpus case.

The next decision is whether retrieval acceptance measures a unique exact file
or a useful, source-grounded entry point for broad queries. If the latter is
desired, curate a separately versioned, independently reviewed relevance set
with explicit alternatives and preserve all existing strict results. If unique
targets remain required, retain the failures and first design independent
multi-subject/role-conflict synthetic cases. Neither option authorizes changing
this frozen corpus in place. Further benchmark-driven tuning stops here.

## Evidence and verification

Evidence lives in `.artifacts/wiki-quality-followup-evidence`. MCP verification
reuses the consolidated solution build in `.artifacts/auth-storage` with
`--no-build --no-restore`; no separate follow-up build scope was created. Native discovery uses
`.artifacts/llm-wiki/tasks/wiki-quality-followup-20260904`; `develop` selected the
small assessment route, not a forced governed implementation workspace.
`develop`, `research`, `test-plan`, `failures` and `diff` exited zero.

`baseline/` retains the four raw pre-fix measurements, process exits and corpus
hashes. `snapshot/` preserves that SQLite graph and dependency fingerprint before
the concurrent production relocations, so engine-only attribution need not be
confounded with layout changes. `synthetic-before.log` retains the expected Node
assertion failure.

The engine-only replay on that same graph covers 190 cases. Exactly one expected
rank changes: `fresh-image-10` moves from 3 to 1 (score 2502), making Image30
30/30 Top-1, 30/30 Top-10 and MRR 1. All other expected ranks are unchanged;
unseen's cohort failure, Probe6 and post-tuning's strict MRR failure remain.
The copied corpora match their recorded baseline hashes. The SQLite file is
byte-identical before and after replay (SHA256
`037fd571e666dd3c2a81520b66ad1bffac32963bdc09c854f7192bbd208cb4a5`).
`engine-only/comparison.json` and `engine-only/raw.json` retain the observations.
This offline diagnostic is not a substitute for the official acceptance facade.

Final runtime parity and combined-layout results are retained separately after
execution; this document is not an all-green Wiki acceptance claim. The MCP
evaluation runner reports corpus thresholds and switch criteria, not the Node
live-regression cohort gate or the SQL script's extra post-tuning assertion.
Reader parity therefore means equal ranks and ordered Top-5 paths/scores, not
that their `passed` fields certify the same strict contract. The final combined
18-corpus measurements and parity comparison are written to
`.artifacts/wiki-quality-followup-evidence/final/summary.json` after the final
source/documentation freeze and shared graph refresh; this link does not assert
that those checks passed.

The full MCP suite passes **266/266, zero skipped**, using the consolidated
build (`mcp-frozen.trx`, `mcp-frozen.log`). All five new synthetic selector cases
pass. The preceding complete run is retained as **265 passed, 1 failed** in
`mcp-final.trx`: a concurrent report edit invalidated the graph during
`ConfiguredServer_AggregatesContextWithoutLockingBuildOutput`. This was not
waived as a baseline failure; the entire suite was rerun with all indexed source
and documentation frozen. The configured-server smoke performs its own graph
refresh and a build-lock check, even when the outer test command uses
`--no-build`. An initial consolidated build's IDE0008 finding in the new test
was corrected to an explicit `JsonNode` type before the successful build/run.

`git diff --check` and the final standalone Node layout regression pass. No
coverage collection, full application-test rerun, package/policy update, commit,
staging or deployment was performed by this follow-up.
