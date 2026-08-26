---
id: workflow.evals
kind: workflow
status: current
sources:
  - .llm-wiki/evals/cases.json
  - .llm-wiki/evals/answer-quality-intake-template.json
  - .llm-wiki/tools/Invoke-LlmWikiEvals.ps1
  - .llm-wiki/tools/Complete-LlmWikiAnswerEvaluationCorpus.ps1
  - .llm-wiki/tools/Measure-LlmWikiAnswerQuality.ps1
  - .llm-wiki/tools/Get-LlmWikiDiffContext.ps1
  - .llm-wiki/tools/Test-LlmWikiChangePolicy.ps1
---

# AI Development Evals

The eval suite protects the quality of agent context and change-policy
classification with representative synthetic change sets.

```powershell
./.llm-wiki/wiki.ps1 evals
./.llm-wiki/wiki.ps1 evals -Detailed
```

Cases currently cover API/backend changes, complete and incomplete
localization pairs, complete and incomplete EF migration pairs, extracted
module detection, security-sensitive classification, and real-task navigation
regressions. Optional `traceQuery` and `privacyQuery` expectations assert that
an agent can find the expected flow and sensitive fields from a bug description.
The visual UI regression also fixes the compact five-stage contract: visual
brief, implementation, focused verification, browser evidence, and completion.
This protects the reduced ceremony without weakening the publication gate.
The dashboard contract-extension regression protects the distinction between
existing sensitive read-model data and a changed sensitive-data lifecycle: the
former remains a normal feature with API compatibility checks, while explicit
migration and authentication cases remain critical.
The local-day Dashboard bug regression ensures an additive query parameter can
cross frontend, HTTP, and application layers without being promoted to a
feature; the expected route remains the compact four-stage bug workflow.
The Dashboard period-selector regression keeps local interaction and component
state on the visual five-stage route when routes, persistence, API, and public
component contracts remain unchanged.
The Cycle repository regression prevents database vocabulary in a bounded
query-performance bug from forcing critical ceremony and prevents handler trace
from displacing explicit Infrastructure and integration-test paths.
Docker-build and Storybook dependency regressions protect the four-stage
maintenance route and ensure concrete diagnostics outrank heuristic discovery.

Each policy rule should have:

- at least one positive classification case;
- a negative structural case when the rule can fail;
- expected modules/scopes/checks;
- no unexpected policy violations.

These evals measure routing and policy correctness, not the quality of generated
application code. Real-task outcome evals can be added after several weeks of
usage data.

Answer quality has a separate, provider-neutral evaluation contour. First,
collect 100–200 real-user queries or independently human-authored questions in
the intake format before any answer is generated. Freeze that corpus with
`Complete-LlmWikiAnswerEvaluationCorpus.ps1`; the tool preserves query text,
rejects placeholders, requires opaque authorship/session evidence, and refuses
generator-authored intake. Generate answers as a separate submission whose
claims expose repository citations, then have a reviewer whose identity differs
from the generator score correctness, completeness, actionability, citation
quality, and unsupported claims. `Measure-LlmWikiAnswerQuality.ps1` validates
the evidence paths and line anchors and enforces the frozen thresholds. It does
not pretend to automate semantic judgment: reviewer scores and real-user query
provenance remain human evidence.

The independent 100-case context-search holdout is also a live regression gate,
not only frozen retirement evidence. It enforces aggregate Top-1, Top-10, MRR,
and per-cohort floors. The evaluator batches all requests through one Node/SQLite
process by default; use `-NoBatch` only for transport parity diagnosis. The gate
also builds the current .NET MCP and requires exact rank and ordered top-five
path/score parity with Node on the holdout and diagnostic unseen corpora. Record
a tamper-evident local benchmark snapshot, including corpus, engine source,
ranking-policy, and change-set hashes plus a delta from the previous snapshot,
with:

```powershell
./.llm-wiki/tools/Write-LlmWikiContextEvaluationSnapshot.ps1 -SkipBuild -FailOnRegression
```

Pass `-SummaryOutputPath .llm-wiki/generated/context-evaluation-summary.md`
when publishing a reviewed benchmark. The Markdown summary is generated from
the same snapshot, so documented Top-1, Top-10, MRR, performance, commit, corpus
hash, and policy hash cannot drift from the measured receipt.

Snapshot creation refuses a stale code graph when `-SkipBuild` is used; without
that switch it refreshes the graph before measuring. It also aborts if the
worktree changes during evaluation and records the actual `node-sqlite` reader
and context-search schema version.

The frozen 2026-08-26 diagnostic corpus in
`.llm-wiki/evals/context-search-unseen-20260826.json` contains 100 targets not
used by earlier committed search corpora. Its queries were generated from target
semantics, so it is target-aware synthetic evidence for regression diagnosis,
not evidence of real-user query quality. The immutable first run was 51/100
Top-1, 83/100 Top-10, and 0.6069 MRR; later runs are post-fix regression evidence,
not replacement blind baselines. A new corpus can be called independently
authored only when a person supplies every query before search execution; the
freeze tool rejects blank placeholders and never derives query text from targets.

Snapshots use one warm-up and three measured iterations by default. They record
the Git HEAD, dirty-state and diff hashes, PowerShell/Node versions, code-graph
parser version, median/p90/p95 timing summaries, and categorized non-Top-1
results. Use `-Iterations 1 -WarmupIterations 0` only for focused diagnostics
and regression tests, not for a performance baseline.

The ranking policy has a 700-rule combined complexity budget, with separate
ceilings of 400 normalization and 400 ranking entries. Tuned control corpora may
not outperform the blind holdout by more than 25 percentage points.
Prefer general role, layer, bounded-context, and runtime affinities over new
file-specific boosts. Promoted corpora require every expected result in Top-10;
their corpus-specific Top-1 and MRR floors remain authoritative. Requiring every
historical case at Top-1 would turn the promoted set into a tuning target and
conflict with the independent holdout's overfitting guard.
Exact-term and prefix keys with identical expansions may be grouped with `|`;
every segment remains an alternative while the policy keeps one behavior
definition.

Strict adaptive verification partitions the suite into three stable shards and
runs them beside the independent routing and experience regression groups. Case
order determines shard membership, every static or promoted case is selected
exactly once, and a failure in any shard fails the whole gate. Direct `wiki
evals` remains a single-process complete run; `-ShardIndex` and `-ShardCount`
are internal performance controls for the orchestrator.
