---
id: workflow-task-brief
kind: workflow
status: current
title: Build a task brief
summary: Compile risk, ownership, knowledge pages, tests, policies, and review obligations for the current change.
tags:
  - workflow
  - planning
  - risk
sources:
  - .llm-wiki/tools/Get-LlmWikiTaskBrief.ps1
  - .llm-wiki/tools/Get-LlmWikiDiffContext.ps1
  - .llm-wiki/tools/Get-LlmWikiOwnershipImpact.ps1
  - .llm-wiki/tools/Test-LlmWikiChangePolicy.ps1
---

# Build a task brief

The brief computes diff and policy once, then passes those compiled objects to
ownership, test-plan, rollout, and ADR analysis. Its JSON also includes the
resolved rollout plan and decision context for downstream planning tools.

Use one command to prepare or review a change:

```powershell
./.llm-wiki/wiki.ps1 brief
```

The brief combines changed scopes, directly affected and downstream modules,
scoped instructions, relevant wiki pages, focused tests, mandatory checks,
test scenarios, structural hotspots, direct test-reference gaps, review
obligations, structural violations, and a deterministic risk indicator.
The score prioritizes review depth; it is not a substitute for engineering
judgment or a security severity rating.
