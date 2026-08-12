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

Matched product journeys such as `FD-AUTH` are exposed beside generated test
scenarios and may be mapped directly. Empty scenario, check, review, or changed
path catalogs are valid pre-implementation state: mapping reports a specific
unknown evidence identifier instead of failing on a missing object property.

For a packet containing only test sources, initialization automatically maps
each criterion to the changed test bundle, focused test paths, and required
checks. This removes repetitive bookkeeping but does not resolve a criterion:
current execution evidence or an explicit evidence note is still required.

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
