---
id: workflow-controlled-learning-promotion
kind: workflow
status: current
title: Promote repeated task learnings under review
summary: Aggregate retrospective hypotheses across independent tasks and require evidence-backed human decisions before they become durable guidance or calibration inputs.
sources:
  - .llm-wiki/tools/Manage-LlmWikiLearningPromotion.ps1
  - .llm-wiki/tools/Manage-LlmWikiLearningExperiment.ps1
  - .llm-wiki/tools/Manage-LlmWikiLearningHealth.ps1
  - .llm-wiki/tools/Manage-LlmWikiRetrospective.ps1
  - .llm-wiki/tools/Manage-LlmWikiContextBundle.ps1
  - .llm-wiki/tools/Manage-LlmWikiVerificationCost.ps1
  - .llm-wiki/tools/Complete-LlmWikiTaskWorkspace.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskAudit.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskHandoff.ps1
  - .llm-wiki/knowledge/learning-promotions.json
  - .llm-wiki/knowledge/learning-experiments.json
  - .llm-wiki/knowledge/learning-health.json
  - .llm-wiki/policies/workspace-policies.json
---

# Controlled Learning Promotion

Post-task retrospectives produce hypotheses, not repository truth. The learning-promotion registry accumulates those hypotheses across independently completed task workspaces before a reviewer can approve them.

## Safety model

- `learning-observe` accepts candidates only from a valid, sealed `retrospective.json`.
- Observation identity combines the retrospective hash and source candidate ID, so retries are idempotent.
- Candidate identity is derived from its type, normalized statement, and tags. Equivalent observations therefore converge across tasks.
- Approval requires the configured number of distinct task workspaces, a sufficient average score, and an explicit review reason.
- Approval records the intended target (`durable-memory` or `verification-calibration`); it does not silently rewrite policy or memory.
- `learning-plan` exposes the exact scoped memory or numeric calibration before mutation.
- Shadow evaluation replays historical evidence without exposing the proposal to a task.
- Canary rollout exposes the proposal to a deterministic percentage of task workspaces and records improved, neutral, or degraded outcomes with evidence.
- `learning-apply` requires the latest experiment to succeed, materializes only an approved candidate, and `learning-rollback` disables it without deleting history.
- Rejection and supersedence remain in the append-only history.
- Every event links to the previous event hash. `learning-verify` recomputes the complete chain and candidate state.

The registry lives at `.llm-wiki/knowledge/learning-promotions.json`.

## Workflow

```powershell
./.llm-wiki/wiki.ps1 learning-observe -WorkspacePath .artifacts/llm-wiki/tasks/<task>
./.llm-wiki/wiki.ps1 learning-candidates -Format Json
./.llm-wiki/wiki.ps1 learning-approve -Id <learning-id> -Reason "Confirmed by repeated independent tasks."
./.llm-wiki/wiki.ps1 learning-reject -Id <learning-id> -Reason "Correlation was incidental."
./.llm-wiki/wiki.ps1 learning-plan -Id <learning-id> -Format Json
./.llm-wiki/wiki.ps1 learning-shadow -Id <learning-id> -Format Json
./.llm-wiki/wiki.ps1 learning-canary-start -Id <learning-id> -CanaryPercentage 25 -Reason "Test on a limited task cohort."
./.llm-wiki/wiki.ps1 learning-canary-record -Id <learning-id> -WorkspacePath .artifacts/llm-wiki/tasks/<task> -CanaryOutcome improved -CanaryEvidence "Fewer repair iterations."
./.llm-wiki/wiki.ps1 learning-canary-evaluate -Id <learning-id> -Format Json
./.llm-wiki/wiki.ps1 learning-canary-stop -Id <learning-id> -Reason "Evidence meets promotion policy."
./.llm-wiki/wiki.ps1 learning-apply -Id <learning-id> -Reason "Reviewed materialization scope and effect."
./.llm-wiki/wiki.ps1 learning-rollback -Id <learning-id> -Reason "Observed effect no longer matches reality."
./.llm-wiki/wiki.ps1 learning-supersede -Id <learning-id> -Reason "A newer learning replaces this guidance."
./.llm-wiki/wiki.ps1 learning-verify -FailOnInvalid
./.llm-wiki/wiki.ps1 learning-experiment-verify -FailOnInvalid
./.llm-wiki/wiki.ps1 learning-health-list -Format Json
./.llm-wiki/wiki.ps1 learning-health-waive -Id <learning-id> -Reason "Degradation is explained and accepted."
./.llm-wiki/wiki.ps1 learning-health-verify -FailOnInvalid
```

Successful `task-finish` invokes `learning-observe` automatically. Audit and handoff surfaces show task-specific observations and globally eligible promotion candidates.

## Promotion boundary

An approved event is an auditable authorization to prepare a durable-memory or calibration change. Applying that change remains a separate reviewable operation:

- applied durable memories are selected only when their observed path scopes overlap the current task;
- applied verification calibrations override only their named check IDs and carry candidate provenance into the cost receipt;
- every generated context bundle snapshots the promotion-registry fingerprint;
- every cost forecast snapshots the applied calibration inputs;
- rollback is append-only and immediately removes the learning from new context and forecasts.

## Experiment boundary

- Shadow results include the exact application snapshot and are recomputed during registry verification.
- Starting a canary invalidates an earlier successful experiment for materialization purposes.
- Exposure is stable for the same candidate and workspace, so retries cannot silently switch cohorts.
- Canary observations are unique per candidate and workspace, require evidence, and remain append-only.
- A canary succeeds only after the minimum independent sample count and within the configured degradation threshold.
- Context bundles and verification-cost receipts snapshot both promotion and experiment registry fingerprints.

## Post-application health

Every sealed task records which applied learnings actually influenced its context bundle or verification-cost forecast. The health registry combines that exposure with reproducible retrospective outcomes:

- readiness, confidence, and independent critique form the baseline quality score;
- failed repairs, false-negative predictions, impact drift, and quarantined context add policy-controlled penalties;
- after the minimum sample count, excessive degradation produces a `rollback` recommendation;
- audit treats an unwaived recommendation for a currently applied learning as requiring attention;
- a reviewer may waive the recommendation with a reason or reopen it later;
- rollback remains an explicit append-only learning-promotion action.

This prevents a single noisy task—or even an approved learning—from modifying the project’s governing rules invisibly.
