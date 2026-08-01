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

Do not generate implementation phases from intent-inferred UI paths alone.
Confirm the runtime owner and rerun with `-PlannedPath` first.

Create a plan from an explicit path set or the current diff:

```powershell
./.llm-wiki/wiki.ps1 plan `
  -Objective "Add an optional fasting start note safely" `
  -ChangedPath FoodDiary.Application/Fasting/Commands/StartFasting/StartFastingCommand.cs
```

The plan orders context discovery, contract migration, domain/data work, implementation, focused verification, deterministic artifact refresh, and release readiness. Phases include concrete files, expected evidence, and stop conditions.

When a compiled brief is available, the planner consumes it directly instead
of recomputing decision and rollout context.
Use `brief -ProposedPath` or `test-plan -ProposedPath` for early exploration;
promote the settled path set to this plan's explicit `-ChangedPath` input.

The generator does not invent product acceptance criteria. If the objective is incomplete, resolve exact behavior before editing. Re-run the plan when the intended path set changes materially.
