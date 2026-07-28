---
id: workflow-release-readiness
kind: workflow
status: current
title: Evaluate release readiness
summary: Produce one non-compensating verdict across policy, architecture, compatibility, scope, acceptance, evidence, privacy, and rollout.
tags:
  - workflow
  - release
  - readiness
  - evidence
sources:
  - .llm-wiki/tools/Get-LlmWikiReleaseReadiness.ps1
  - .llm-wiki/tools/Get-LlmWikiChangePacket.ps1
  - .llm-wiki/tools/Manage-LlmWikiChangeManifest.ps1
  - .llm-wiki/tools/Manage-LlmWikiAcceptanceMatrix.ps1
  - .llm-wiki/tools/Manage-LlmWikiEvidence.ps1
---

# Evaluate Release Readiness

Run the strict final gate against the real Git change set:

```powershell
./.llm-wiki/wiki.ps1 readiness `
  -RequireManifest `
  -RequireAcceptance `
  -RequireEvidence `
  -FailOnNotReady
```

The scorecard evaluates:

- structural change policy;
- enforced architecture health;
- API compatibility;
- manifest scope and obligation drift;
- acceptance criteria coverage;
- resolved check and review evidence;
- privacy-sensitive impact;
- specialized rollout readiness.

Verdicts are:

- `ready`: every applicable dimension passed and nothing is unassessed;
- `conditional`: no hard failure exists, but optional governance artifacts or an assessment are absent;
- `blocked`: at least one hard gate failed.

The numeric score is informational. A failed dimension always blocks release and cannot be compensated by points elsewhere. Synthetic `-ChangedPath` inputs cannot prove an OpenAPI diff, so API compatibility remains unassessed until run against a real Git diff.
