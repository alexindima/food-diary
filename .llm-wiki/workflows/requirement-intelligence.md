---
id: workflow-requirement-intelligence
kind: workflow
status: current
title: Analyze and expand acceptance requirements
summary: Detect vague, compound, duplicate, and risk-incomplete acceptance criteria before implementation evidence is collected.
tags:
  - workflow
  - requirements
  - acceptance
  - planning
sources:
  - .llm-wiki/tools/Manage-LlmWikiRequirementModel.ps1
  - .llm-wiki/tools/Test-LlmWikiGovernedAuthenticationStart.ps1
  - .llm-wiki/tools/Manage-LlmWikiAcceptanceMatrix.ps1
  - .llm-wiki/tools/Manage-LlmWikiTaskWorkspace.ps1
  - .llm-wiki/tools/Complete-LlmWikiTaskWorkspace.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskAudit.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskHandoff.ps1
  - .llm-wiki/policies/workspace-policies.json
---

# Analyze and expand acceptance requirements

Analyze the current objective and acceptance criteria:

```powershell
./.llm-wiki/wiki.ps1 task-requirements-assess `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name> `
  -FailOnInvalid
```

The model classifies criteria as behavior, failure, compatibility, security,
data, performance, localization, or observability requirements. It measures
atomicity and testability, detects criteria that are too short, vague,
compound, or near-duplicates, and compares coverage with the task packet's
scopes, risk, review obligations, and changed resources.

Missing dimensions are recommendations rather than silent mutations. Apply
the recommendations explicitly:

```powershell
./.llm-wiki/wiki.ps1 task-requirements-expand `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name> `
  -Reason <why-the-expanded-contract-is-intended>
```

Expansion first replaces supported compound outcome lists with atomic,
provenance-marked criteria, preserving the original mapping on every split.
Risk recommendations are themselves atomic; security coverage is represented
separately for authorization, identity-data scope, secrets, and sensitive
logging. Expansion then appends missing recommendations, records the decision
in the task journal, and invalidates old proof and requirement receipts. A
second `task-requirements-assess -FailOnInvalid` must pass before implementation.
The new criteria must still be mapped, verified, and resolved normally.

Task completion automatically seals `requirement-model.json`. Its hash binds
the acceptance matrix, task packet, policy, classifications,
recommendations, findings, and verdict. Status, audit, and handoff expose the
same analysis, while later requirement, packet, or policy drift invalidates
the receipt.
