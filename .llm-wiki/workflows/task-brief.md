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

Intent-inferred paths are low-confidence navigation evidence. For UI work,
confirm the runtime owner with `ui-trace` and supply `-PlannedPath` before risk
classification or editing.

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

A broad repository audit is a separate mode, not an unscoped feature request.
When intent names a repository-wide assessment across multiple risk dimensions,
the brief returns `analysis.mode=broad-assessment`, eight assessment lanes, the
33 application modules as assessment coverage, a representative cross-layer test
sample, and a high review-exposure score. That score expresses required audit
breadth rather than a confirmed defect or changed-code severity. Its next step
links topology, privacy, security, architecture health, quality, dependencies,
journeys, and the repository-assessment test plan.

C# and frontend intent candidates use one refreshed SQLite compiled-context
selection by default. After the established PowerShell scoring infers paths,
the nested diff filters that same safe candidate superset to exact C# and
frontend symbol paths instead of starting a second Node process. This removes
the direct frontend-index JSON parse without adding a round trip; risk,
provenance, inferred paths, and downstream planning shapes remain unchanged.
The `analysis.compiledIndex` diagnostic reports source, selection mode, SQL and
round-trip duration, scanned/candidate counts, source hashes, direct source
bytes, and whether the selection was reused for diff. Missing or stale required
projections fail explicitly; `-CompiledIndexSource Json` exists for parity tests
and diagnostics only. The query cache keys the selected source and uses the
graph dependency fingerprint for the SQLite route, preventing SQL and
JSON-baseline results from colliding.

Risk impact records also use SQLite by default. One `task-brief-impact` action
selects exact changed-path records from quality, runtime topology, sensitive
data, frontend contracts, domain data, backend contracts, and architecture
health. It preserves source order and repeated records, includes the global
architecture violation sets, and fails if any projected source hash is missing
or stale. `analysis.impactIndex` reports scanned/candidate/returned records, SQL
and round-trip timing, all seven hashes, bytes verified for freshness, and bytes
actually materialized. `-CompiledIndexSource Json` is the explicit full-parse
baseline, not an automatic fallback.

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

Intent inference uses explicit frontend and backend vocabulary before ranking
symbols. Visual terms such as SVG, SCSS, styling, layout, and component keep
the candidate set in frontend sources unless the intent also names a backend
concern. Only candidates within one point of the best score are retained.

Presentation-only changes contain templates, styles, SVG, and tests but no
production TypeScript. Existing state-related words in those files do not by
themselves imply changed interaction behavior. Their risk profile and uncertain
intent-inferred work are capped at medium unless API, database, deployment,
configuration, security, or privacy evidence independently requires more.
The risk packet exposes `rawScore`, `profile`, and `calibration` so callers can
see when such a cap was applied.

For agent context, prefer `brief -Compact -Format Json`. It retains risk,
paths, instructions, focused tests, scenarios, checks, and review obligations,
but replaces large consumer and contract objects with impact counts.
