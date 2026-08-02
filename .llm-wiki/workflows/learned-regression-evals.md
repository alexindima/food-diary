---
id: workflow-learned-regression-evals
kind: workflow
status: current
title: Turn completed-task failures into governed regression evals
summary: Capture strong retrospective signals as reviewable eval candidates and run approved cases in every LLM Wiki verification.
sources:
  - .llm-wiki/tools/Manage-LlmWikiEvalPromotion.ps1
  - .llm-wiki/tools/Manage-LlmWikiRetrospective.ps1
  - .llm-wiki/tools/Complete-LlmWikiTaskWorkspace.ps1
  - .llm-wiki/tools/Invoke-LlmWikiEvals.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskAudit.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskHandoff.ps1
  - .llm-wiki/knowledge/eval-promotions.json
  - .llm-wiki/evals/cases.json
  - .llm-wiki/policies/workspace-policies.json
---

# Learned Regression Evals

Static evals cover known architectural scenarios. Learned evals extend that suite with change profiles that produced strong signals during a real completed task: a missed failure prediction, failed repair, impact drift, flaky check, cost miss, or quarantined context.

## Lifecycle

1. `task-finish` seals a reproducible retrospective.
2. `eval-observe` snapshots the task's changed paths, strong signals, and the final expected module, scope, policy-rule, check, and violation sets.
3. A reviewer approves or rejects the candidate with a reason.
4. `eval-apply` first executes the candidate against current behavior, then adds it to the active eval suite through an append-only event.
5. `eval-rollback` removes the case from future eval runs without erasing its history.

```powershell
./.llm-wiki/wiki.ps1 eval-observe -WorkspacePath .artifacts/llm-wiki/tasks/<task>
./.llm-wiki/wiki.ps1 eval-candidates -Format Json
./.llm-wiki/wiki.ps1 eval-approve -Id <eval-id> -Reason "The failure mode is general and must not recur."
./.llm-wiki/wiki.ps1 eval-apply -Id <eval-id> -Reason "The captured expectations pass under current policy."
./.llm-wiki/wiki.ps1 evals -Detailed
./.llm-wiki/wiki.ps1 eval-rollback -Id <eval-id> -Reason "The scenario was superseded."
./.llm-wiki/wiki.ps1 eval-verify -FailOnInvalid
```

## Safety properties

- Only valid retrospectives from sealed workspaces can produce candidates.
- Signals below the policy threshold are ignored; a task with no strong signal creates no candidate.
- Observation identity is deterministic over changed paths and signal IDs, making retries idempotent.
- The exact eval case and its provenance are hashed into an append-only event chain.
- Approval never activates a case. Application is separate and requires the captured expectations to pass first.
- Static and learned case IDs must remain globally unique.
- Audit and handoff expose pending and applied cases with the registry fingerprint.

The active eval suite therefore grows from evidence, but no single task can silently redefine expected behavior. Static cases may also preserve verified
real-bug lessons with optional trace and privacy expectations; the dietologist
invitation-link case protects both natural-language discovery and token
classification.

Static adaptive-route cases remain the baseline for ceremony shape. In
particular, the visual UI case requires the compact five-stage route so learned
experience cannot silently reintroduce a full research packet, separate
acceptance ceremony, or verification before browser evidence.
The existing dashboard contract-extension case likewise prevents sensitive
read-model references from automatically forcing governed critical ceremony;
explicit authentication and migration cases preserve the true critical
boundary.
The Dashboard local-day case also protects bounded cross-layer bug routing so
layer count alone cannot restore mandatory journeys, design, or full local
verification.
The Dashboard period-selector case protects the same ceremony budget for local
component interaction, even when the intent introduces new selectable behavior.
