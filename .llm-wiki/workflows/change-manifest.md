---
id: workflow-change-manifest
kind: workflow
status: current
title: Govern a change with a manifest
summary: Freeze objective, scope, implementation phases, checks, reviews, generated artifacts, and rollout assumptions, then reconcile them with the final diff and evidence.
tags:
  - workflow
  - manifest
  - scope
  - evidence
sources:
  - .llm-wiki/tools/Manage-LlmWikiChangeManifest.ps1
  - .llm-wiki/tools/Get-LlmWikiImplementationPlan.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskBrief.ps1
  - .llm-wiki/tools/Manage-LlmWikiEvidence.ps1
---

# Govern a Change with a Manifest

Initialize after the intended scope is understood:

```powershell
./.llm-wiki/wiki.ps1 manifest-init `
  -Objective "Add an optional fasting start note safely" `
  -AllowedPath '^FoodDiary\.Application/Fasting/' `
  -AllowedPath '^tests/FoodDiary\.Application\.Tests/Fasting/'
```

The manifest snapshots risk, modules, implementation phases, required checks, review obligations, test scenarios, generated actions, rollout flags, and a SHA-256 plan fingerprint.

Manifest initialization compiles the task brief once and passes that exact
snapshot into implementation planning, keeping its fingerprint and obligations
internally consistent.

Validate repeatedly as the diff evolves:

```powershell
./.llm-wiki/wiki.ps1 manifest-validate
./.llm-wiki/wiki.ps1 manifest-validate -RequireEvidence -FailOnInvalid
```

Validation fails on out-of-scope files, newly triggered checks or reviews absent from the snapshot, structural violations, or unresolved required evidence. Planned files not yet changed are reported but are not failures because plans may include optional or investigative paths.

Reinitialize intentionally when the objective or architecture changes materially; do not silently widen scope merely to make validation pass.
