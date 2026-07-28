---
id: workflow-decision-context
kind: workflow
status: current
title: Review architecture decision context
summary: Detect ADR review triggers and find existing decisions related to the changed scopes and modules.
tags:
  - workflow
  - adr
  - architecture
sources:
  - docs/adr/README.md
  - docs/adr/template.md
  - .llm-wiki/tools/Get-LlmWikiDecisionContext.ps1
---

# Review architecture decision context

Decision matching can consume a compiled diff and policy snapshot, ensuring ADR
guidance is derived from the same inputs as the task brief.

```powershell
./.llm-wiki/wiki.ps1 decision
```

The command reports whether deterministic architecture triggers matched, lists
decision drivers from the change, and searches accepted ADRs for related modules
and concepts. A trigger requires a review decision, not necessarily a new ADR.
Create a record only for durable boundaries, ownership, deployment, consistency,
public-contract policy, or enduring engineering policy.
