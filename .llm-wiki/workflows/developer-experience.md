---
id: workflow-developer-experience
kind: workflow
status: current
title: Use the compact LLM Wiki developer experience
summary: Drive normal work through a small facade while preserving existing adaptive, manifest, journey, evidence, and delivery sources of truth.
tags:
  - workflow
  - developer-experience
  - planning
sources:
  - .llm-wiki/tools/Get-LlmWikiExperience.ps1
  - .llm-wiki/tools/Get-LlmWikiSolutionComparison.ps1
  - .llm-wiki/tools/Get-LlmWikiPhaseStatus.ps1
  - .llm-wiki/tools/Get-LlmWikiManualQaPlan.ps1
  - .llm-wiki/tools/Get-LlmWikiWorkflowMetrics.ps1
  - .llm-wiki/tools/Get-LlmWikiResearchPacket.ps1
  - .llm-wiki/policies/experience-policies.json
---

# Compact LLM Wiki developer experience

Normal development has five user-facing steps:

```text
develop -> next -> phase-next -> validate -> handoff
```

Start or inspect work with one command:

```powershell
./.llm-wiki/wiki.ps1 next -Intent "Fix the invitation link" -PlannedPath '<known path>'
./.llm-wiki/wiki.ps1 next -WorkspacePath .artifacts/llm-wiki/tasks/<name>
```

`next` prints exactly one recommended action and a short preview of later work.
It derives state from the adaptive route or existing governed workspace; it does
not create a second workflow record. `status` exposes the same compact pipeline
without replacing the detailed delivery and task status commands.

When a durable choice exists, compare options before design:

```powershell
./.llm-wiki/wiki.ps1 solutions -Intent '<outcome>' `
  -Option '<bounded option>','<structural option>'
```

The comparison is derived advice. Record the selected option and source evidence
through the existing `design -Decision` or task journal.

For governed work, inspect implementation phases with `phase-status` or
`phase-next`. `phase-complete -FailOnInvalid` verifies the selected manifest
phase against the current Git diff. Phase state is never stored separately from
the manifest and Git evidence.

Generate manual exploratory coverage with `qa`. Journey cases come from the
durable product journey catalog; generic failure and retry cases are always
included, while accessibility, localization, and mobile cases are added only for
frontend scope. Generated output is disposable unless a reviewed scenario is
promoted into the journey catalog.

`workflow-metrics` summarizes local workspace adoption and outcomes. Metrics are
signals for retrospective improvement, not proof that the Wiki caused quality.
Ceremony budgets keep tiny and bug work short and reserve governed workspaces and
independent critique for evidence that requires them.

The `ui-discovery` budget prevents intent-only UI wording from selecting a
governed route. It allows only runtime-owner research and grounded
reclassification; inferred paths cannot authorize edits or introduce backend
and auth context merely because the named screen belongs to a sensitive journey.

The `scope-discovery` budget applies the same rule to ambiguous non-visual
features and bugs. It allows a compact brief plus existing-flow research, then
requires reclassification with refined intent and confirmed paths. No design,
workspace, security, rollout, or implementation ceremony starts until evidence
proves the corresponding boundary. Existing sensitive fields remain visible as
review context without automatically escalating an unchanged data lifecycle.

The bounded cross-layer bug route treats layer count as transport shape rather
than feature scope. Once current sources prove one root cause and one existing
module flow, an additive Frontend/API/Backend fix uses a compact brief and trace,
implementation, focused tests, then diff plus `verify-fast`. Journey mapping,
design, and full local verification become conditional; publication hooks and
CI remain strict.

The `visual-ui-change` budget is a five-stage focused route for bounded frontend
rendering work with unchanged API, provider, persistence, privacy, security,
configuration, and architecture boundaries. It starts with a compact brief that
confirms runtime ownership, UI-kit versus application-shell placement, scoped
instructions, design-system constraints, and observable outcomes. It then moves
directly through implementation, combined focused tests and build, browser
evidence, and final diff plus `verify-fast`, avoiding a separate acceptance
ceremony, a full research packet, and unrelated full-index refreshes. The brief
constrains the work but does not invent the UX solution; current code, the
design system, and browser inspection remain the design inputs. Repository
pre-push and CI retain the complete frontend and Wiki verification as
publication gates. Browser QA follows the declared viewport scope instead of
requiring mobile proof for every desktop-only patch.

Research packets expose five stable lanes—flow, tests, integrations, precedents,
and guidance—so independent investigation can be parallelized without making
agent-specific scratch output part of the Wiki contract. Design checkpoints expose
one review checkpoint per implementation slice.
