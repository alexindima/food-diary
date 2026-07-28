---
id: workflow.change-policy
kind: workflow
status: current
sources:
  - .llm-wiki/policies/change-policies.json
  - .llm-wiki/tools/Test-LlmWikiChangePolicy.ps1
  - AGENTS.md
---

# Change Policy

The change-policy engine converts changed paths into deterministic checks,
review obligations, and structural invariants.

```powershell
./.llm-wiki/tools/Test-LlmWikiChangePolicy.ps1

./.llm-wiki/tools/Test-LlmWikiChangePolicy.ps1 `
  -BaseRef origin/master `
  -HeadRef HEAD `
  -FailOnViolation
```

Current policy families cover backend boundaries, HTTP contracts, paired
English/Russian localization, EF migration pairs, frontend verification,
security-sensitive areas, and LLM-Wiki freshness.

Structural violations fail immediately. Checks and human/agent review
obligations can additionally be validated against an evidence bundle with
`-EvidencePath` and `-RequireEvidence`.

Policies should encode stable repository requirements, not temporary task
preferences. New rules need positive and negative eval cases.
