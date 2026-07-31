---
id: workflow-acceptance-evidence-matrix
kind: workflow
status: current
title: Map acceptance criteria to evidence
summary: Require every task criterion to have an explicit verification mapping and a resolved evidence-backed outcome.
tags:
  - workflow
  - acceptance
  - evidence
  - requirements
sources:
  - .llm-wiki/tools/Manage-LlmWikiAcceptanceMatrix.ps1
  - .llm-wiki/tools/Get-LlmWikiChangePacket.ps1
  - .llm-wiki/tools/Manage-LlmWikiEvidence.ps1
---

# Map Acceptance Criteria to Evidence

Create the matrix from explicit product or engineering criteria:

```powershell
./.llm-wiki/wiki.ps1 acceptance-init `
  -Objective "Safely evolve fasting start" `
  -Criterion "Existing clients remain compatible" `
  -Criterion "Invalid notes are rejected"
```

Map each criterion to one or more discovered verification targets:

```powershell
./.llm-wiki/wiki.ps1 acceptance-map `
  -CriterionId AC-001 `
  -ScenarioId backend-contract-consumers `
  -CheckId architecture-tests
```

Resolve only after observing evidence:

```powershell
./.llm-wiki/wiki.ps1 acceptance-resolve `
  -CriterionId AC-001 `
  -AcceptanceStatus satisfied `
  -EvidenceNote "Consumer compilation and focused tests passed."

./.llm-wiki/wiki.ps1 acceptance-validate -RequireEvidence -FailOnInvalid
```

Validation rejects unmapped, pending, rejected, or satisfied-but-unverified criteria. A satisfied criterion needs either an explicit evidence note or a mapped check/review resolved in the evidence bundle. `not-applicable` requires a reason.

Visual and browser-backed criteria may map to a review resolved through
`evidence-artifact`. The evidence bundle stores the artifact kind, workspace
path, and SHA-256 while the review lineage records the exact attestation.

The matrix prevents green infrastructure checks from being mistaken for proof that every requested behavior was delivered.
