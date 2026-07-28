---
id: workflow-verification-planner
kind: workflow
status: current
title: Plan minimal trusted verification
summary: Compile required checks into a deterministic, integrity-protected execution plan with explicit coverage and fail-fast ordering.
sources:
  - .llm-wiki/tools/Manage-LlmWikiVerificationPlan.ps1
  - .llm-wiki/tools/Invoke-LlmWikiTaskChecks.ps1
  - .llm-wiki/policies/change-policies.json
  - .llm-wiki/policies/workspace-policies.json
---

# Plan minimal trusted verification

See [cost-aware verification](cost-aware-verification.md) for the expected-time
and repair-cost signal used to order otherwise required checks.

Create a plan after task scope is current:

```powershell
./.llm-wiki/wiki.ps1 task-verification-plan `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name>
```

The planner starts from the current change policy, not from commands supplied by
an agent. It covers every required check exactly once, collapses identical
commands, applies explicitly configured supersedence such as
`frontend-verify` covering `frontend-i18n`, and orders structural or inexpensive
checks before broad suites. Already resolved evidence is retained unless
`-IncludePassed` requests a fresh run.

Every required check also receives a decision trace: the policy rule and paths
that selected it, its predicted failure probability and estimated duration,
whether it will execute, reuse trusted evidence, or be covered by another
canonical execution, and a human-readable rationale. The selection summary
separates savings from evidence reuse and command consolidation.

The saved `verification-plan.json` binds the changed-path set, change policy,
workspace policy, complete requirement set, execution commands, coverage map,
decision trace, and selection economics into one SHA-256 hash. Verification
independently recompiles the canonical selection; a newly hashed receipt cannot
invent supersedence, omit a required execution, or forge its reported savings.
Verify before execution:

```powershell
./.llm-wiki/wiki.ps1 task-verification-verify `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name> `
  -FailOnInvalid
```

Changed scope, policy drift, missing or duplicate coverage, non-canonical
commands, and any edited plan field invalidate the plan. Recreate it rather than
editing it.

Preview or execute the compiled plan:

```powershell
./.llm-wiki/wiki.ps1 task-verification-run `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name> `
  -DryRun

./.llm-wiki/wiki.ps1 task-verification-run `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name> `
  -FailOnFailure
```

Execution remains inside the existing canonical command allowlist. A successful
primary execution can resolve explicitly covered checks without rerunning the
same work; each covered requirement receives its own evidence lineage. Failed
checks stop the plan by default, while `-ContinueOnFailure` is available for
diagnostic collection.

Task audit reports invalid saved plans, and handoff includes the plan hash,
execution count, validity, and the correct resume command.
