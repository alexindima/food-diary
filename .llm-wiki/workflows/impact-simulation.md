---
id: workflow-impact-simulation
kind: workflow
status: current
title: Simulate change impact before implementation
summary: Forecast contract, consumer, runtime, data, frontend, privacy, module, risk, and verification blast radius from proposed paths.
tags:
  - workflow
  - impact
  - planning
  - blast-radius
sources:
  - .llm-wiki/tools/Manage-LlmWikiImpactSimulation.ps1
  - .llm-wiki/tools/Get-LlmWikiChangePacket.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskBrief.ps1
  - .llm-wiki/tools/Manage-LlmWikiTaskWorkspace.ps1
  - .llm-wiki/tools/Complete-LlmWikiTaskWorkspace.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskAudit.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskHandoff.ps1
  - .llm-wiki/policies/workspace-policies.json
---

# Simulate change impact before implementation

Impact simulation may use inferred paths for discovery, but comparison and
scope commitments require current Git paths or explicit planned paths.

Forecast a hypothetical change without creating a task workspace:

```powershell
./.llm-wiki/wiki.ps1 impact-simulate `
  -ProposedPath FoodDiary.Application/Feature/Handler.cs `
  -Objective 'Change feature behavior' `
  -Format Json
```

The simulation uses the repository indexes and the same deterministic change
packet compiler as normal task planning. It reports scopes, direct and
downstream modules, backend contracts and consumers, frontend contracts and
consumers, runtime bindings, domain/data bindings, privacy boundaries,
required checks, review obligations, risk, and a normalized blast-radius
score.
For frontend proposals it also compares objective-derived feature names with
the proposed paths. The `alignment` result reports `aligned` or `mismatch`,
confidence, evidence terms, expected and covered features, reasons, and
suggested feature/shared roots. Explicit feature names in the objective are
reported as required features, so an incidental match in a different feature
does not hide a path mismatch. This is a planning warning rather than a validity
failure: the agent must still confirm suggestions in source.
The direct `brief` and `test-plan` commands also accept `-ProposedPath` for a
smaller pre-diff view when a complete impact simulation is unnecessary.

For an active workspace, the manifest's planned paths form the forecast and
the stored task packet forms the actual impact:

```powershell
./.llm-wiki/wiki.ps1 task-impact-assess `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name> `
  -FailOnInvalid
```

Unexpected impacts are classified by dimension. Strict defaults permit no
unforecast scope, module, contract, consumer, runtime, data, or frontend
bindings. Missing forecast impacts are retained as useful overestimation
feedback but do not block completion.

Task completion seals `impact-simulation.json`. The receipt binds the
manifest, actual packet, forecast packet fingerprint, policy, both impact
snapshots, drift classification, findings, and verdict. Any later plan,
packet, policy, or classification change invalidates it. Status, audit, and
handoff expose the forecast and actual blast radius.
