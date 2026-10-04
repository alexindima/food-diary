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
Domain invariant review covers `Modules/<Module>/Domain`, shared domain
primitives, and the remaining legacy domain projects.
Data-access review covers module infrastructure and the independent mail-service
infrastructure under `Services/MailInbox` and `Services/MailRelay`. Legacy mail
paths still match during extraction diffs. Migration and test paths remain
excluded from this rule. The focused policy group exercises current real paths
and positive and negative path cases in its persistence regression.
Angular TypeScript/templates require public component-contract review. Pure
CSS/SCSS changes retain rendered visual and accessibility review without
claiming that selectors, inputs, outputs, translations, or API shape changed.

Structural violations fail immediately. Checks and human/agent review
obligations can additionally be validated against an evidence bundle with
`-EvidencePath` and `-RequireEvidence`.

Policies should encode stable repository requirements, not temporary task
preferences. New rules need positive and negative eval cases.
