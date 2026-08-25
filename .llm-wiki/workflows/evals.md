---
id: workflow.evals
kind: workflow
status: current
sources:
  - .llm-wiki/evals/cases.json
  - .llm-wiki/tools/Invoke-LlmWikiEvals.ps1
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

The independent 100-case context-search holdout is also a live regression gate,
not only frozen retirement evidence. It enforces aggregate Top-1, Top-10, MRR,
and per-cohort floors. The evaluator batches all requests through one Node/SQLite
process by default; use `-NoBatch` only for transport parity diagnosis. Record a
tamper-evident local benchmark snapshot, including corpus and ranking-policy
hashes plus a delta from the previous snapshot, with:

```powershell
./.llm-wiki/tools/Write-LlmWikiContextEvaluationSnapshot.ps1 -SkipBuild -FailOnRegression
```

The ranking policy has a 600-rule complexity budget and the tuned control
corpora may not outperform the blind holdout by more than 25 percentage points.
Prefer general role, layer, bounded-context, and runtime affinities over new
file-specific boosts. Promoted corpora require every expected result in Top-10;
their corpus-specific Top-1 and MRR floors remain authoritative. Requiring every
historical case at Top-1 would turn the promoted set into a tuning target and
conflict with the independent holdout's overfitting guard.

Strict adaptive verification partitions the suite into three stable shards and
runs them beside the independent routing and experience regression groups. Case
order determines shard membership, every static or promoted case is selected
exactly once, and a failure in any shard fails the whole gate. Direct `wiki
evals` remains a single-process complete run; `-ShardIndex` and `-ShardCount`
are internal performance controls for the orchestrator.
