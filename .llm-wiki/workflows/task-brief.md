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

Before implementation creates a diff, pass the expected files explicitly:

```powershell
./.llm-wiki/wiki.ps1 brief `
  -Intent "Add provider controls to account settings" `
  -PlannedPath FoodDiary.Application/Authentication/Commands/Example/ExampleCommand.cs
```

`Intent`/`PlannedPath` are aliases for the standard objective/proposed-path
inputs. Proposed and explicitly changed paths are combined for classification,
while `change.intent` and `change.proposedPaths` preserve planning provenance.

If only `-Intent` is supplied, the brief ranks matching C# and frontend symbols
and returns up to eight inferred paths. This mode is explicitly marked
`intent-inferred` with low confidence and provenance; confirm it with
`-PlannedPath` before treating the result as authoritative. An unscoped brief
warns that no diff, intent, or planned paths were supplied.

The brief combines changed scopes, directly affected and downstream modules,
scoped instructions, relevant wiki pages, focused tests, mandatory checks,
test scenarios, structural hotspots, direct test-reference gaps, review
obligations, structural violations, and a deterministic risk indicator.
The score prioritizes review depth; it is not a substitute for engineering
judgment or a security severity rating.

For agent context, prefer `brief -Compact -Format Json`. It retains risk,
paths, instructions, focused tests, scenarios, checks, and review obligations,
but replaces large consumer and contract objects with impact counts.
