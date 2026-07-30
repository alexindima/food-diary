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
  -PlannedPath @(
    'FoodDiary.Web.Client/src/app/features/profile/pages/account.ts'
    'FoodDiary.Web.Client/src/app/features/profile/pages/account.html'
  )
```

`Intent`/`PlannedPath` are aliases for the standard objective/proposed-path
inputs. Proposed and explicitly changed paths are combined for classification,
while `change.intent` and `change.proposedPaths` preserve planning provenance.

If only `-Intent` is supplied, the brief ranks matching C# and frontend symbols
and returns up to eight inferred paths. This mode is explicitly marked
`intent-inferred` with low confidence and provenance; confirm it with
`-PlannedPath` before treating the result as authoritative. An unscoped brief
returns a structured `nextSteps` entry with copyable commands instead of only
an empty risk packet. For simpler shell input, multiple paths may be supplied
as one semicolon-delimited value: `-PlannedPath 'path/one;path/two'`.
The unscoped response is produced before policy, ownership, test-plan, rollout,
decision, or compiled-index analysis. With no task evidence to rank, avoiding
those repository-wide dependencies keeps the result fast and deterministic
across shells and CI environments.

The brief combines changed scopes, directly affected and downstream modules,
scoped instructions, relevant wiki pages, focused tests, mandatory checks,
test scenarios, structural hotspots, direct test-reference gaps, review
obligations, structural violations, and a deterministic risk indicator.
The score prioritizes review depth; it is not a substitute for engineering
judgment or a security severity rating.

Frontend risk also accounts for modal/dialog flows, responsive breakpoints,
accessibility contracts, and multi-state interactions. These signals are
derived from the intent, path names, and existing scoped source, and keep
interactive UI changes from defaulting to low risk merely because no API or
database boundary changed.

For agent context, prefer `brief -Compact -Format Json`. It retains risk,
paths, instructions, focused tests, scenarios, checks, and review obligations,
but replaces large consumer and contract objects with impact counts.
