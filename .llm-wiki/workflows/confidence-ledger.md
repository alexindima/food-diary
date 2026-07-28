---
id: workflow-confidence-ledger
kind: workflow
status: current
title: Explain confidence in an AI task result
summary: Combine requirements, conformance, proof, evidence, impact, repair, prediction, telemetry, and context security into a capped integrity-protected confidence score.
sources:
  - .llm-wiki/tools/Manage-LlmWikiConfidenceLedger.ps1
  - .llm-wiki/tools/Manage-LlmWikiTaskWorkspace.ps1
  - .llm-wiki/tools/Complete-LlmWikiTaskWorkspace.ps1
  - .llm-wiki/policies/workspace-policies.json
---

# Explain confidence in an AI task result

Release readiness answers whether governance gates are satisfied. The confidence
ledger answers how much trust the available evidence supports and which signals
limit that trust.

```powershell
./.llm-wiki/wiki.ps1 task-confidence-assess -WorkspacePath .artifacts/llm-wiki/tasks/<name>
./.llm-wiki/wiki.ps1 task-confidence-create -WorkspacePath .artifacts/llm-wiki/tasks/<name>
./.llm-wiki/wiki.ps1 task-confidence-verify -WorkspacePath .artifacts/llm-wiki/tasks/<name> -FailOnInvalid
```

The score has nine policy-weighted dimensions:

- requirements;
- plan conformance;
- proof of change;
- acceptance and verification evidence;
- impact simulation;
- controlled repair;
- failure prediction;
- verification telemetry;
- context security.

Passing, warning, unassessed, and failing signals receive explicit policy
multipliers. Critical conditions also apply hard caps: unresolved evidence or
repairs cannot produce majority confidence, while invalid context security limits
the score more aggressively. A high raw score can therefore never hide a blocking
condition.

`confidence-ledger.json` records every dimension, earned points, cap, source
artifact hash, final score, level, verdict, and ledger hash. Completion seals the
ledger and refuses a blocked verdict; conditional confidence remains visible
instead of being silently promoted to trusted.
