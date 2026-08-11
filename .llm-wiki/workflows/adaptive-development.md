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
  - .llm-wiki/tools/Invoke-LlmWikiReadOnlyTool.ps1
  - .llm-wiki/tools/Test-LlmWikiReadOnlyGuard.ps1
  - .llm-wiki/tools/Start-LlmWikiDevelopment.ps1
  - .llm-wiki/tools/Get-LlmWikiAdaptiveWorkflow.ps1
  - .llm-wiki/tools/Get-LlmWikiResearchPacket.ps1
  - .llm-wiki/tools/Get-LlmWikiGitPrecedents.ps1
  - .llm-wiki/tools/Get-LlmWikiDesignCheckpoint.ps1
  - .llm-wiki/tools/Manage-LlmWikiAdaptiveSession.ps1
  - .llm-wiki/tools/Test-LlmWikiAdaptiveWorkflow.ps1
  - .llm-wiki/tools/Invoke-LlmWikiAdaptiveVerification.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskBrief.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskHandoff.ps1
  - .llm-wiki/tools/Manage-LlmWikiPlanConformance.ps1
  - .llm-wiki/tools/Invoke-LlmWikiDeliveryWorkflow.ps1
  - .llm-wiki/tools/Find-LlmWikiProductJourney.ps1
  - .llm-wiki/tools/Get-LlmWikiFrontendRuntimeOwner.ps1
  - .llm-wiki/tools/Manage-LlmWikiTaskBaseline.ps1
  - .llm-wiki/tools/Resolve-LlmWikiSession.ps1
  - .llm-wiki/tools/Test-LlmWikiTaskBaseline.ps1
  - .llm-wiki/tools/Manage-LlmWikiVerificationCache.ps1
  - .llm-wiki/tools/Test-LlmWikiVerificationCache.ps1
  - .llm-wiki/tools/LlmWikiQueryCache.ps1
  - .llm-wiki/tools/Test-LlmWikiQueryCache.ps1
  - .llm-wiki/knowledge/product-journeys.json
---

# Adaptive FoodDiary Development

For a non-trivial feature, prefer `./.llm-wiki/wiki.ps1 start -Intent <task> [-PlannedPath <paths>]`. It captures the task baseline before discovery, compiles research and a scope-aware acceptance checklist, and creates the governed workspace immediately when the adaptive route requires one and concrete paths are known. The checklist covers API/OpenAPI compatibility, migration pairs, notification safety, background-job configuration and direct consumers, frontend states, localization parity, and architecture boundaries only when applicable.

Use `develop` as the read-oriented classifier when automatic workspace creation is not wanted.

Read-oriented facade commands run under a shared index lock and byte-preserving guard for compiled Wiki, knowledge, and review files. An unexpected write is reverted and reported as a tool defect. When `-PlannedPath` is explicit, research and planning use that scope instead of inheriting an unrelated session baseline.

Use one entrypoint before a non-trivial bug or feature:

```powershell
./.llm-wiki/wiki.ps1 develop `
  -Intent "Fix the dietologist invitation email link"
```

Add `-PlannedPath 'path/one;path/two'` whenever likely files are known. Intent-only
classification is allowed, including Russian task descriptions, but remains lower
confidence until repository paths are grounded. Do not treat an inferred path as
authorization to edit it.

`develop` captures the starting `HEAD` and fingerprints any already modified or
untracked files in the current worktree. Later delta-aware facade commands use
only paths changed after that capture, so unrelated work that was already present
does not expand affected indexes, policy checks, or source-impact reviews. A
subsequent edit to an already dirty file is still detected. Explicit
`-ChangedPath` always overrides the captured task delta.

Baselines use a Wiki-owned internal UUID. A Codex thread/task environment value,
when available, is only an external lookup hint; one active internal session can
be recovered without it, while ambiguous concurrent sessions must supply
`-TaskSessionId` or `-WorkspacePath`. Governed facade commands resolve the same
session workspace after commits, so delivery state is not lost when HEAD moves.
Parallel sessions therefore keep independent snapshots of pre-existing dirt even
though they share the same Git worktree. After runtime ownership and scope are
confirmed, `wiki.ps1 continue-ui` keeps subsequent frontend iterations on the
task delta, focused tests, browser evidence, and the visual completion gate. A
backend, dependency-manifest, or public-entry-point expansion rejects this fast
path and returns the task to the normal adaptive route.

`diff` preserves that isolation while reporting the count and coarse scopes of
still-dirty paths excluded by the task baseline. An existing backend, API, or
database slice therefore remains visible beside a later frontend delta without
being silently claimed from another session. Pass explicit `-ChangedPath` only
when those paths genuinely belong to the current task.

## Profiles

The router selects one FoodDiary-specific profile:

| Profile | Typical change | Required flow |
|---|---|---|
| `ui-discovery` | visual/UI intent whose paths are absent or only heuristically inferred | `ui-trace`, grounded research, rerun `develop -PlannedPath`; no implementation yet |
| `scope-discovery` | non-visual feature or bug intent whose data flow and boundary changes are not grounded | compact brief, existing-flow research, rerun `develop` with refined intent and confirmed paths; no workspace or implementation yet |
| `maintenance` | path-grounded CI diagnostics, dependency compatibility, or deployment/container build fixes without runtime contract changes | evidence brief, implementation, exact failing check, diff plus `verify-fast` |
| `tiny` | bounded presentation-only HTML/SVG or equally local low-risk work | research, implementation, diff/test-plan, `verify-fast` |
| `visual-ui-change` / `visual-tiny` | grounded frontend presentation work; the `visual-tiny` variant is CSS/SCSS-only | compact constraints, implementation, focused checks, browser evidence, `verify-strict-affected`; `visual-tiny` uses stylelint without retracing, tests, or a build during each iteration |
| `bug` | corrective behavior in one bounded flow | research, implementation, focused tests, diff, `verify-fast`; strict publication verification stays in hooks and CI |
| `pattern-extension` | grounded extension of a current, tested repository precedent, including an analogous additive API or migration | precedent brief, compatibility delta, implementation, focused parity tests, `verify-strict-affected` |
| `test-only` | grounded additions or strengthening inside test sources/fixtures only | coverage brief, test implementation, focused test execution, affected refresh plus `verify-fast` |
| `feature` | new behavior or a cross-cutting product slice | research, design, implementation phases, conformance-aware review, full verify |
| `critical` | auth, credentials, identity/private data, payments, migrations, providers, email/invitations, configuration, or delivery boundaries | research, decision checkpoint, design, governed workspace, full verify, independent critique |
| `architectural` | project references, DI/ownership boundaries, module topology, or durable architecture constraints | research, decision checkpoint, design, governed workspace, conformance, independent critique |

The profile is a routing decision, not a waiver. Actual diff evidence may elevate it.
Run `develop` again with changed paths, or use `diff` and `task-refresh`, when the
implementation boundary changes.

For a grounded CSS/SCSS-only scope, the router reports the `visual-tiny`
workflow variant. It does not repeat runtime-owner tracing or component test/build
ceremony during visual calibration. Browser evidence and stylelint remain local
iteration gates; strict publication checks still run before integration and in CI.

Heuristically inferred paths are discovery hints, not grounded scope. Visual
intent that mentions a sensitive UI surface such as an authentication dialog or
billing page stays in `ui-discovery` until `ui-trace` confirms the runtime owner
and the caller supplies `-PlannedPath`. A concrete request to change an auth,
credential, token, privacy, payment, migration, or provider boundary remains
`critical`; naming a UI surface alone does not create backend/auth scope.

Ungrounded feature and bug intent uses `scope-discovery` when it does not
explicitly request an auth, credential, migration, persistence, provider,
privacy, or security boundary change. Merely naming a database or sensitive
entity in a corrective query/read-model intent does not prove such a change.
Explicit incidents such as authorization bypass or credential/data leakage,
and explicit migration, storage, exposure, rotation, or revocation actions,
still escalate immediately. The discovery route verifies whether the
requested data is already produced, traces its current read-model and transport
path, and then re-runs `develop` with evidence-refined intent and confirmed
paths. It cannot create a governed workspace before reclassification.

Path-grounded CI diagnostics, package compatibility failures, and
Docker/container build failures use `maintenance`. Supplied diagnostic paths
and external failure output outrank heuristic module discovery. The route does
not run journeys, handler trace, research, design, or a governed workspace
unless evidence reveals a runtime, security, persistence, or architecture
boundary. Dependency maintenance runs `dependencies`; deployment-build
maintenance runs `rollout`; both finish by rerunning the original failing
command and `verify-fast`.

An explicit request to port, mirror, or repeat an existing repository pattern
uses `pattern-extension` when target paths are grounded and no provider,
configuration, sensitive-data lifecycle, security incident, or architecture
boundary changes. The route verifies a current precedent and its tests, checks
only the API/migration/rollout delta that actually applies, and avoids a fresh
design workspace. Mentioning a pattern never downgrades real critical evidence.

When every grounded path is a test source or fixture, the router uses
`test-only`. Names such as authentication, OpenTelemetry, outbox, provider, or
privacy describe the production behavior being covered; they do not imply that
those production boundaries changed. The route still requires the test to prove
an explicit branch or invariant and calls out removed or relaxed assertions.
Derived Wiki indexes and source-review receipts are removed before this routing
decision, so bookkeeping created by a previous verification pass cannot promote
an otherwise test-only change to `critical`.
Project files, dependency manifests, runner configuration, production sources,
migrations, and API snapshots are excluded and route normally.

Sensitive-data references calibrate review and testing, but their presence in
an existing read model does not by itself make a change critical. A bounded
feature that extends one existing application module through its current
Backend/API/Frontend contract can use the normal feature route without a
governed evidence workspace. API compatibility, consumer review,
localization, focused tests, and browser evidence remain applicable. New
storage, migrations, external providers, changed sensitive-data lifecycle, and
explicit auth or privacy boundaries still select `critical`.

A confirmed bug may cross Frontend, API, Contracts, and Backend while remaining
bounded when all edits belong to one existing module flow, the API change is
additive, and no migration, provider, storage, sensitive-data lifecycle, or
architecture boundary changes. This `bug` variant uses four required stages:
compact root-cause brief and trace, implementation with regression coverage,
focused producer/transport/consumer tests, and final diff plus `verify-fast`.
For an explicitly grounded repository/query-performance fix, the compact brief
replaces handler trace because request-handler discovery is the wrong tool for
that edit boundary.
Journeys and design remain optional unless the trace exposes a product decision
or a second behavioral flow. Full verification stays enforced by pre-push and
CI rather than blocking local bug completion.

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
Structured research, brief, and test-plan results are cached only while the commit,
arguments, and complete dirty-worktree content fingerprint are identical. This makes
repeated adaptive composition fast without weakening freshness after an edit.

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
feature entry point. A bounded frontend-only layout change or local interaction
inside an existing component with no route, public component contract, API,
provider, persistence, privacy, security, configuration, or architecture
boundary uses the `visual-ui-change` profile: a compact constraint and ownership
brief, implementation, focused tests with frontend build, browser evidence, and
a final local completion gate.
It does not require a governed workspace or full index regeneration merely
because the surrounding product journey is critical.

Local toggles, selectors, tabs, and component-state changes stay on this route
when they only alter existing rendering behavior. A new user journey, persisted
state, navigation route, shared public contract, or API still escalates to a
feature route.

The compact visual brief identifies the runtime owner, distinguishes reusable
UI-kit surfaces from application-shell composition, loads scoped instructions,
and records browser-verifiable constraints. It does not prescribe the visual
solution. Layout exploration and UX judgment remain grounded in current code,
the design system, and browser inspection. A separate acceptance stage and a
full research packet are unnecessary unless the brief exposes an unresolved
product, ownership, compatibility, or accessibility decision.

During visual iteration, `verify-fast -VisualUiCompletion` remains the cached local
feedback gate. It reports the affected index plan but deliberately defers regeneration.
After the last visual iteration, `ui-finalize` performs one affected update and the
uncached strict affected checks. Full repository verification remains the CI gate.

For this profile, the local completion gate follows focused tests, build, and
browser evidence when the final diff
remains frontend-only and does not change contracts,
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

1. Confirm actual scope and journey impact with `diff` and `journeys`. Use
   `delivery-status` only when `develop` emitted a required `workspace` stage;
   ordinary feature routes intentionally have no governed task workspace.
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

The strict regression gate preserves this coverage while running independent
parts concurrently. Routing profiles and ceremony budgets run in one group;
research, solutions, QA, handoff, and delivery lifecycle run in another. The
standalone smoke command defaults to `-Group All`, so the grouped orchestrator
changes scheduling only and does not remove assertions.

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

`solutions` reports current-source coverage, matching Git precedent counts, explicit
tradeoffs, rejection conditions, and the evidence that would change the recommendation.
A low-cost option remains only a starting recommendation while its evidence is partial;
structural work is not preferred without proof that the existing boundary cannot satisfy
an explicit invariant. Supply that current-source proof with `solutions -BoundaryEvidence`;
the structural alternative remains partial while the proof is absent.

For `feature`, `critical`, and `architectural` profiles, `design` additionally emits
acceptance-oriented vertical slices: minimum observable behavior, compatibility and
failure behavior, then publication proof. Each slice includes implementation and its
closest verification instead of separating all backend, frontend, and tests into broad
horizontal phases. `tiny`, `maintenance`, and bounded `bug` profiles emit no vertical
slice ceremony.

For cross-layer, API, provider, external-data, asynchronous, `critical`, or
`architectural` work, run `integration-scan` to compose inbound consumers,
outbound dependencies, side effects, async continuations, external boundaries,
and focused verification into one read-only view. It reuses existing indexes and
research evidence and never becomes another source of truth. The command remains
opt-in and reports `recommended=false` for bounded work without integration evidence,
so small routes gain no mandatory stage.
