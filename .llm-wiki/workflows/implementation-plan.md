---
id: workflow-implementation-plan
kind: workflow
status: current
title: Generate an implementation plan
summary: Convert change context, consumers, invariants, policies, tests, and rollout constraints into an ordered executable plan.
tags:
  - workflow
  - planning
  - implementation
sources:
  - .llm-wiki/tools/Get-LlmWikiImplementationPlan.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskBrief.ps1
  - .llm-wiki/tools/Get-LlmWikiRolloutPlan.ps1
  - .llm-wiki/tools/Get-LlmWikiDecisionContext.ps1
---

# Generate an Implementation Plan

Create a plan from an explicit path set or the current diff:

```powershell
./.llm-wiki/wiki.ps1 plan `
  -Objective "Add an optional fasting start note safely" `
  -ChangedPath FoodDiary.Application/Fasting/Commands/StartFasting/StartFastingCommand.cs
```

The plan orders context discovery, contract migration, domain/data work, implementation, focused verification, deterministic artifact refresh, and release readiness. Phases include concrete files, expected evidence, and stop conditions.

When a compiled brief is available, the planner consumes it directly instead
of recomputing decision and rollout context.

The generator does not invent product acceptance criteria. If the objective is incomplete, resolve exact behavior before editing. Re-run the plan when the intended path set changes materially.
