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

Each policy rule should have:

- at least one positive classification case;
- a negative structural case when the rule can fail;
- expected modules/scopes/checks;
- no unexpected policy violations.

These evals measure routing and policy correctness, not the quality of generated
application code. Real-task outcome evals can be added after several weeks of
usage data.
