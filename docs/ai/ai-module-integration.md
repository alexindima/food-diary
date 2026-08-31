# Ai integration with explicitly deferred verification gaps

On 2026-08-31 the user explicitly requested integration before investigating the
remaining Wiki/PDF issues, so the completed extraction worktree can be closed.
This is permission to integrate locally, not a claim of fully green delivery or
permission to deploy/push.

## Provenance and scope

- Source commit: `9f1225b85d512ddb59974d43af73f22ed121c222`.
- Source base: `a11d9a5d2c4dce2682abb4691b2b23fb6b38b9a6`.
- Target before merge: `df532f21561c7f0260e8af436d3caa5514d5d2f2` (`master`).
- The target-only cleanup removes unused ContentReports/DailyAdvices leftovers;
  these deletions are retained. No product/project merge conflict occurred.
- Four generated Wiki indexes and the source-review ledger conflicted. Indexes
  are regenerated from the combined tree; old reviews do not certify new hashes.
- No runtime behavior, package versions, schema, provider calls, coverage
  collection, ranking or quality thresholds are changed by this integration.

## Evidence retained before closing the source worktree

All 2,716 files (529,502,385 bytes) from the source `.artifacts` were copied to
`C:/FD/.artifacts/ai-extraction-archive-9f1225b85/artifacts`; every source/copy
SHA-256 matched. Original embedded paths in receipts remain historical and must
not be rewritten to pretend that tests ran in master.

The archive includes `ai-evidence/handoff.json`, all TRX/logs, source/corpus audits,
the rejected governed delivery, Wiki state/cache and copied hook logs. The
source commit and branch remain in Git. This integration's own results live in
`C:/FD/.artifacts/ai-integration-evidence`.

## Source verification (not reruns on master)

Read directly from source TRX: Ai Application 59/59, Domain 23/23,
Infrastructure 87/87; Architecture 788/788; unfiltered PostgreSQL 116/116.
Source full solution build had zero warnings/errors; EF reported no model drift.
HTTP 175/175 excluded `PostgresPerformanceBaselineTests` and is not a full-suite
claim. Source commit used enabled hooks. See the detailed
[extraction report](ai-module-extraction-wiki-findings.md).

## Explicitly deferred work

1. Retrieval regressed at the measured extraction checkpoint: top1 92/100 and
   top10 96/100 versus the supplied base 94/98; accepted precision .9481 and
   error capture .5. AiQuotaReservation ranked 21, AiPromptTemplate ranked 12.
   This is not covered by the older unchanged-baseline exception. Re-measure
   the final combined tree before deciding on a generic fix; do not tune frozen
   targets, query text, thresholds or ranking to the holdout.
2. Source full Wiki verify hit its 600-second outer timeout. Later manual page
   edits left its active Wiki check pending. Partial/standalone passes are not
   a successful complete verify. Source governed validate/critique remain
   rejected (critique 24/100); integrating does not rewrite that evidence.
3. Central Infrastructure unit runs each had one failure: first
   `LoadMealImageAsync_WhenCallerCancels_PropagatesCancellation`, then
   `LoadMealImagesAsync_WhenReportDeadlineExpires_ReturnsWithoutImages`.
   No PDF code changed, but neither failure is proven to exist on the base.
   Diagnose timing/cancellation deterministically in a separate follow-up.

## Integration checks

Actual merged-tree checks:

- `dotnet restore FoodDiary.slnx --locked-mode` in the isolated integration
  output scope passed; lockfiles/project files are identical to the Ai commit.
- Full solution build passed with zero warnings/errors (9m11.77s).
- Module tests passed: Application 59/59, Domain 23/23, Infrastructure 87/87.
- Full ArchitectureTests passed 788/788. These 957 executed tests had no skips.
- `git diff 9f1225b85 -- '*.cs' '*.csproj' '*.slnx' .nuget/lockfiles` contains
  only the two previously removed, uncompiled orphan AssemblyInfo files.
- Native Wiki update passed; eight source-impact reviews were renewed against
  the combined source hashes. No inherited failed check was relabelled passed.
- One `wiki.ps1 verify -BaseRef 9f1225b85...` actually exited 1. The facade's
  effective affected selection included the staged extraction and two smoke
  groups, not just the target-only cleanup. Workspace policy, page contracts,
  lint regression and index freshness passed. Graph prewarm completed; adaptive
  evals passed. Context-bundle then failed its live quality gate after 280.68s:
  top10 96/100, accepted precision .9481, error capture .5. This was not a timeout
  and does not fit the older 98/100 baseline exception. No retry was performed.
- The separate new integration governance workspace has not been made green:
  initial automated planning retained a legacy Ai source-path criterion;
  validation reported unmapped administrative criteria/checks, and critique
  encountered a stale compiled projection while the verified graph refresh was
  underway. Those logs are retained; neither constitutes product test failure
  nor completed governance approval. The explicit user decision above permits
  this local merge with these open obligations, not a deployment approval.
- The integration output scope was cleaned with native `dotnet clean`, exit 0,
  zero warnings/errors. Master user build outputs and source evidence were not
  cleaned. The ordinary merge commit still uses enabled hooks.

Remaining tail checks and commit-hook output are retained in
`.artifacts/ai-integration-evidence`. The user-approved deferral remains in
effect; unrelated Wiki/PDF repairs are out of scope.
