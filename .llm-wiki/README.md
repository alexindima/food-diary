# FoodDiary LLM Wiki

This directory is a compiled navigation and knowledge layer for coding agents.
It summarizes repository knowledge, but it is not a source of truth.

## Authority Model

When sources disagree, use this precedence:

1. Executable code, tests, project manifests, and runtime configuration.
2. Accepted ADRs for the decisions they record.
3. Current living documentation under `docs/`.
4. Applicable `AGENTS.md` instructions.
5. Pages in this directory.

`AGENTS.md` has a special role: it is authoritative for how an agent must work in
its scope even when a wiki page provides broader context.

## Page Contract

Every knowledge page except this README must start with front matter containing:

```yaml
---
id: stable.unique.id
kind: index|system|module|workflow
status: current|draft|stale
generated_by: optional/path/to/deterministic-generator
sources:
  - path/to/source
---
```

Rules:

- `id` is stable and unique.
- `sources` use repository-relative paths and must exist.
- Important claims link to their canonical source.
- A wiki page must not silently introduce a new architectural rule.
- Generated pages declare `generated_by` and are validated by that generator's
  check mode instead of manual freshness review.
- Set `status: stale` when the sources no longer support the summary.
- Prefer updating an existing page over creating an overlapping page.

## Usage

Start at [index.md](index.md). Follow the smallest relevant set of pages, then
open the cited source files before changing code.

For normal work, prefer the compact path:

```powershell
./.llm-wiki/wiki.ps1 develop -Intent '<task>' -PlannedPath '<known path>'
./.llm-wiki/wiki.ps1 next -Intent '<task>' -PlannedPath '<known path>'
./.llm-wiki/wiki.ps1 next -WorkspacePath .artifacts/llm-wiki/tasks/<name>
./.llm-wiki/wiki.ps1 phase-next -WorkspacePath .artifacts/llm-wiki/tasks/<name>
./.llm-wiki/wiki.ps1 delivery-validate -WorkspacePath .artifacts/llm-wiki/tasks/<name> -FailOnInvalid
./.llm-wiki/wiki.ps1 handoff
```

Use `solutions` when alternatives matter, `qa` for journey-derived manual
scenarios, and `workflow-metrics` during retrospectives. The longer command
catalog below is the diagnostic and governance interface; agents should not
manually orchestrate it when `next` already provides a sufficient action.

Task identity is owned by the Wiki, not by a Codex-specific environment
variable. `develop` creates an internal UUID under the repository Git directory;
known Codex task/thread variables are treated only as optional external hints.
When there is one active task, later commands recover it automatically. With
multiple active tasks and no stable hint, the Wiki refuses to guess and asks for
`-TaskSessionId` or an explicit `-WorkspacePath`. Governed commands without an
explicit workspace use the session workspace, so a commit does not lose the
delivery state.

Adaptive routing includes `pattern-extension` for grounded requests that copy a
current, tested repository precedent. It checks the compatibility delta,
migrations and consumers where applicable, but skips design-from-scratch and
critical ceremony unless actual security, provider or architecture evidence
requires it.

Index updates are serialized and transaction-backed. Every index worker emits a
heartbeat, has its own timeout, and a failed update restores the generated tree.
Verification timeouts terminate the complete subprocess tree and print the exact
standalone command for diagnosis; publication CI still runs uncached strict
verification.
Interrupted local `verify` and `verify-full` runs resume passed stages by
default; receipts are content-addressed independently from each stage's relevant
inputs. CI disables this default and never trusts the local stage cache.

The unified developer entrypoint is:

```powershell
./.llm-wiki/wiki.ps1 help
./.llm-wiki/wiki.ps1 update
./.llm-wiki/wiki.ps1 lint
./.llm-wiki/wiki.ps1 lint -Format Json
./.llm-wiki/wiki.ps1 smoke -SmokeGroup portable
./.llm-wiki/wiki.ps1 smoke -SmokeGroup linux
./.llm-wiki/wiki.ps1 smoke -SmokeGroup tools
./.llm-wiki/wiki.ps1 verify
./.llm-wiki/wiki.ps1 verify-full
./.llm-wiki/wiki.ps1 workspace-policy -FailOnInvalid
./.llm-wiki/wiki.ps1 context -Module Billing -ChangeType Api
./.llm-wiki/wiki.ps1 brief
./.llm-wiki/wiki.ps1 plan -Objective "Describe the intended outcome"
./.llm-wiki/wiki.ps1 packet -Objective "Describe the intended outcome" -OutputPath .artifacts/llm-wiki/change-packet.json
./.llm-wiki/wiki.ps1 acceptance-init -Objective "Describe the intended outcome" -Criterion "Observable criterion"
./.llm-wiki/wiki.ps1 acceptance-validate -RequireEvidence -FailOnInvalid
./.llm-wiki/wiki.ps1 readiness -RequireManifest -RequireAcceptance -RequireEvidence -FailOnNotReady
./.llm-wiki/wiki.ps1 report -OutputPath .artifacts/llm-wiki/review-report.md
./.llm-wiki/wiki.ps1 task-start -Objective "Describe the intended outcome" -Criterion "Observable criterion" -WorkspacePath .artifacts/llm-wiki/tasks/my-task
./.llm-wiki/wiki.ps1 task-list
./.llm-wiki/wiki.ps1 task-audit -StaleAfterDays 7 -EvidenceMaxAgeDays 3
./.llm-wiki/wiki.ps1 task-status -WorkspacePath .artifacts/llm-wiki/tasks/my-task
./.llm-wiki/wiki.ps1 task-refresh -WorkspacePath .artifacts/llm-wiki/tasks/my-task
./.llm-wiki/wiki.ps1 task-run -WorkspacePath .artifacts/llm-wiki/tasks/my-task -DryRun
./.llm-wiki/wiki.ps1 task-handoff -WorkspacePath .artifacts/llm-wiki/tasks/my-task -OutputPath .artifacts/llm-wiki/tasks/my-task/handoff.md
./.llm-wiki/wiki.ps1 task-export -WorkspacePath .artifacts/llm-wiki/tasks/my-task
./.llm-wiki/wiki.ps1 task-export-verify -ExportPath .artifacts/llm-wiki/exports/my-task.task-export.json -FailOnInvalid
./.llm-wiki/wiki.ps1 task-import -ImportPath .artifacts/llm-wiki/exports/my-task.task-export.json -WorkspacePath .artifacts/llm-wiki/tasks/resumed-task -DryRun
./.llm-wiki/wiki.ps1 task-note -WorkspacePath .artifacts/llm-wiki/tasks/my-task -JournalType decision -Text "Record a durable decision"
./.llm-wiki/wiki.ps1 task-journal -WorkspacePath .artifacts/llm-wiki/tasks/my-task
./.llm-wiki/wiki.ps1 task-doctor -WorkspacePath .artifacts/llm-wiki/tasks/my-task -FailOnInvalid
./.llm-wiki/wiki.ps1 task-migrate -WorkspacePath .artifacts/llm-wiki/tasks/my-task -DryRun
./.llm-wiki/wiki.ps1 task-policy-sync -WorkspacePath .artifacts/llm-wiki/tasks/my-task -DryRun
./.llm-wiki/wiki.ps1 task-policy-impact -WorkspacePath .artifacts/llm-wiki/tasks/my-task
./.llm-wiki/wiki.ps1 task-refresh -WorkspacePath .artifacts/llm-wiki/tasks/my-task -DryRun
./.llm-wiki/wiki.ps1 task-lineage -WorkspacePath .artifacts/llm-wiki/tasks/my-task -FailOnInvalid
./.llm-wiki/wiki.ps1 task-cache-find -WorkspacePath .artifacts/llm-wiki/tasks/my-task -CheckId architecture-tests
./.llm-wiki/wiki.ps1 task-cache-reuse -WorkspacePath .artifacts/llm-wiki/tasks/my-task -CheckId architecture-tests -DryRun
./.llm-wiki/wiki.ps1 task-graph -FailOnBlocked
./.llm-wiki/wiki.ps1 task-schedule -MaxConcurrency 3
./.llm-wiki/wiki.ps1 task-schedule-plan-create -MaxConcurrency 3
./.llm-wiki/wiki.ps1 task-schedule-plan-claim -PlanId <id>
./.llm-wiki/wiki.ps1 task-schedule-plan-claim -PlanId <id> -Apply
./.llm-wiki/wiki.ps1 task-orchestration-audit -FailOnInvalid
./.llm-wiki/wiki.ps1 task-orchestrate -MaxConcurrency 3
./.llm-wiki/wiki.ps1 task-orchestrate -MaxConcurrency 3 -Apply -FailOnAttention
./.llm-wiki/wiki.ps1 task-orchestration-cycle-list
./.llm-wiki/wiki.ps1 task-orchestration-cycle-verify -CycleId <id>
./.llm-wiki/wiki.ps1 task-watchdog
./.llm-wiki/wiki.ps1 task-watchdog -Apply
./.llm-wiki/wiki.ps1 task-circuit-list
./.llm-wiki/wiki.ps1 task-circuit-reset -WorkspacePath .artifacts/llm-wiki/tasks/<name> -Reason "Reviewed and safe to retry"
./.llm-wiki/wiki.ps1 task-decompose-plan -WorkspacePath .artifacts/llm-wiki/tasks/<name>
./.llm-wiki/wiki.ps1 task-decompose-apply -DecompositionId <id>
./.llm-wiki/wiki.ps1 task-verification-plan -WorkspacePath .artifacts/llm-wiki/tasks/<name>
./.llm-wiki/wiki.ps1 task-verification-verify -WorkspacePath .artifacts/llm-wiki/tasks/<name>
./.llm-wiki/wiki.ps1 task-verification-run -WorkspacePath .artifacts/llm-wiki/tasks/<name> -DryRun
./.llm-wiki/wiki.ps1 task-cost-forecast -WorkspacePath .artifacts/llm-wiki/tasks/<name>
./.llm-wiki/wiki.ps1 task-cost-verify -WorkspacePath .artifacts/llm-wiki/tasks/<name> -FailOnInvalid
./.llm-wiki/wiki.ps1 verification-telemetry-metrics
./.llm-wiki/wiki.ps1 verification-telemetry-verify -FailOnInvalid
./.llm-wiki/wiki.ps1 task-context-create -WorkspacePath .artifacts/llm-wiki/tasks/<name>
./.llm-wiki/wiki.ps1 task-context-budget-create -WorkspacePath .artifacts/llm-wiki/tasks/<name>
./.llm-wiki/wiki.ps1 task-context-budget-verify -WorkspacePath .artifacts/llm-wiki/tasks/<name> -FailOnInvalid
./.llm-wiki/wiki.ps1 task-context-benchmark -SourceWorkspacePath <baseline> -WorkspacePath <candidate>
./.llm-wiki/wiki.ps1 task-context-benchmark-create -SourceWorkspacePath <baseline> -WorkspacePath <candidate>
./.llm-wiki/wiki.ps1 task-context-experiment-plan -WorkspacePath .artifacts/llm-wiki/tasks/<name>
./.llm-wiki/wiki.ps1 task-context-experiment-run -WorkspacePath .artifacts/llm-wiki/tasks/<name>
./.llm-wiki/wiki.ps1 task-context-strategy-preview -WorkspacePath .artifacts/llm-wiki/tasks/<name>
./.llm-wiki/wiki.ps1 task-context-strategy-approve -WorkspacePath .artifacts/llm-wiki/tasks/<name> -Reason <review-rationale>
./.llm-wiki/wiki.ps1 task-context-strategy-apply -WorkspacePath .artifacts/llm-wiki/tasks/<name>
./.llm-wiki/wiki.ps1 context-outcome-metrics
./.llm-wiki/wiki.ps1 context-outcome-health
./.llm-wiki/wiki.ps1 context-outcome-verify -FailOnInvalid
./.llm-wiki/wiki.ps1 task-context-verify -WorkspacePath .artifacts/llm-wiki/tasks/<name>
./.llm-wiki/wiki.ps1 task-context-security-assess -WorkspacePath .artifacts/llm-wiki/tasks/<name>
./.llm-wiki/wiki.ps1 task-context-security-verify -WorkspacePath .artifacts/llm-wiki/tasks/<name> -FailOnInvalid
./.llm-wiki/wiki.ps1 task-confidence-assess -WorkspacePath .artifacts/llm-wiki/tasks/<name>
./.llm-wiki/wiki.ps1 task-confidence-verify -WorkspacePath .artifacts/llm-wiki/tasks/<name> -FailOnInvalid
./.llm-wiki/wiki.ps1 task-context-compare -SourceWorkspacePath .artifacts/llm-wiki/tasks/<source> -WorkspacePath .artifacts/llm-wiki/tasks/<target>
./.llm-wiki/wiki.ps1 task-context-feedback -DispatchId <id> -Owner agent-1 -HelpfulContextPath AGENTS.md -MissingContextPath docs/ARCHITECTURE.md -Reason "Observed during implementation"
./.llm-wiki/wiki.ps1 task-context-feedback-metrics -FailOnInvalid
./.llm-wiki/wiki.ps1 task-agent-quarantine -AgentId <id> -Owner agent-1 -Reason "Repeated failures"
./.llm-wiki/wiki.ps1 task-agent-unquarantine -AgentId <id> -Owner agent-1
./.llm-wiki/wiki.ps1 task-agent-register -Owner agent-1 -Capability backend,api,tests -Capacity 1
./.llm-wiki/wiki.ps1 task-agent-list
./.llm-wiki/wiki.ps1 task-agent-coverage -FailOnGap
./.llm-wiki/wiki.ps1 task-dispatch-start -WorkspacePath .artifacts/llm-wiki/tasks/my-task -Owner agent-1 -Lane 1
./.llm-wiki/wiki.ps1 task-dispatch-heartbeat -DispatchId <id> -Owner agent-1
./.llm-wiki/wiki.ps1 task-dispatch-complete -DispatchId <id> -Owner agent-1 -Result "Implemented and verified"
./.llm-wiki/wiki.ps1 task-dispatch-list -FailOnInvalid
./.llm-wiki/wiki.ps1 task-dispatch-reconcile
./.llm-wiki/wiki.ps1 task-dispatch-reconcile -Apply
./.llm-wiki/wiki.ps1 task-dispatch-prune -RetentionDays 30
./.llm-wiki/wiki.ps1 task-dispatch-prune -RetentionDays 30 -Apply
./.llm-wiki/wiki.ps1 task-dispatch-metrics -WindowDays 30
./.llm-wiki/wiki.ps1 task-dispatch-snapshot-save -WindowDays 30
./.llm-wiki/wiki.ps1 task-dispatch-snapshot-compare -FailOnRegression
./.llm-wiki/wiki.ps1 task-lease-acquire -WorkspacePath .artifacts/llm-wiki/tasks/my-task -Owner agent-1
./.llm-wiki/wiki.ps1 task-finish -WorkspacePath .artifacts/llm-wiki/tasks/my-task
./.llm-wiki/wiki.ps1 task-verify -WorkspacePath .artifacts/llm-wiki/tasks/my-task -FailOnInvalid
./.llm-wiki/wiki.ps1 manifest-init -Objective "Describe the intended outcome" -AllowedPath '<regex>'
./.llm-wiki/wiki.ps1 manifest-validate -RequireEvidence -FailOnInvalid
./.llm-wiki/wiki.ps1 test-plan
./.llm-wiki/wiki.ps1 dependencies -BaseRef origin/master
./.llm-wiki/wiki.ps1 rollout
./.llm-wiki/wiki.ps1 hotspots
./.llm-wiki/wiki.ps1 test-gaps
./.llm-wiki/wiki.ps1 topology
./.llm-wiki/wiki.ps1 privacy -PrivacyCategory credential
./.llm-wiki/wiki.ps1 ui -FrontendView components -Query autocomplete
./.llm-wiki/wiki.ps1 domain -DomainView invariants -Query weight
./.llm-wiki/wiki.ps1 contracts -BackendContractView consumers -Query StartFastingCommand
./.llm-wiki/wiki.ps1 health -HealthView dead-candidates
./.llm-wiki/wiki.ps1 diff
./.llm-wiki/wiki.ps1 ownership
./.llm-wiki/wiki.ps1 api-compat -BaseRef origin/master -FailOnBreaking
./.llm-wiki/wiki.ps1 policy
./.llm-wiki/wiki.ps1 evidence-init
./.llm-wiki/wiki.ps1 evals
./.llm-wiki/wiki.ps1 eval-candidates -Format Json
./.llm-wiki/wiki.ps1 eval-approve -Id <eval-id> -Reason "Preserve this confirmed failure mode."
./.llm-wiki/wiki.ps1 eval-apply -Id <eval-id> -Reason "Captured expectations pass."
./.llm-wiki/wiki.ps1 learning-health-list -Format Json
./.llm-wiki/wiki.ps1 learning-health-waive -Id <learning-id> -Reason "Reviewed degradation is accepted."
./.llm-wiki/wiki.ps1 task-similarity-find -WorkspacePath .artifacts/llm-wiki/tasks/<target>
./.llm-wiki/wiki.ps1 task-similarity-reuse -WorkspacePath .artifacts/llm-wiki/tasks/<target> -SourceWorkspacePath .artifacts/llm-wiki/tasks/<source> -DryRun
```

`lint` is the fast deterministic page gate. It enforces the front matter
contract, unique ids, normalized and existing sources, generated-page
provenance, local link targets and anchors, and high-confidence credential
signatures. Text diagnostics use stable `WIKI###` codes; `-Format Json` exposes
the same result to any agent or CI consumer.

`smoke portable` is the short cross-version contract and runs on the current
PowerShell. `smoke linux` runs that contract in the pinned PowerShell 7.5
Ubuntu Docker image; `smoke tools` runs the complete stateful developer-tool
lifecycle.

`verify` is the fast interactive and handoff gate. It starts with lint and its
regression fixtures, then checks page structure, generated indexes, freshness,
eval regressions, failure records, change policy, and impact review.
`verify-full` adds the portable and complete stateful smoke suites while
running independent index checks and tool scenarios concurrently. In CI the
full Wiki gate is a separate job, so it no longer blocks backend restore,
build, and tests.

Verify the wiki from the repository root:

```powershell
./.llm-wiki/tools/Test-LlmWiki.ps1
```

Regenerate and verify the machine-readable repository catalog:

```powershell
./.llm-wiki/tools/Build-LlmWikiCatalog.ps1
./.llm-wiki/tools/Build-LlmWikiCatalog.ps1 -Check
./.llm-wiki/tools/Build-LlmWikiModulePages.ps1
./.llm-wiki/tools/Build-LlmWikiModulePages.ps1 -Check
./.llm-wiki/tools/Build-LlmWikiSymbolIndex.ps1
./.llm-wiki/tools/Build-LlmWikiSymbolIndex.ps1 -Check
./.llm-wiki/tools/Build-LlmWikiFrontendIndex.ps1
./.llm-wiki/tools/Build-LlmWikiFrontendIndex.ps1 -Check
./.llm-wiki/tools/Build-LlmWikiConfigurationIndex.ps1
./.llm-wiki/tools/Build-LlmWikiConfigurationIndex.ps1 -Check
./.llm-wiki/tools/Build-LlmWikiQualityIndex.ps1
./.llm-wiki/tools/Build-LlmWikiQualityIndex.ps1 -Check
./.llm-wiki/tools/Build-LlmWikiRuntimeTopology.ps1
./.llm-wiki/tools/Build-LlmWikiRuntimeTopology.ps1 -Check
./.llm-wiki/tools/Build-LlmWikiSensitiveDataIndex.ps1
./.llm-wiki/tools/Build-LlmWikiSensitiveDataIndex.ps1 -Check
```

Build a compact task context:

```powershell
./.llm-wiki/tools/Find-LlmWikiContext.ps1 -Module Billing -ChangeType Api
```

Analyze the current change set:

```powershell
./.llm-wiki/tools/Get-LlmWikiDiffContext.ps1
```

Promote an evidence-backed task decision for reuse by future context bundles:

```powershell
./.llm-wiki/wiki.ps1 memory-candidates `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name>
./.llm-wiki/wiki.ps1 memory-promote `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name> `
  -NoteId J-0001 `
  -Id <durable-id> `
  -MemoryScopePath '<path-regex>' `
  -MemoryEvidence '<verification>'
./.llm-wiki/wiki.ps1 memory-verify -FailOnInvalid
```

Review pages affected by local changes:

```powershell
./.llm-wiki/tools/Get-LlmWikiImpact.ps1
```

Enforce freshness for a Git change set:

```powershell
./.llm-wiki/tools/Get-LlmWikiImpact.ps1 -BaseRef origin/main -HeadRef HEAD -FailOnUnreviewed
```

When a declared source changes, the corresponding page must also change in the
same change set or already have `status: stale`. A page edit means its summary
was reviewed; use `stale` when it cannot yet be reconciled with the source.

The verifier is deterministic. Generating or refreshing prose may use an LLM,
but CI verification must not require a model or network access.
