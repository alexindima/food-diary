---
id: workflow-dependency-rollout
kind: workflow
status: current
title: Review dependencies and rollout
summary: Inspect package changes and generate deployment, verification, and rollback considerations.
tags:
  - workflow
  - dependencies
  - deployment
sources:
  - .llm-wiki/tools/Get-LlmWikiDependencyChanges.ps1
  - .llm-wiki/tools/Get-LlmWikiRolloutPlan.ps1
  - .llm-wiki/generated/configuration-index.json
  - .llm-wiki/policies/change-policies.json
---

# Review dependencies and rollout

Rollout analysis reuses the compiled diff and policy result when called from a
task brief or change packet, so deployment flags and obligations stay aligned.

```powershell
./.llm-wiki/wiki.ps1 dependencies -BaseRef origin/master
./.llm-wiki/wiki.ps1 rollout
```

Dependency review reports added, removed, and version-changed direct NuGet/npm
references. The rollout plan detects migrations, configuration, dependencies,
jobs, external integrations, API, and frontend impact and generates pre-deploy,
deployment, post-deploy, and rollback prompts.

These reports do not replace vulnerability/license tooling, provider changelogs,
staging validation, or an operator-approved production plan.
