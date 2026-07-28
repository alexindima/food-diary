---
id: workflow-context-strategy-outcome-learning
title: Learn context strategies from completed tasks
kind: workflow
status: current
area: ai-development
summary: Record tamper-evident real-task outcomes for applied context strategies and use bounded empirical evidence in future experiments.
tags:
  - context
  - learning
  - outcomes
  - verification
sources:
  - .llm-wiki/tools/Manage-LlmWikiContextOutcome.ps1
  - .llm-wiki/tools/Manage-LlmWikiContextExperiment.ps1
  - .llm-wiki/tools/Complete-LlmWikiTaskWorkspace.ps1
  - .llm-wiki/knowledge/context-strategy-outcomes.json
  - .llm-wiki/policies/workspace-policies.json
---

# Learn context strategies from completed tasks

Synthetic context benchmarks answer whether a bundle is compact, relevant, diverse, and safe. They do not prove that the strategy helped an agent finish a real task. The outcome registry closes that gap.

## Lifecycle

1. Run a context experiment and apply its approved winning strategy.
2. Complete the task normally. Completion seals verification evidence, critique, confidence, repairs, and the retrospective.
3. `task-finish` records one immutable outcome event for the applied strategy.
4. Outcome metrics aggregate success rate, rollback frequency, and actual task score by strategy variant.
5. Metrics also separate frontend, API, database, and backend cohorts by risk level.
6. Future context experiments prefer an eligible matching cohort, fall back to eligible global history, and add the bounded empirical adjustment to synthetic quality before selecting a winner.

The outcome score combines readiness, confidence, critique, and verification quality. Failed repairs, prediction misses, impact drift, quarantined context, and explicit rollback reduce the score. Policy caps both penalties and the influence of historical outcomes, so history can break close synthetic ties without bypassing safety gates.

Small samples are shrunk toward the neutral success threshold with a governed prior. Each profile reports its posterior score and confidence instead of treating a few tasks as certain evidence. Health monitoring compares a recent window with the older baseline and watches recent success rate. A degraded profile may only contribute a neutral or negative experiment adjustment; it cannot boost a strategy.

When `blockDegradedAdoption` is enabled, experiments still benchmark a degraded variant for observability but mark it ineligible with `degraded-outcome-history`. The next healthy candidate wins. If every candidate is blocked, the experiment returns `no-safe-variant`; preview, approval, and application then stop before changing the workspace.

## Commands

```powershell
./.llm-wiki/wiki.ps1 context-outcome-metrics
./.llm-wiki/wiki.ps1 context-outcome-profile -WorkspacePath .artifacts/llm-wiki/tasks/<name>
./.llm-wiki/wiki.ps1 context-outcome-health
./.llm-wiki/wiki.ps1 context-outcome-list
./.llm-wiki/wiki.ps1 context-outcome-verify -FailOnInvalid
./.llm-wiki/wiki.ps1 context-outcome-verify -WorkspacePath .artifacts/llm-wiki/tasks/<name> -FailOnInvalid
```

`context-outcome-observe` is idempotent per completion fingerprint and is normally invoked automatically by `task-finish`.

## Integrity

The governed registry is an append-only hash chain in `.llm-wiki/knowledge/context-strategy-outcomes.json`. Every event binds the completion, retrospective, strategy application, applied limits, synthetic score, actual score, task cohort, policy, and previous event hash. A completed workspace keeps a matching `context-strategy-outcome.json` receipt for handoff and audit.
