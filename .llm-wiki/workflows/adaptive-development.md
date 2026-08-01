---
id: workflow-adaptive-development
kind: workflow
status: current
title: Route a FoodDiary change through an adaptive AI development flow
summary: Select the smallest safe research, design, governance, review, and verification path from task intent and grounded repository evidence.
tags:
  - workflow
  - development
  - research
  - design
  - handoff
sources:
  - .llm-wiki/tools/Get-LlmWikiAdaptiveWorkflow.ps1
  - .llm-wiki/tools/Get-LlmWikiResearchPacket.ps1
  - .llm-wiki/tools/Get-LlmWikiGitPrecedents.ps1
  - .llm-wiki/tools/Get-LlmWikiDesignCheckpoint.ps1
  - .llm-wiki/tools/Manage-LlmWikiAdaptiveSession.ps1
  - .llm-wiki/tools/Test-LlmWikiAdaptiveWorkflow.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskBrief.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskHandoff.ps1
  - .llm-wiki/tools/Manage-LlmWikiPlanConformance.ps1
  - .llm-wiki/tools/Invoke-LlmWikiDeliveryWorkflow.ps1
  - .llm-wiki/tools/Find-LlmWikiProductJourney.ps1
  - .llm-wiki/tools/Get-LlmWikiFrontendRuntimeOwner.ps1
  - .llm-wiki/knowledge/product-journeys.json
---

# Adaptive FoodDiary Development

Use one entrypoint before a non-trivial bug or feature:

```powershell
./.llm-wiki/wiki.ps1 develop `
  -Intent "Fix the dietologist invitation email link"
```

Add `-PlannedPath 'path/one;path/two'` whenever likely files are known. Intent-only
classification is allowed, including Russian task descriptions, but remains lower
confidence until repository paths are grounded. Do not treat an inferred path as
authorization to edit it.

## Profiles

The router selects one FoodDiary-specific profile:

| Profile | Typical change | Required flow |
|---|---|---|
| `tiny` | bounded presentation-only HTML/SVG/SCSS or equally local low-risk work | research, implementation, diff/test-plan, `verify-fast` |
| `bug` | corrective behavior in one bounded flow | research, implementation, focused tests, diff, full Wiki verify |
| `feature` | new behavior or a cross-cutting product slice | research, design, implementation phases, conformance-aware review, full verify |
| `critical` | auth, credentials, identity/private data, payments, migrations, providers, email/invitations, configuration, or delivery boundaries | research, decision checkpoint, design, governed workspace, full verify, independent critique |
| `architectural` | project references, DI/ownership boundaries, module topology, or durable architecture constraints | research, decision checkpoint, design, governed workspace, conformance, independent critique |

The profile is a routing decision, not a waiver. Actual diff evidence may elevate it.
Run `develop` again with changed paths, or use `diff` and `task-refresh`, when the
implementation boundary changes.

## Grounded research

Compile a bounded research packet before editing:

```powershell
./.llm-wiki/wiki.ps1 research `
  -Intent "Fix the dietologist invitation email link" `
  -PlannedPath 'FoodDiary.Application/Dietologist;FoodDiary.Web.Client/src/app/features/dietologist'
```

The packet combines ranked current-source paths, symbols, routes, DI, focused tests,
scoped guides, Wiki pages, verified failure knowledge, and Git precedents. Every
category exposes provenance. Current code, tests, accepted ADRs, current docs, and
scoped `AGENTS.md` remain authoritative; indexes and history are navigation evidence.

Git history can also be queried directly:

```powershell
./.llm-wiki/wiki.ps1 precedents `
  -Intent "Improve photo annotation visibility" `
  -PlannedPath 'FoodDiary.Web.Client/src/app/components/shared/ai-input-bar/ai-photo-result'
```

A precedent is never automatically a recommended pattern. Verify it against current
source and note follow-up fixes or superseded architecture before reuse.

Map the task to durable product journeys with `journeys`. Journey scenarios become
candidate acceptance mappings and end-to-end regression scope; they remain reviewed
navigation evidence, not proof of runtime execution.

For a visual frontend change, confirm the runtime owner before accepting an
inferred path or journey:

```powershell
./.llm-wiki/wiki.ps1 ui-trace `
  -Query 'dashboard AI photo annotation result' `
  -PlannedPath 'FoodDiary.Web.Client/src/app/components/shared/ai-input-bar/ai-photo-result/ai-photo-preview/ai-photo-preview.html'
```

The trace walks template consumers from the rendered component back toward the
feature entry point. A bounded frontend-only layout change with no API,
provider, persistence, privacy, security, configuration, or architecture
boundary uses the `visual-ui-change` profile: runtime-owner research, explicit
acceptance, implementation, focused tests, frontend build, and browser evidence.
It does not require a governed workspace or full index regeneration merely
because the surrounding product journey is critical.

For this profile, `verify-fast -VisualUiCompletion` is the local completion
gate when the final diff remains frontend-only and does not change contracts,
dependencies, architecture, providers, persistence, privacy, security, or
configuration. Full `npm run verify` and full Wiki verification are publication
gates supplied by the repository pre-push hook and CI; they are not repeated
before local completion. Browser evidence covers the viewport named by scope.
Mobile evidence is required only for responsive or mobile behavior; otherwise
record mobile as explicitly out of scope.
For upload-driven rendering, use `visual-qa`: its default mode validates and
prints the browser contract without side effects, while `-Run` uses Playwright
to upload the declared fixture, wait for the expected result selector, reject
console or page errors, and capture the screenshot automatically.

Research deliberately exposes blocking open questions instead of filling them with
heuristics. When no implementation path is grounded, discover an exact route,
command, handler, component, or service with `trace`/source search and rerun research
with `-PlannedPath`.

## Conditional design checkpoint

Feature, critical, and architectural profiles use:

```powershell
./.llm-wiki/wiki.ps1 design `
  -Intent "Link Google login to an existing account" `
  -PlannedPath 'FoodDiary.Presentation.Api/Features/Auth;FoodDiary.Web.Client/src/app/features/auth' `
  -Decision 'Use the authenticated current-user link flow; ownership is proven by normal login and the existing handler enforces Google ID uniqueness.'
```

The checkpoint separates current-state evidence from target behavior, lists stop
conditions and compatibility gates, exposes unresolved decisions, compares bounded
change against structural change, and emits ordered implementation phases. Do not
start implementation while a blocking question lacks current-source evidence or an
explicitly recorded product decision.

`tiny` and ordinary bounded bug profiles skip design unless the actual evidence
introduces ambiguity or a durable constraint.

## Long-running tasks

Critical, architectural, and sufficiently cross-cutting changes should use
`task-start`. At a session boundary:

```powershell
./.llm-wiki/wiki.ps1 pause `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name>
```

`pause` creates a bounded normal handoff plus an adaptive continuity receipt binding
Git HEAD and the current packet fingerprint. It does not silently refresh or bless
stale evidence.

Resume with:

```powershell
./.llm-wiki/wiki.ps1 resume `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name>
```

`resume` validates workspace integrity, compares Git HEAD, reports paths changed
since the pause, detects packet drift, and refuses to recommend direct continuation
when derived context requires refresh. Previous checks remain subject to their normal
evidence lineage and freshness rules.

## Completion

The adaptive route never replaces deterministic gates:

1. Confirm actual scope and journey impact with `diff`, `journeys`, and `delivery-status`.
2. Use `delivery-replan -Reason <evidence>` only for intentional divergence; it
   refreshes evidence but never widens the task contract.
3. Execute focused checks and the profile-selected Wiki verification.
4. Map and resolve every acceptance requirement with current implementation or
   verification evidence.
5. Run `delivery-validate -FailOnInvalid` to combine requirement quality,
   acceptance coverage, plan conformance, proof-of-change, and readiness.
6. For critical or architectural work, run `delivery-critique -FailOnInvalid` as
   an adverse review independent of the implementation narrative.
7. Seal and verify governed workspaces before claiming completion.

The objective is proportional rigor: small local changes stay fast, while sensitive
or structural changes receive explicit research, decisions, evidence, and independent
review.

## Compact execution interface

The adaptive route exposes a ceremony budget for every profile. Use `next` to
obtain one recommended command without manually coordinating the internal tools:

```powershell
./.llm-wiki/wiki.ps1 next -Intent '<task>' -PlannedPath '<known path>'
./.llm-wiki/wiki.ps1 next -WorkspacePath .artifacts/llm-wiki/tasks/<name>
```

When multiple durable approaches remain, run `solutions` before `design`. During
governed implementation, use `phase-next`; phase state is derived from the change
manifest and Git diff. Research packets group evidence into flow, tests,
integrations, precedents, and guidance lanes so investigations can run independently
without persisting agent scratch output as a second source of truth.
